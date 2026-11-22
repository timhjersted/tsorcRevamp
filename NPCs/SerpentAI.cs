using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.NPCs.Bosses.GreatSerpent;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs
{
    ///<summary>
    ///Movement/chain AI for the OolacileSerpent boss. Forked from AIWorm (GlobalNPC.cs:4812) rather than sharing
    ///it, since every other worm/dragon boss using AIWorm flies (noGravity+noTileCollide, ignores terrain
    ///entirely) while this one needs to actually read and follow terrain. Body/tail chain-following math is
    ///ported near-verbatim from AIWorm (that part is generic chain plumbing, independent of head movement); only
    ///the head's movement is new.
    ///</summary>
    public static class SerpentAI
    {
        const int TileSize = 16;

        //Sensing distance for the "obstacle too tall to climb -- look for a shorter spot" fallback.
        public const int NavSearchRadius = 80;

        //Obstacles at or below this height are walked/stepped over smoothly (normal ground-follow).
        //Taller than this (up to MaxClimbHeightTiles) triggers the climb sub-state.
        //(internal: SmartSerpent4AI's edge builder shares these thresholds so the graph never promises a move
        //the reactive executor can't perform.)
        internal const int SmallStepTiles = 2;

        //Tallest obstacle the serpent will rear up and climb over (~half its body length).
        internal const int MaxClimbHeightTiles = 11;

        //How far below/above a chain-follow-predicted position we'll look for ground before treating the
        //segment as spanning a gap (and skipping the ground-snap correction).
        const int GroundSnapToleranceTiles = 4;

        //Wider fallback search used ONLY when the normal tolerance above finds nothing (see SnapHeadToGround).
        //A bad climb (or the old Unstick failsafe, since removed) could otherwise leave the head well above the
        //real floor with "no ground within tolerance" reading as a void -- it would then just glide flat in
        //open air forever instead of ever coming back down (the reported "floats instead of moving back down"
        //bug). This is a wide, slow-descend catch-all for that, separate from the tight tolerance used for
        //normal terrain-hugging.
        const int FarGroundSearchTiles = 200;
        const float MaxFallbackDescendSpeed = 3f;

        const float GroundFollowLerp = 0.25f;
        const float ClimbRiseSpeed = 1.8f;
        const float WallClimbSpeed = 3.4f;   //px/tick it scales a sheer wall to reach a player on a ledge above
        const float FarSpeedBoostRangeTiles = 32f; //distance past kite range at which the speed boost maxes out
        const float FarSpeedBoostMax = 0.65f;      //up to +65% closing speed above 50% HP
        const float LowHealthSpeedMultiplier = 1.45f; //base pursuit multiplier below 50% HP
        const float LowHealthFarSpeedBoostMax = 1.25f; //much stronger closing-distance speed boost below 50% HP
        const float MoveSoundSpeedFraction = 0.85f; //only slither-sounds when near this fraction of top speed
        const int MoveSoundIntervalTicks = 42;
        //Max consecutive ticks of climbing before it must travel flat again.
        const int ClimbBudgetTicks = 180;

        //Follower spacing = average neighbour sprite-length minus a fixed pixel overlap (the values live on SerpentChain,
        //which the offline rig shares). The sprites are trimmed to sit SegmentOverlap px on top of each other for a
        //seamless body; the first HeadGlueSegments pieces overlap much more so the head stays welded to the body on turns.
        const float SegmentOverlap = SerpentChain.SegmentOverlap;
        const int HeadGlueSegments = SerpentChain.HeadGlueSegments;
        const float HeadGlueOverlap = SerpentChain.HeadGlueOverlap;

        //Facing only flips once the neighbour's center is clearly to one side -- kills per-frame flicker on near-vertical
        //stretches of body (steep slopes, the overhead C). See the spriteDirection block in Run().
        const float SpriteFlipDeadzone = 10f;

        ///<summary>
        ///How fast the HEAD sprite may turn its facing, per tick (~0.05 rad = ~172 deg/sec). Body joints no longer use this:
        ///they are solved by SerpentChain.SolveLink, which holds a thickness-based comfort bend between neighbours instead.
        ///</summary>
        const float JointMaxTurnRadians = 0.05f;

        //-- Anti-stuck failsafe --
        //NEVER phases through terrain (a prior version, Unstick, did -- removed per feedback: the serpent must
        //never clip through solid tiles, not even as an escape hatch). Instead it forces a terrain-respecting
        //RunWander window, exactly like the natural "player unreachable" wander, just triggered by "physically
        //stuck" instead of "can't see/reach the player".
        const int StuckSampleTicks = 90;       //no meaningful movement for this long (while unable to engage) -> stuck
        const float StuckMoveThreshold = 32f;  //px of travel that counts as "made progress"
        const int StuckWanderDurationTicks = 180; //how long the forced recovery-wander runs before it can re-arm

        //-- Unreachable -> wander -> despawn --
        const float AggroRangeTiles = 150f;    //beyond this (with no active nav plan and outside NavSearchRadius) it counts as unreachable
        const int WanderStartTicks = 20 * 60;  //20s unreachable -> stop hunting, wander
        const int DespawnTicks = 60 * 60;      //60s unreachable -> give up and despawn (per design)
        const float WanderSpeedMul = 0.65f;

        //-- Passive wander (the state it spawns in; see RunPassive) --
        const float PassiveWanderSpeed = 1.5f;     //px/tick -- a slow amble, well under the hunting pace
        const int PassiveLegMinTicks = 420;        //how long one walk lasts before it stops to rest (blocked terrain ends it early)
        const int PassiveLegMaxTicks = 900;
        const int PassiveHaltMaxTicks = 90;        //brake window cap; it also ends as soon as the head is nearly still
        const float PassiveHaltStopSpeed = 0.15f;
        const float PassiveTurnSpeed = 1.6f;       //px/tick along the U-turn arc
        const float PassiveTurnMaxRadius = 110f;   //arc radius (px); the head rises 2x this at the top
        const float PassiveTurnMinRadius = 80f;    //about the tightest curve 15 deg joints allow for the ~19 px front links (link / 0.26); a low roof stops the head at the ceiling instead
        const int PassiveTurnClearanceScanTiles = 22;
        const int PassiveRestTicks = 600;
        const float PassiveRestLowerLerp = 0.04f;  //slow settle so the head eases down off the top of the turn
        const float PassiveRestHeadDrop = 0.3f;    //tan of the resting head's downward tilt (~17 deg)
        const int PassiveFlickGapMinTicks = 120;
        const int PassiveFlickGapMaxTicks = 280;
        const int PassiveGraceTicks = 30;          //after spawn, HP is re-baselined each tick so spawn-time scaling can't read as a hit

        //Tail flick: the last TailFlickPieceCount pieces (body pieces + the tail) each bend an equal share of the
        //sweep, so the end of the snake lifts off the ground and curls up into a C, holds a moment, then lays back down.
        //Kept to the very end of the chain on purpose: the C scales with the curled length, and curling the whole
        //rear section (17 pieces, ~420px) towered ~20 tiles. The 98px tail piece alone is ~6 tiles of that height.
        const int TailFlickRiseTicks = 50;
        const int TailFlickHoldTicks = 25;
        const int TailFlickLowerTicks = 70;
        const int TailFlickPieceCount = 7;         //6 body pieces + the tail
        const float TailFlickSweepRadians = 1.5f;  //total curl across those pieces (~86 deg); per joint it must stay near the pieces comfort bend (SerpentChain.ComfortBend)

        //-- Kiting: approach aggressively, stop at close kite range, attack from there, never reverse.
        //When it reaches the player it may cross THROUGH to the far side.
        const float KiteRangeTiles = 9f;         //stop advancing once the player is within this horizontally (tighter below 50% HP)
        const float LowHealthKiteRangeTiles = 6.5f;
        const float DirDeadzoneTiles = 4f;       //hysteresis: don't flip facing until the player is this far to one side
        const float CrossOverTriggerTiles = 6f;  //this close -> a chance to cross to the far side
        const float CrossOverExitTiles = 16f;    //keep crossing until this far past the player on the far side
        const int CrossOverCooldownTicks = 300;
        const int CrossOverTriggerRoll = 120;

        //-- Charge --
        const int ChargeTelegraphTicks = 40;
        const int ChargeDurationTicks = 70;
        const int ChargeCooldownTicks = 240;
        const float ChargeSpeedMultiplier = 2.5f;   //x the base speed -> a telegraphed burst
        const int ChargeMinDistanceTiles = 16;
        const int ChargeTriggerRoll = 40;
        const int ChargeContactDamage = 55;

        //-- Idle ripple --
        const int RippleIdleCooldownTicks = 180;      // 3s, per design
        const int RippleDurationTicks = 60;
        const float RippleAmplitude = 5f;
        const float RippleFrequency = 0.25f;
        const float RippleSegmentPhaseOffset = 0.6f;

        //-- Swim --
        const float SwimSpeedMultiplier = 0.8f;
        const float SwimSineAmplitude = 3f;
        const float SwimSineFrequency = 0.15f;
        const float WaterKiteRangeTiles = 14f;   //hold this far off in water instead of overlapping the player
        const float WaterLungeMultiplier = 0.55f; //bite/pounce lunges are slower (heavier) underwater

        //-- Predator-style hunting cloak + scripted HP-threshold ambush --
        //NOT a random blink (that was the wrong reference -- VoodooShaman's style uses a per-frame chance roll,
        //explicitly not wanted here). This is state-driven: cloaks when it loses line of sight to the player
        //while actively hunting (not mid attack/tailstab/charge/ambush/unstick), and only reveals again once LOS
        //is reacquired -- a clean, deterministic transition, not a timer. Gated by CloakCooldownTicks so it
        //can't spam on/off every time LOS flickers. Values are -10% from the first pass per feedback that it
        //read as too invisible; neither should ever approach 255 (truly invisible).
        const int IdleCloakAlpha = 153;    //light, still-fair transparency while hunting without LOS
        const int AmbushCloakAlpha = 212;  //deeper cloak during the scripted retreat-then-sneak-attack
        const int CloakCooldownTicks = 600; //must elapse after a hunting-cloak reveal before it can cloak again
        const float AmbushHpThresholdFraction = 0.3f; //fires once per 30%-of-max-HP step lost
        const int AmbushVanishTicks = 70;              //retreat-and-fade window before it starts sneaking back in
        const int AmbushRepositionMaxTicks = 240;      //failsafe cap (4s) so a bad approach can't hide it forever
        const float AmbushRepositionRangeTiles = 20f;  //close enough (while still cloaked) to commit to the reveal
        const float AmbushMoveSpeed = 6f;              //shared retreat/reposition ground speed

        //-- Scripted death: fade to invisible + blood spray instead of an instant vanilla pop --
        public const int DeathFadeDuration = 60;  //~1s -- read by GreatSerpentHead.CheckDead()
        const int SegmentDeathFadeTicks = 18;     //~0.3s per body/tail piece once its neighbour starts dissolving too

        public static int[] BuildBodyTypes()
        {
            int body = ModContent.NPCType<GreatSerpentBody>();
            int body2 = ModContent.NPCType<GreatSerpentBody2>();
            int body3 = ModContent.NPCType<GreatSerpentBody3>();

            //Adaptive to BodySegmentCount: mostly the fat Body, tapering to the two thinner variants near the
            //tail. (Must return exactly BodySegmentCount entries -- the spawn loop indexes bodyTypes[0..count-1].)
            int count = GreatSerpentHead.BodySegmentCount;
            int[] arr = new int[count];
            for (int i = 0; i < count; i++)
            {
                if (i >= count - 2)
                {
                    arr[i] = body3;
                }
                else if (i >= count - 4)
                {
                    arr[i] = body2;
                }
                else
                {
                    arr[i] = body;
                }
            }
            return arr;
        }

        ///<summary>Entry point, called from every OolacileSerpent piece's AI().</summary>
        public static void Run(NPC npc, int headType, int[] bodyTypes, int tailType, int wormLength, float maxSpeed)
        {
            //Flip sprite so it's always facing the right way (verbatim from AIWorm).
            if (npc.type == headType)
            {
                if (npc.velocity.X < 0f || Math.Abs(npc.velocity.X) < 0.1f)
                {
                    npc.spriteDirection = 1;
                }
                else if (npc.velocity.X > 0f)
                {
                    npc.spriteDirection = -1;
                }
            }
            else
            {
                //Compare joint CENTERS (not top-left position.X, which shifts by half-width differences between
                //Head=50, Body=26, Body2=22, Body3=14, Tail=10) and require a generous deadzone so segments
                //resting on a slope never flicker their spriteDirection.
                NPC aheadSegment = Main.npc[(int)npc.ai[1]];
                float aheadCenterDx = aheadSegment.Center.X - npc.Center.X;
                if (Math.Abs(aheadCenterDx) > SpriteFlipDeadzone)
                {
                    if (aheadCenterDx < 0f)
                    {
                        npc.spriteDirection = 1;
                    }
                    else
                    {
                        npc.spriteDirection = -1;
                    }
                }
            }

            int linkedHeadIndex = (int)npc.ai[3];
            if (npc.type == headType)
            {
                npc.realLife = npc.whoAmI;
            }
            else if (linkedHeadIndex >= 0 && linkedHeadIndex < Main.maxNPCs && Main.npc[linkedHeadIndex].active
                && Main.npc[linkedHeadIndex].type == headType)
            {
                //NPC slot zero is a valid head too; every client must route body damage to it.
                npc.realLife = linkedHeadIndex;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (npc.localAI[0] == 1 && npc.localAI[0] > 0)
                {
                    npc.netUpdate = true;
                    npc.localAI[0] = -1;
                }
                else
                {
                    npc.localAI[0]--;
                }

                //Bulk-spawn the whole chain from the head, once, the first frame it exists. Ported from
                //AIWorm's fly-spawn branch (GlobalNPC.cs:4872-4913) -- simpler and proven than the incremental
                //one-at-a-time spawn AIWorm also supports, and independent of movement style.
                if (npc.ai[0] == 0f && npc.type == headType)
                {
                    npc.ai[3] = npc.whoAmI;
                    npc.realLife = npc.whoAmI;

                    int npcID = npc.whoAmI;
                    for (int chainIndex = 0; chainIndex < wormLength - 1; chainIndex++)
                    {
                        int npcType = chainIndex == wormLength - 2 ? tailType : bodyTypes[chainIndex];
                        int newnpcID = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, npcType, npc.whoAmI);

                        Main.npc[newnpcID].ai[3] = npc.whoAmI;
                        Main.npc[newnpcID].realLife = npc.whoAmI;
                        Main.npc[newnpcID].ai[1] = npcID;
                        Main.npc[newnpcID].ai[2] = chainIndex; //0-based body position for the free/grounded split
                        Main.npc[newnpcID].rotation = npc.rotation; //start facing the same way as the head (all pieces spawn co-located) so the hose-chain joint limit doesn't have to slowly unwind an arbitrary default-0 rotation
                        Main.npc[npcID].ai[0] = newnpcID;
                        Main.npc[npcID].localAI[0] = 2 + chainIndex * 2;

                        npcID = newnpcID;
                    }
                    npc.netUpdate = true;
                }

                //Body/tail pieces never really die from direct damage (see RunBodyFollow's caller, GetHeadData,
                //and the head-only CheckDead override) -- their removal is entirely this neighbour-cascade. A
                //neighbour counts as "gone" once it's fully inactive OR once it's already mid fade (localAI[2] >
                //0) -- the latter is what lets the WHOLE snake start dissolving together instead of a strictly
                //serial chain (each segment waiting for the previous one to FINISH fading before even starting,
                //which would stretch a ~1s cascade into 50x that). Explicitly excludes the head: its OWN death
                //is entirely owned by CheckDead()/RunDeathFade, and must never be short-circuited by this generic
                //cascade (which sets npc.active=false directly, bypassing loot/OnKill entirely).
                if (npc.type != headType)
                {
                    NPC aheadOfMe = Main.npc[(int)npc.ai[1]];
                    NPC behindMe = npc.type != tailType ? Main.npc[(int)npc.ai[0]] : null;
                    bool aheadGone = !aheadOfMe.active || aheadOfMe.aiStyle != npc.aiStyle;
                    bool behindGone = behindMe != null && (!behindMe.active || behindMe.aiStyle != npc.aiStyle || behindMe.localAI[2] > 0f);
                    if ((aheadGone || behindGone) && npc.localAI[2] <= 0f)
                    {
                        npc.localAI[2] = SegmentDeathFadeTicks;
                    }
                    if (npc.localAI[2] > 0f)
                    {
                        npc.velocity = default;
                        SpraySegmentDeathBlood(npc);
                        npc.alpha = (int)MathHelper.Clamp(255f * (1f - (npc.localAI[2] - 1f) / SegmentDeathFadeTicks), 0f, 255f);
                        npc.localAI[2]--;
                        if (npc.localAI[2] <= 0f)
                        {
                            npc.life = 0;
                            npc.active = false;
                        }
                        return; //dissolving -- skip normal chain-follow/attacks for the rest of this tick
                    }
                }
            }

            //Enforce phasing through terrain every frame -- the serpent is drawn behind tiles and must never get
            //shoved out of a hill by any stray vanilla collision. Cheap insurance against clip/pop.
            npc.noTileCollide = true;
            npc.noGravity = true;

            if (npc.type == headType)
            {
                Vector2 headPositionBefore = npc.position;

                RunHeadLocomotion(npc, maxSpeed);

                // noTileCollide is on (the engine would drift the head straight into walls), so stop the head at
                // solid tiles by hand. Every locomotion branch above returns early, so this has to wrap them.
                BlockHeadAtTerrain(npc, headPositionBefore);

                // The head is the only piece that emits light (a soft pink-violet glow just ahead of the snout, brighter while
                // the mouth is busy). It fades with the cloak so a hidden snake does not give itself away.
                if (!Main.dedServ)
                {
                    GreatSerpentHead glowHead = npc.ModNPC as GreatSerpentHead;
                    float cloakVisibility = 1f - npc.alpha / 255f;
                    float pulse = 0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 2.4f);
                    float glowStrength = 0.45f + 0.1f * pulse;
                    if (glowHead != null && glowHead.IsMouthAttackActive)
                    {
                        glowStrength = 0.85f;
                    }

                    Vector3 glowColor = new Vector3(0.9f, 0.25f, 0.85f);
                    Lighting.AddLight(npc.Center + HeadForward(npc) * EyeForwardPixels, glowColor * glowStrength * cloakVisibility);
                }
            }
            else
            {
                bool isTail = npc.type == tailType;
                int segmentIndex = (int)npc.ai[2];
                bool isRearGrounded = isTail || segmentIndex >= GreatSerpentHead.FrontFreeSegmentCount;
                //Only the last TailAttackSegmentCount pieces (+ tail) participate in the tail attack pose; the
                //rest of the body keeps to the ground even mid-attack.
                bool isTailAttackSeg = isTail || segmentIndex >= GreatSerpentHead.BodySegmentCount - GreatSerpentHead.TailAttackSegmentCount;

                GreatSerpentHead headData = GetHeadData(npc);
                if (headData == null)
                {
                    RunBodyFollow(npc);
                    return;
                }

                //Idle/wander cloak (and the deeper ambush cloak): the head computes one shared alpha target for
                //the whole chain each tick (see RunHeadLocomotion) and every segment just mirrors it here.
                if (isTail)
                {
                    //Tail hide/reveal: invisible (alpha 255) when fully collapsed, fading in as it pokes out for
                    //an attack. Combined with the cloak via Max so whichever wants more transparency wins --
                    //without this, enabling the cloak on the tail would silently clobber its own extend-fade.
                    int tailHideAlpha = (int)(255 * (1f - headData.TailExtend));
                    npc.alpha = Math.Max(tailHideAlpha, headData.CloakAlphaTarget);
                }
                else
                {
                    npc.alpha = headData.CloakAlphaTarget;
                }

                //During a tail attack the tail-end pieces are POSED into a C/S by the head each tick
                //(PoseTailStabArc). Those pieces must not run their own follow/ground-snap or they'd fight the pose.
                bool isPosedByArc = headData.TailStab != GreatSerpentHead.TailStabState.None && isTailAttackSeg;
                if (isPosedByArc)
                {
                    int firstPosedIndex = GreatSerpentHead.BodySegmentCount - GreatSerpentHead.TailAttackSegmentCount;
                    if (!isTail && segmentIndex == firstPosedIndex)
                    {
                        //The anchor has completed its own AI/movement now. Pose once here, rather than on
                        //the head's earlier update, so the front socket cannot move away from the raised chain.
                        NPC owningHead = Main.npc[(int)npc.ai[3]];
                        PoseTailStabArc(owningHead, headData);
                    }

                    return;
                }

                if (isTail)
                {
                    npc.damage = 0; //tail only deals damage while the head's stab is driving it
                }

                //Passive tail flick: while the curl amount is up, every rear piece is told to hold a fixed bend
                //relative to the piece ahead of it (instead of "point at the piece ahead"), which makes the end of
                //the chain curl into a C through the normal hose joints. SmoothStep eases the rise and the return.
                bool inFlickSection = isTail || segmentIndex >= GreatSerpentHead.BodySegmentCount - (TailFlickPieceCount - 1);
                bool curling = inFlickSection && headData.TailFlickAmount > 0f;
                float curlJointRadians = 0f;
                if (curling)
                {
                    float curlShape = MathHelper.SmoothStep(0f, 1f, headData.TailFlickAmount);
                    curlJointRadians = headData.TailFlickSign * curlShape * TailFlickSweepRadians / TailFlickPieceCount;
                }

                //The free front pieces normally ride wherever the head leads them. After the passive U-turn the
                //neck is left hanging in a loop, so while it rests the whole body settles onto the ground like the rear does.
                bool restingPassive = !headData.Provoked && headData.Passive == GreatSerpentHead.PassivePhase.Rest;

                //Ground-hug is a bias on the joint's pull (see RunBodyFollow), never a position write, so spacing and the
                //bend limit still hold. Skipped on a curling piece, where it would pull the lifted C straight back down.
                bool hugGround = (isRearGrounded || restingPassive) && !curling;

                RunBodyFollow(npc, curling, curlJointRadians, hugGround, BodyFlexibility(headData));

                if (!hugGround && headData.RippleTimer > 0)
                {
                    ApplyRippleOffset(npc, segmentIndex, headData);
                }

                //Idle tail collapse: not attacking -> pull the (invisible-or-fading) tail toward the piece ahead
                //of it (the last body segment, typically GreatSerpentBody3) instead of trailing at full chain
                //distance, so it visually "lives inside" that segment until an attack pokes it back out.
                if (isTail)
                {
                    NPC aheadOfTail = Main.npc[(int)npc.ai[1]];
                    if (aheadOfTail.active)
                    {
                        npc.Center = Vector2.Lerp(aheadOfTail.Center, npc.Center, headData.TailExtend);
                    }
                }

                //AcidBody: active body pieces (not the tail) trail purple dust + acid pools on the ground they cross
                if (!isTail && headData.AcidBodyTimer > 0)
                {
                    ApplyAcidBodyTrail(npc);
                }
            }
        }

        static GreatSerpentHead GetHeadData(NPC npc)
        {
            int headIndex = (int)npc.ai[3];
            if (headIndex < 0 || headIndex >= Main.npc.Length || !Main.npc[headIndex].active)
            {
                return null;
            }
            return Main.npc[headIndex].ModNPC as GreatSerpentHead;
        }

        //How loose the body is right now, scaling every joint's comfort bend (see SerpentChain.ComfortBend): an unprovoked
        //snake ambles loosely, a lunge or charge streaks nearly straight, everything else is normal.
        const float RelaxedBodyFlexibility = 1.35f;
        const float StreakingBodyFlexibility = 0.7f;

        static float BodyFlexibility(GreatSerpentHead headData)
        {
            if (!headData.Provoked)
            {
                return RelaxedBodyFlexibility;
            }

            if (headData.IsLunging || headData.ChargeTimer > 0)
            {
                return StreakingBodyFlexibility;
            }

            return 1f;
        }

        //Terrain probe for body pieces: sized so a segment sinking into a floor or wall trips the probe before
        //the sprite visually clips into solid terrain.
        const int BodyProbeSize = 18;
        static readonly Func<Vector2, bool> BodyTerrainProbe = IsBodyCenterInsideTerrain;
        static readonly Func<int, int, bool> HeadSupportProbe = IsSolidTile;

        static bool IsBodyCenterInsideTerrain(Vector2 center)
        {
            Vector2 probeTopLeft = center - new Vector2(BodyProbeSize / 2f, BodyProbeSize / 2f);
            bool inside = Collision.SolidCollision(probeTopLeft, BodyProbeSize, BodyProbeSize);

            return inside;
        }

        ///<summary>
        ///Hose-chain joint, solved by SerpentChain.SolveBodyJoint: this piece's HEADING (the direction from it to the piece
        ///ahead; rotation = heading + 90 deg) chases the pull toward the piece ahead, its bend away from the piece ahead's
        ///heading is held near a comfort angle that comes from the sprites' thickness (and the head's flexibility), and its
        ///position is then DERIVED from that heading at exactly one link from the piece ahead. Position can never disagree
        ///with rotation, spacing is exact, and no joint can kink or fold back -- the chain behaves like a hose.
        ///<para/>
        ///Terrain: if the derived centre lands inside solid tiles, SolveLink tries other legal headings, so the body
        ///wraps corners instead of cutting through them. Ground-hug is a lean on the pull (not a position write).
        ///</summary>
        static void RunBodyFollow(NPC npc, bool curling = false, float curlJointRadians = 0f, bool hugGround = false, float flexibility = 1f)
        {
            if (npc.ai[1] < 0f || npc.ai[1] >= Main.npc.Length)
            {
                return;
            }

            NPC ahead = Main.npc[(int)npc.ai[1]];
            int headIndex = (int)npc.ai[3];
            if (!ahead.active || ahead.whoAmI == npc.whoAmI)
            {
                return;
            }

            bool headStationary = false;
            GreatSerpentHead headData = null;
            NPC headNpc = null;

            if (headIndex >= 0 && headIndex < Main.npc.Length && Main.npc[headIndex].active)
            {
                headNpc = Main.npc[headIndex];
                headData = headNpc.ModNPC as GreatSerpentHead;
                bool tailActive = curling || (headData != null && (headData.TailStab != GreatSerpentHead.TailStabState.None || headData.TailFlickAmount > 0.01f));
                headStationary = !tailActive && headNpc.velocity.LengthSquared() < 0.04f;
            }

            bool deliberateRaise = headData != null && (headData.Attack != GreatSerpentHead.AttackState.None
                || (!headData.Provoked && headData.Passive == GreatSerpentHead.PassivePhase.Turn) || headNpc.velocity.Y < -0.35f);
            if (!hugGround && !curling && !deliberateRaise && headData != null && !IsInWater(headNpc))
            {
                //Keep tree/obstacle-supported circular bends. Only free, unsupported front links gain
                //the same gentle floor bias as the rear; this does not change the terrain/climb rules.
                bool hasBrace = false;
                int centerTileX = (int)(npc.Center.X / TileSize);
                int centerTileY = (int)(npc.Center.Y / TileSize);
                for (int y = centerTileY - 3; y <= centerTileY + 3 && !hasBrace; y++)
                {
                    for (int x = centerTileX - 3; x <= centerTileX + 3; x++)
                    {
                        if (!WorldGen.InWorld(x, y, 1))
                        {
                            continue;
                        }

                        Tile supportTile = Main.tile[x, y];
                        if (!supportTile.HasTile || supportTile.IsActuated)
                        {
                            continue;
                        }

                        bool treeBrace = TileID.Sets.IsATreeTrunk[supportTile.TileType] || supportTile.TileType == TileID.PalmTree;
                        if (treeBrace || IsSolidTile(x, y))
                        {
                            hasBrace = true;
                            break;
                        }
                    }
                }

                if (!hasBrace)
                {
                    hugGround = true;
                }
            }

            // Ground-hug target: the centre height that rests this piece on the floor under it. NaN = no lean (not
            // hugging, or no ground in reach -- a gap or open water -- so the chain spans it).
            // Anchor the search to Center.Y rather than bottom edge so a sub-pixel vertical shift near a tile boundary
            // cannot flip startTileY between adjacent rows and vibrate on slopes.
            float groundCenterY = float.NaN;
            if (hugGround)
            {
                int centerTileX = (int)(npc.Center.X / TileSize);
                int centerTileY = (int)(npc.Center.Y / TileSize);
                int groundTileY = FindGroundSurfaceTileYSmoothed(centerTileX, centerTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 2 + 1);
                if (groundTileY < 0)
                {
                    groundTileY = FindGroundSurfaceTileYSmoothed(centerTileX, centerTileY, 16);
                }

                if (groundTileY >= 0)
                {
                    //A horizontal taper rests on its width; a vertical climbing piece still needs
                    //clearance for its full length. This preserves supported circular bends.
                    float verticalLength = (float)Math.Abs(Math.Cos(npc.rotation)) * npc.height;
                    float verticalWidth = (float)Math.Abs(Math.Sin(npc.rotation)) * npc.width;
                    float halfHeight = (verticalLength + verticalWidth) * 0.5f;
                    groundCenterY = groundTileY * TileSize - halfHeight;
                }
            }

            // The ahead piece has already updated this tick (NPC index order = chain order), so bends accumulate
            // down the chain into one smooth curve.
            Vector2 newCenter;
            float movementBudget = SerpentChain.MaxBodyStepPixels;
            if (headNpc != null)
            {
                movementBudget = Math.Max(movementBudget, headNpc.velocity.Length());
            }

            float newHeading = SerpentChain.SolveBodyJoint(
                ahead.Center, ahead.height, ahead.width, ahead.rotation - MathHelper.PiOver2,
                npc.Center, npc.height, npc.width, npc.rotation - MathHelper.PiOver2,
                (int)npc.ai[2], flexibility, curling, curlJointRadians, groundCenterY, BodyTerrainProbe, out newCenter, movementBudget);

            float targetRotation = newHeading + MathHelper.PiOver2;

            // At rest on a slope, suppress sub-pixel / sub-degree hunting between quantized tile heights and link angles.
            if (headStationary && !IsBodyCenterInsideTerrain(npc.Center))
            {
                float rotationDelta = Math.Abs(MathHelper.WrapAngle(targetRotation - npc.rotation));
                float positionDeltaSq = Vector2.DistanceSquared(newCenter, npc.Center);
                if (rotationDelta < 0.035f && positionDeltaSq < 0.65f)
                {
                    npc.velocity = default;
                    return;
                }
            }

            if (headData != null && DebugLogging && !Main.dedServ)
            {
                float actualStep = Vector2.Distance(npc.Center, newCenter);
                if (actualStep > headData.DebugBodyMaxStep)
                {
                    headData.DebugBodyMaxStep = actualStep;
                    headData.DebugBodyMaxStepIndex = (int)npc.ai[2];
                    headData.DebugBodyMaxStepTick = Main.GameUpdateCount;
                }

                float actualTurn = Math.Abs(MathHelper.WrapAngle(targetRotation - npc.rotation));
                headData.DebugBodyMaxTurn = Math.Max(headData.DebugBodyMaxTurn, actualTurn);
            }

            npc.rotation = targetRotation;
            npc.velocity = default;
            npc.Center = newCenter;
        }

        ///<summary>
        ///Stops the head at solid tiles. The head runs noTileCollide (so the engine integrates velocity straight through
        ///walls), and locomotion also writes velocity from many branches, so this runs once after all of them: if the
        ///head's end-of-tick box would sit inside terrain, it is swept from where it started and slides along whichever
        ///axes are free. A head that already starts embedded is left alone so it can leave.
        ///</summary>
        static void BlockHeadAtTerrain(NPC npc, Vector2 positionBefore)
        {
            Vector2 intendedEnd = npc.position + npc.velocity;
            Vector2 intendedMove = intendedEnd - positionBefore;
            if (intendedMove == Vector2.Zero)
            {
                return;
            }

            bool endsInside = Collision.SolidCollision(intendedEnd, npc.width, npc.height);
            if (!endsInside)
            {
                return;
            }

            bool startedInside = Collision.SolidCollision(positionBefore, npc.width, npc.height);
            if (startedInside)
            {
                return;
            }

            Vector2 allowedMove = Collision.TileCollision(positionBefore, intendedMove, npc.width, npc.height, true, true);
            npc.position = positionBefore;
            npc.velocity = allowedMove;
        }

        const float TailExtendRate = 0.045f; //~1/rate ticks to fully pop out or retract

        ///<summary>Idles at 0 (hidden); the instant a tail attack starts, ramps to 1 so the tail pokes out
        ///smoothly through the Coiling windup, then eases back to 0 once the attack ends.</summary>
        static void UpdateTailExtend(GreatSerpentHead data)
        {
            float target = 0f;
            if (data.TailStab != GreatSerpentHead.TailStabState.None || data.TailFlickAmount > 0f)
            {
                target = 1f;
            }
            data.TailExtend = MathHelper.Lerp(data.TailExtend, target, TailExtendRate);
        }

        ///<summary>Decorative sine offset for front-free segments while an idle ripple wave is playing.</summary>
        static void ApplyRippleOffset(NPC npc, int segmentIndex, GreatSerpentHead headData)
        {
            float elapsed = RippleDurationTicks - headData.RippleTimer;
            float fade = headData.RippleTimer / (float)RippleDurationTicks;
            float wave = (float)Math.Sin((elapsed - segmentIndex * RippleSegmentPhaseOffset) * RippleFrequency);
            npc.position.Y += wave * RippleAmplitude * fade;
        }

        ///<summary>
        ///Grounded terrain-aware head locomotion, replacing AIWorm's velocity/gravity head branch entirely.
        ///State priority: stagger flop > burrow > charge > swim > normal ground movement (climb/walk/too-tall).
        ///</summary>
        static void RunHeadLocomotion(NPC npc, float maxSpeed)
        {
            GreatSerpentHead data = npc.ModNPC as GreatSerpentHead;
            tsorcRevampGlobalNPC poise = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Player player = Main.player[npc.target];

            //Scripted death takes over everything else the instant CheckDead() intercepts the killing blow.
            if (data.IsDying)
            {
                RunDeathFade(npc, data);
                return;
            }

            //Contact damage is OFF by default; only specific attack states below turn it back on.
            npc.damage = 0;

            //Passive tail flick curl amount: eases toward 1 while a flick is wanted and back to 0 otherwise. Runs
            //before the stagger/passive branches so a flick interrupted by a hit or a stagger still lays itself
            //back down instead of leaving the tail frozen up in the air.
            if (data.TailFlickWanted)
            {
                data.TailFlickTimer++;
                data.TailFlickAmount = Math.Min(1f, data.TailFlickAmount + 1f / TailFlickRiseTicks);
            }
            else
            {
                data.TailFlickAmount = Math.Max(0f, data.TailFlickAmount - 1f / TailFlickLowerTicks);
            }

            if (poise.StaggerTimer > 0)
            {
                RunStaggerFlop(npc);
                return;
            }

            UpdateAcidBody(npc, data);
            UpdateTailExtend(data);

            if (data.ChargeCooldown > 0) { data.ChargeCooldown--; }
            if (data.RippleCooldown > 0) { data.RippleCooldown--; }
            if (data.AttackCooldown > 0) { data.AttackCooldown--; }
            if (data.TailStabCooldown > 0) { data.TailStabCooldown--; }
            if (data.CrossOverCooldown > 0) { data.CrossOverCooldown--; }
            if (data.CloakCooldown > 0) { data.CloakCooldown--; }

            //Passive until struck. Any drop in the head's life (body hits reach it through realLife) provokes it for
            //good; after that every branch below runs exactly as it always has. The authority decides, clients follow
            //the synced Provoked flag. HP is re-baselined for the first PassiveGraceTicks so a spawn-time max-life
            //rescale can't read as a hit.
            if (!data.Provoked)
            {
                if (data.PassiveAge < PassiveGraceTicks)
                {
                    data.PassiveAge++;
                    data.PassiveLifeBaseline = npc.life;
                }

                bool struck = npc.life < data.PassiveLifeBaseline;
                if (struck && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    data.Provoked = true;
                    data.TailFlickWanted = false; //any curl in progress lays itself back down while it gives chase
                    npc.boss = true;
                    npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
                }
            }

            if (!data.Provoked)
            {
                RunPassive(npc, data);
                SerpentLog(npc, data, player);
                return;
            }

            //While busy with a deliberate attack/hold, the head isn't "stuck" -- reset the detector so it doesn't
            //spuriously arm the forced-wander recovery mid-attack (which the log showed happening during the tail stab).
            bool busy = data.TailStab != GreatSerpentHead.TailStabState.None
                || data.Attack != GreatSerpentHead.AttackState.None
                || data.ChargeTelegraphTimer > 0 || data.ChargeTimer > 0
                || data.Ambush != GreatSerpentHead.AmbushState.None;
            if (busy)
            {
                data.StuckTimer = 0;
                data.StuckCheckPos = npc.Center;
            }

            //Predator-style hunting cloak: cloaks the instant it loses line of sight to the player while
            //actively hunting (not busy with an attack/tailstab/charge/ambush, and not mid-forced-wander), and
            //only reveals again once LOS is reacquired -- a clean, deterministic transition, NOT a random
            //per-frame blink (that's the VoodooShaman style, explicitly not wanted here). CloakCooldown then
            //holds it off before it's allowed to cloak again, so a reveal can't immediately re-trigger on the
            //next LOS flicker. RunAmbush drives CloakAlphaTarget itself while active, so leave it alone here.
            if (data.Ambush == GreatSerpentHead.AmbushState.None)
            {
                bool hasLOS = Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height);
                if (data.CloakAlphaTarget > 0)
                {
                    if (hasLOS)
                    {
                        data.CloakAlphaTarget = 0;
                        data.CloakCooldown = CloakCooldownTicks;
                    }
                }
                else if (!hasLOS && !busy && data.StuckWanderTimer <= 0 && data.CloakCooldown <= 0)
                {
                    data.CloakAlphaTarget = IdleCloakAlpha;
                }
            }
            npc.alpha = data.CloakAlphaTarget;

            //Retreat-then-sneak-attack: once per 30%-of-max-HP step lost, vanish, sneak back in, and reveal
            //itself right as the next attack's telegraph starts. Never interrupts an attack/tailstab/forced-
            //wander already in progress -- only arms between them.
            int thresholdsCrossed = (int)((1f - npc.life / (float)npc.lifeMax) / AmbushHpThresholdFraction);
            //Server arms it: the HP step is the same number everywhere, but a client's copy of npc.life lags, so two
            //machines would start the vanish on different ticks and the reveal (which forces an attack) would drift.
            if (thresholdsCrossed > data.AmbushThresholdsTriggered && data.Ambush == GreatSerpentHead.AmbushState.None
                && data.TailStab == GreatSerpentHead.TailStabState.None && data.Attack == GreatSerpentHead.AttackState.None
                && data.StuckWanderTimer <= 0 && data.ChargeTelegraphTimer <= 0 && data.ChargeTimer <= 0
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                data.AmbushThresholdsTriggered = thresholdsCrossed;
                data.Ambush = GreatSerpentHead.AmbushState.Vanish;
                data.AmbushTimer = AmbushVanishTicks;
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
            }

            if (data.Ambush != GreatSerpentHead.AmbushState.None)
            {
                RunAmbush(npc, data, player);
                data.LastAction = "ambush-" + data.Ambush;
                SerpentLog(npc, data, player);
                return;
            }

            //Unreachable tracking: if it can neither see nor navigate to the player within NavSearchRadius, it
            //eventually wanders and then despawns. Only counts when NOT mid-attack or actively following a nav plan.
            float distanceToPlayerPx = Vector2.Distance(npc.Center, player.Center);
            bool hasActivePlan = data.Plan != null && data.PlanIndex < data.Plan.Count;
            bool withinNavRadius = distanceToPlayerPx <= NavSearchRadius * TileSize;
            bool hasDirectSight = Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height)
                && distanceToPlayerPx <= AggroRangeTiles * TileSize;
            bool canReach = hasDirectSight || hasActivePlan || withinNavRadius;

            if (canReach || busy)
            {
                data.UnreachableTimer = 0;
            }
            else
            {
                data.UnreachableTimer++;
            }

            //Removing the boss is the server's call; clients get it through the normal NPC sync. UnreachableTimer is
            //synced so every machine reaches the threshold together, but a client must never retire it on its own.
            if (data.UnreachableTimer >= DespawnTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                DespawnPoof(npc);
                return;
            }
            if (!busy && data.StuckWanderTimer <= 0 && data.UnreachableTimer >= WanderStartTicks)
            {
                RunWander(npc, data);
                SerpentLog(npc, data, player);
                return;
            }

            //Overhead tail stab is its own head-state: the head holds on the ground (body settles) while the
            //rear half rears up and arcs over to strike. Runs to completion before the head does anything else.
            if (data.TailStab != GreatSerpentHead.TailStabState.None)
            {
                UpdateTailStab(npc, data, player);
                HoldGround(npc, player);
                data.LastAction = "tailstab-" + data.TailStab;
                SerpentLog(npc, data, player);
                return;
            }

            //Failsafe: wedged / marooned -> forced Wander for a while. This NEVER clips through terrain (unlike
            //the old Unstick failsafe it replaced, which phased straight at the player through walls -- removed
            //entirely per feedback: the serpent must never clip through solid tiles, not even as an escape
            //hatch). RunWander already fully respects terrain (turns at walls/cliffs), so reusing it here is a
            //safe, terrain-honest way to guarantee it can never be permanently marooned.
            if (data.StuckWanderTimer > 0)
            {
                data.StuckWanderTimer--;
                RunWander(npc, data);
                SerpentLog(npc, data, player);
                return;
            }

            if (!busy)
            {
                UpdateStuckDetector(npc, data, player);
            }

            if (data.Attack != GreatSerpentHead.AttackState.None)
            {
                RunAttack(npc, data, poise);
                data.LastAction = "attack-" + data.Attack;
                SerpentLog(npc, data, player);
                return;
            }

            if (data.ChargeTelegraphTimer > 0 || data.ChargeTimer > 0)
            {
                RunCharge(npc, data, maxSpeed);
                data.LastAction = data.ChargeTelegraphTimer > 0 ? "charge-telegraph" : "charge";
                SerpentLog(npc, data, player);
                return;
            }

            if (IsInWater(npc))
            {
                RunSwim(npc, data, player, maxSpeed);
                data.LastAction = "swim";
                SerpentLog(npc, data, player);
                return;
            }

            RunGroundMovement(npc, data, player, maxSpeed);
            SerpentLog(npc, data, player);
        }

        ///<summary>
        ///Watches the head's own position. If it hasn't meaningfully moved for StuckSampleTicks while it still
        ///wants to be doing something (i.e. it isn't legitimately kiting-with-LOS), arm a forced-Wander window
        ///(StuckWanderTimer). Without this the serpent can be permanently marooned: it only ever rides the
        ///surface beneath it, so once it climbs into a spot it can't progress from there is no move that gets
        ///it out. This NEVER phases through terrain (see the StuckWanderTimer dispatch in RunHeadLocomotion) --
        ///a prior version of this failsafe (Unstick) did clip through walls and was removed per feedback.
        ///</summary>
        static void UpdateStuckDetector(NPC npc, GreatSerpentHead data, Player player)
        {
            //Legitimately holding station: in kite range with a clear line to the player = not stuck, just waiting.
            bool engaged = Math.Abs(player.Center.X - npc.Center.X) <= KiteRangeTiles * TileSize
                && Math.Abs(player.Center.Y - npc.Center.Y) <= KiteRangeTiles * TileSize
                && Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height);

            if (engaged || Vector2.Distance(npc.Center, data.StuckCheckPos) > StuckMoveThreshold)
            {
                data.StuckCheckPos = npc.Center;
                data.StuckTimer = 0;
                return;
            }

            data.StuckTimer++;
            if (data.StuckTimer >= StuckSampleTicks)
            {
                data.StuckTimer = 0;
                data.StuckCheckPos = npc.Center;
                data.StuckWanderTimer = StuckWanderDurationTicks;
                //Going stuck means the current nav plan (if any) demonstrably failed -- drop it and bad-edge
                //the step it died on so the post-wander replan routes around it.
                SmartSerpent4AI.OnStuckRecoveryArmed(data);
                npc.netUpdate = true;
            }
        }

        ///<summary>Wander: slow ground pacing (no attacks) once the player is unreachable, turning at walls/cliffs.
        ///Climbs 1..MaxClimbHeightTiles steps so recovery-wander never gets wedged on a 1-2 tile step.</summary>
        static void RunWander(NPC npc, GreatSerpentHead data, float maxSpeed = 4.5f)
        {
            data.LastAction = "wander";
            int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int centerTileX = (int)(npc.Center.X / TileSize);
            int headTileY = (int)(npc.position.Y / TileSize);
            bool hasHeadroom = !IsSolidTile(centerTileX, headTileY - 1) && !IsSolidTile(centerTileX, headTileY - 2);

            int obsNear = GetObstacleHeightAhead(centerTileX + data.WanderDir, feetTileY, MaxClimbHeightTiles + 2);
            int obsFar = GetObstacleHeightAhead(centerTileX + data.WanderDir * SmallStepTiles, feetTileY, MaxClimbHeightTiles + 2);
            int obstacleHeight = Math.Max(obsNear, obsFar);
            int aheadGround = FindGroundSurfaceTileYSmoothed(centerTileX + data.WanderDir * 2, feetTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 3);

            bool canClimb = obstacleHeight >= 1 && obstacleHeight <= MaxClimbHeightTiles && hasHeadroom && data.ClimbBudget > 0;
            if ((obstacleHeight > 0 && !canClimb) || aheadGround < 0)
            {
                data.WanderDir = -data.WanderDir;
            }

            data.Facing = data.WanderDir;
            npc.direction = data.WanderDir;
            npc.velocity.X = MathHelper.Lerp(npc.velocity.X, data.WanderDir * maxSpeed * WanderSpeedMul, 0.08f);

            if (canClimb)
            {
                npc.velocity.Y = -ClimbRiseSpeed;
                data.ClimbBudget--;
            }
            else
            {
                SnapHeadToGround(npc, GroundFollowLerp);
                data.ClimbBudget = ClimbBudgetTicks;
            }

            float aimLookX = data.Facing;
            if (Math.Abs(npc.velocity.X) > 0.4f)
            {
                aimLookX = npc.velocity.X;
            }

            AimHead(npc, aimLookX, npc.velocity.Y);
        }

        ///<summary>
        ///Pre-provocation behaviour (see GreatSerpentHead.Provoked): Wander (slow terrain-following amble that climbs
        ///small steps) -> Halt (brake) -> Turn (head rears up and swings back over its own neck) -> Rest (head laid
        ///down for PassiveRestTicks, tail flicking into a C every 120-280 ticks) -> Wander the other way. Never attacks,
        ///never does contact damage (the caller already zeroed it), never despawns for being unreachable.
        ///<para/>
        ///The authority (server/singleplayer) rolls every random value and makes every phase change; clients run the
        ///same per-tick motion off the synced phase + timers and wait for the next snapshot to change phase.
        ///</summary>
        static void RunPassive(NPC npc, GreatSerpentHead data)
        {
            bool isAuthority = Main.netMode != NetmodeID.MultiplayerClient;
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            //Keep the hunting systems quiet: nothing here may arm the stuck-wander, the unreachable->despawn clock
            //or the LOS cloak, all of which key off "can't reach the player" and would misfire on a docile snake.
            data.UnreachableTimer = 0;
            data.StuckTimer = 0;
            data.StuckCheckPos = npc.Center;
            data.CloakAlphaTarget = 0;
            npc.alpha = 0;

            if (!data.PassiveStarted && isAuthority)
            {
                data.PassiveStarted = true;
                data.WanderDir = Main.rand.NextBool() ? 1 : -1;
                data.Passive = GreatSerpentHead.PassivePhase.Wander;
                data.PassiveTimer = Main.rand.Next(PassiveLegMinTicks, PassiveLegMaxTicks + 1);
                data.ClimbBudget = ClimbBudgetTicks; //starts at 0 otherwise, which would read the first climbable step as a wall
                globalNPC.RequestNetworkSnapshot();
            }

            //Counts down in every phase; clamped at 0 so a client waiting on the authority's next snapshot can't run it negative.
            data.PassiveTimer = Math.Max(data.PassiveTimer - 1, 0);

            int centerTileX = (int)(npc.Center.X / TileSize);
            int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);

            switch (data.Passive)
            {
                case GreatSerpentHead.PassivePhase.Wander:
                {
                    data.LastAction = "passive-wander";
                    int dir = data.WanderDir;
                    data.Facing = dir;
                    npc.direction = dir;

                    int obsNear = GetObstacleHeightAhead(centerTileX + dir, feetTileY, MaxClimbHeightTiles + 2);
                    int obsFar = GetObstacleHeightAhead(centerTileX + dir * SmallStepTiles, feetTileY, MaxClimbHeightTiles + 2);
                    int obstacleHeight = Math.Max(obsNear, obsFar);
                    if (obstacleHeight > SmallStepTiles)
                    {
                        int obstacleHeightNext = GetObstacleHeightAhead(centerTileX + dir * (SmallStepTiles + 1), feetTileY, MaxClimbHeightTiles + 2);
                        if (obstacleHeightNext <= SmallStepTiles)
                        {
                            obstacleHeight = 0;
                        }
                    }

                    int aheadGroundTileY = FindGroundSurfaceTileYSmoothed(centerTileX + dir * 2, feetTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 3);
                    int headTileY = (int)(npc.position.Y / TileSize);
                    bool hasHeadroom = !IsSolidTile(centerTileX, headTileY - 1) && !IsSolidTile(centerTileX, headTileY - 2);

                    bool stepUp = obstacleHeight >= 1 && obstacleHeight <= SmallStepTiles && hasHeadroom;
                    bool climbable = obstacleHeight > SmallStepTiles && obstacleHeight <= MaxClimbHeightTiles && hasHeadroom && data.ClimbBudget > 0;
                    bool blocked = (obstacleHeight > 0 && !stepUp && !climbable) || aheadGroundTileY < 0;

                    if (isAuthority && (blocked || data.PassiveTimer <= 0))
                    {
                        data.Passive = GreatSerpentHead.PassivePhase.Halt;
                        data.PassiveTimer = PassiveHaltMaxTicks;
                        globalNPC.RequestNetworkSnapshot();
                    }

                    if (stepUp || climbable)
                    {
                        npc.velocity.X = MathHelper.Lerp(npc.velocity.X, dir * PassiveWanderSpeed * 0.7f, 0.06f);
                        npc.velocity.Y = -ClimbRiseSpeed;
                        if (climbable)
                        {
                            data.ClimbBudget--;
                        }
                    }
                    else if (blocked)
                    {
                        //Don't push into the wall/edge while the authority's Halt arrives.
                        npc.velocity.X = MathHelper.Lerp(npc.velocity.X, 0f, 0.1f);
                        SnapHeadToGround(npc, GroundFollowLerp);
                    }
                    else
                    {
                        npc.velocity.X = MathHelper.Lerp(npc.velocity.X, dir * PassiveWanderSpeed, 0.06f);
                        SnapHeadToGround(npc, GroundFollowLerp);
                        data.ClimbBudget = ClimbBudgetTicks;
                    }

                    float passiveLookX = dir;
                    if (Math.Abs(npc.velocity.X) > 0.4f)
                    {
                        passiveLookX = npc.velocity.X;
                    }

                    AimHead(npc, passiveLookX, npc.velocity.Y);
                    break;
                }

                case GreatSerpentHead.PassivePhase.Halt:
                {
                    data.LastAction = "passive-halt";
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, 0f, 0.08f);
                    SnapHeadToGround(npc, GroundFollowLerp);
                    AimHead(npc, data.WanderDir, npc.velocity.Y);

                    bool stopped = Math.Abs(npc.velocity.X) < PassiveHaltStopSpeed || data.PassiveTimer <= 0;
                    if (stopped && isAuthority)
                    {
                        //Size the arc to the headroom: the head rises 2x the radius at the top of the swing, so leave
                        //a tile of margin under the ceiling. A very low roof clamps to the minimum and grazes it.
                        int headTopTileY = (int)(npc.position.Y / TileSize);
                        int clearTiles = 0;
                        while (clearTiles < PassiveTurnClearanceScanTiles && !IsSolidTile(centerTileX, headTopTileY - 1 - clearTiles))
                        {
                            clearTiles++;
                        }

                        float fitRadius = (clearTiles * TileSize - TileSize) * 0.5f;
                        data.PassiveTurnRadius = MathHelper.Clamp(fitRadius, PassiveTurnMinRadius, PassiveTurnMaxRadius);
                        data.PassiveTimer = (int)Math.Ceiling(MathHelper.Pi * data.PassiveTurnRadius / PassiveTurnSpeed);

                        //From here WanderDir is the way it will face (and walk) after the turn; the arc starts the old way.
                        data.WanderDir = -data.WanderDir;
                        data.Passive = GreatSerpentHead.PassivePhase.Turn;
                        globalNPC.RequestNetworkSnapshot();
                    }
                    break;
                }

                case GreatSerpentHead.PassivePhase.Turn:
                {
                    data.LastAction = "passive-turn";

                    //The head follows a half circle: it starts moving the old way, rises, and ends moving the new way
                    //(heading = (oldDir*cos a, -sin a), a = 0..pi). The neck hose-follows that path, folding back over itself.
                    float arcTicks = MathHelper.Pi * data.PassiveTurnRadius / PassiveTurnSpeed;
                    float progress = 1f;
                    if (arcTicks >= 1f)
                    {
                        progress = MathHelper.Clamp(1f - data.PassiveTimer / arcTicks, 0f, 1f);
                    }

                    float arcAngle = MathHelper.Pi * progress;
                    int oldDir = -data.WanderDir;
                    Vector2 heading = new Vector2(oldDir * (float)Math.Cos(arcAngle), -(float)Math.Sin(arcAngle));
                    npc.velocity = heading * PassiveTurnSpeed;

                    //Rotation is driven straight off the heading so the sprite sweeps UP through vertical (AimHead
                    //would take the shortest way round, through the ground). The sprite mirrors at the top of the arc,
                    //where the head points straight up and a horizontal flip is seamless.
                    npc.rotation = heading.ToRotation() + 1.57f;
                    int arcFacing = oldDir;
                    if (arcAngle >= MathHelper.PiOver2)
                    {
                        arcFacing = data.WanderDir;
                    }
                    npc.spriteDirection = -arcFacing;
                    npc.direction = arcFacing;
                    data.Facing = arcFacing;

                    if (isAuthority && data.PassiveTimer <= 0)
                    {
                        data.Passive = GreatSerpentHead.PassivePhase.Rest;
                        data.PassiveTimer = PassiveRestTicks;
                        data.PassiveFlickGapTimer = Main.rand.Next(PassiveFlickGapMinTicks, PassiveFlickGapMaxTicks + 1);
                        globalNPC.RequestNetworkSnapshot();
                    }
                    break;
                }

                case GreatSerpentHead.PassivePhase.Rest:
                {
                    data.LastAction = "passive-rest";
                    data.Facing = data.WanderDir;
                    npc.direction = data.WanderDir;

                    //Ease down off the top of the turn onto the ground, then lie there with the snout drooping a little.
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, 0f, 0.1f);
                    SnapHeadToGround(npc, PassiveRestLowerLerp);
                    AimHead(npc, data.WanderDir, PassiveRestHeadDrop);

                    int flickHeldTicks = TailFlickRiseTicks + TailFlickHoldTicks;
                    if (data.TailFlickWanted)
                    {
                        //Held long enough -> let go; the curl amount then eases back down on its own (RunHeadLocomotion).
                        if (isAuthority && data.TailFlickTimer >= flickHeldTicks)
                        {
                            data.TailFlickWanted = false;
                            data.TailFlickTimer = 0;
                            data.PassiveFlickGapTimer = Main.rand.Next(PassiveFlickGapMinTicks, PassiveFlickGapMaxTicks + 1);
                            globalNPC.RequestNetworkSnapshot();
                        }
                    }
                    else
                    {
                        data.PassiveFlickGapTimer = Math.Max(data.PassiveFlickGapTimer - 1, 0);

                        //Only start one that can finish (rise + hold + lower) before the rest does, and only from a settled tail.
                        bool roomToFlick = data.PassiveTimer > flickHeldTicks + TailFlickLowerTicks;
                        bool tailSettled = data.TailFlickAmount <= 0f;
                        if (isAuthority && data.PassiveFlickGapTimer <= 0 && roomToFlick && tailSettled)
                        {
                            //The body trails away from the way the head now faces (it just folded back over itself),
                            //so "up" is a negative bend when the tail trails right and a positive one when it trails left.
                            data.TailFlickSign = -data.WanderDir;
                            data.TailFlickWanted = true;
                            data.TailFlickTimer = 0;
                            globalNPC.RequestNetworkSnapshot();
                        }
                    }

                    //Don't leave mid-flick: wait for the tail to be fully laid back down so walking never starts from a raised C.
                    bool restOver = data.PassiveTimer <= 0 && !data.TailFlickWanted && data.TailFlickAmount <= 0f;
                    if (isAuthority && restOver)
                    {
                        data.Passive = GreatSerpentHead.PassivePhase.Wander;
                        data.PassiveTimer = Main.rand.Next(PassiveLegMinTicks, PassiveLegMaxTicks + 1);
                        data.ClimbBudget = ClimbBudgetTicks;
                        globalNPC.RequestNetworkSnapshot();
                    }
                    break;
                }
            }
        }

        ///<summary>Give-up despawn: a dust poof and gone (kept separate from the boss's all-players-dead handler).</summary>
        static void DespawnPoof(NPC npc)
        {
            //Dust burst disabled per feedback -- the give-up despawn shouldn't explode into dust.
            //if (!Main.dedServ)
            //{
            //    for (int i = 0; i < 40; i++)
            //    {
            //        Vector2 v = Main.rand.NextVector2Circular(8f, 8f);
            //        int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.AncientLight, v.X, v.Y, 150, Color.Purple, 2f);
            //        Main.dust[d].noGravity = true;
            //    }
            //}
            npc.active = false; //body pieces die on their next tick when the head is gone
        }

        ///<summary>
        ///Retreat-then-sneak-attack, armed once per 30% of max HP lost (see the trigger in RunHeadLocomotion).
        ///Vanish: brake, turn away, and retreat while fading into the deep ambush cloak. Reposition: still
        ///cloaked, sneak back toward the player (or wait out a failsafe timer) until close enough to commit.
        ///Ending Reposition FORCE-STARTS a bite/pounce telegraph directly (bypassing the normal attack roll/
        ///cooldown) and reveals in the same tick -- "coming out of invisible at the point of telegraph" is the
        ///whole point of the ambush.
        ///</summary>
        static void RunAmbush(NPC npc, GreatSerpentHead data, Player player)
        {
            data.AmbushTimer--;
            data.CloakAlphaTarget = AmbushCloakAlpha;
            npc.alpha = data.CloakAlphaTarget;

            switch (data.Ambush)
            {
                case GreatSerpentHead.AmbushState.Vanish:
                {
                    Vector2 away = npc.Center - player.Center;
                    if (away.LengthSquared() < 1f) { away = new Vector2(-npc.direction, 0f); }
                    away.Normalize();
                    data.Facing = away.X >= 0f ? 1 : -1;
                    npc.direction = data.Facing;
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, away.X * AmbushMoveSpeed, 0.06f);
                    SnapHeadToGround(npc, GroundFollowLerp);
                    AimHead(npc, npc.velocity.X, npc.velocity.Y);

                    if (data.AmbushTimer <= 0)
                    {
                        data.Ambush = GreatSerpentHead.AmbushState.Reposition;
                        data.AmbushTimer = AmbushRepositionMaxTicks;
                    }
                    break;
                }

                case GreatSerpentHead.AmbushState.Reposition:
                {
                    float dx = player.Center.X - npc.Center.X;
                    data.Facing = dx >= 0f ? 1 : -1;
                    npc.direction = data.Facing;
                    npc.velocity.X = MathHelper.Lerp(npc.velocity.X, data.Facing * AmbushMoveSpeed, 0.05f);
                    SnapHeadToGround(npc, GroundFollowLerp);
                    AimHead(npc, npc.velocity.X, npc.velocity.Y);

                    bool closeEnough = Math.Abs(dx) <= AmbushRepositionRangeTiles * TileSize
                        && Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height);
                    //The reveal picks the attack it bursts out with, so the server owns it; a client holds the
                    //reposition until the snapshot brings the chosen attack.
                    if ((closeEnough || data.AmbushTimer <= 0) && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        data.Ambush = GreatSerpentHead.AmbushState.None;
                        data.CloakAlphaTarget = 0; //reveal exactly as the forced telegraph below begins
                        npc.alpha = 0;

                        bool pounce = Main.rand.NextBool(2);
                        data.Attack = pounce ? GreatSerpentHead.AttackState.PounceTelegraph : GreatSerpentHead.AttackState.BiteTelegraph;
                        data.AttackTimer = pounce ? PounceTelegraphTicks : BiteTelegraphTicks;
                        data.MouthTransitionTimer = GreatSerpentHead.MouthTransitionTicks;
                        data.AttackAnchorY = npc.position.Y;
                        npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
                    }
                    break;
                }
            }
        }

        ///<summary>Scripted death: fade the head to fully invisible while spraying blood, then finish the REAL
        ///kill (loot/OnKill/etc) once the fade completes. See GreatSerpentHead.CheckDead for the intercept.</summary>
        static void RunDeathFade(NPC npc, GreatSerpentHead data)
        {
            npc.velocity = Vector2.Lerp(npc.velocity, Vector2.Zero, 0.1f);
            npc.damage = 0;
            data.LastAction = "dying";

            float progress = 1f - data.DeathFadeTimer / (float)DeathFadeDuration;
            npc.alpha = (int)MathHelper.Clamp(255f * progress, 0f, 255f);
            SprayHeadDeathBlood(npc);

            data.DeathFadeTimer--;
            if (data.DeathFadeTimer <= 0)
            {
                npc.life = 0;
                npc.checkDead(); //finishes the real kill now that the fade has played out -- CheckDead() lets it through this time
            }
        }

        static void SprayHeadDeathBlood(NPC npc)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 5; i++)
            {
                Vector2 v = Main.rand.NextVector2Circular(5f, 5f);
                int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, v.X, v.Y, 0, default, 2f);
                Main.dust[d].noGravity = false;
            }
        }

        static void SpraySegmentDeathBlood(NPC npc)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                Vector2 v = Main.rand.NextVector2Circular(4f, 4f);
                int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.Blood, v.X, v.Y, 0, default, 1.6f);
                Main.dust[d].noGravity = false;
            }
        }

        ///<summary>Head holds its ground position (used while the tail stab plays out): face the player, brake to
        ///a stop, and keep hugging the surface. The front body strings out along the ground behind it.</summary>
        static void HoldGround(NPC npc, Player player)
        {
            npc.velocity.X *= 0.8f;
            if (Math.Abs(player.Center.X - npc.Center.X) > DirDeadzoneTiles * TileSize)
            {
                npc.direction = player.Center.X >= npc.Center.X ? 1 : -1;
            }
            SnapHeadToGround(npc, GroundFollowLerp);
            //Face the player horizontally, level (not rotation 0, which points the head straight up).
            AimHead(npc, player.Center.X - npc.Center.X, 0f);
        }

        static void RunGroundMovement(NPC npc, GreatSerpentHead data, Player player, float maxSpeed)
        {
            bool isLowHealth = npc.life <= npc.lifeMax / 2;
            float effectiveMaxSpeed = maxSpeed;
            float activeKiteRange = KiteRangeTiles;
            float activeFarBoostMax = FarSpeedBoostMax;
            float horizontalLerp = 0.11f;

            if (isLowHealth)
            {
                effectiveMaxSpeed = maxSpeed * LowHealthSpeedMultiplier;
                activeKiteRange = LowHealthKiteRangeTiles;
                activeFarBoostMax = LowHealthFarSpeedBoostMax;
                horizontalLerp = 0.16f;
            }

            //SmartSerpent4AI plan layer: when the player isn't directly engageable (no LOS, or a different
            //room/floor), an A* plan over the span graph picks the next waypoint and the reactive logic below
            //steers at THAT instead of the raw player position. With no plan this whole method behaves exactly
            //as it did before planning existed (steerTarget == player.Center). See NPCs/SmartSerpent4AI.cs.
            bool hasPlanStep = SmartSerpent4AI.UpdateAndSteer(npc, data, player, out Vector2 steerTarget, out SmartSerpent4AI.StepKind stepKind);
            if (!hasPlanStep)
            {
                steerTarget = player.Center;
            }

            float targetDx = steerTarget.X - npc.Center.X;
            float absTargetDx = Math.Abs(targetDx);

            //Facing hysteresis: only flip once the target is clearly to one side. Prevents the left/right
            //jitter (and the resulting body thrash) when the player sits right on top of the head's X.
            if (absTargetDx > DirDeadzoneTiles * TileSize)
            {
                if (targetDx >= 0f)
                {
                    data.Facing = 1;
                }
                else
                {
                    data.Facing = -1;
                }
            }
            npc.direction = data.Facing;

            //Decide horizontal intent: pursue / kite-stop / cross-over. It NEVER intentionally reverses.
            //While executing a plan step, kiting/cross-over don't apply (those are close-quarters behaviours;
            //the plan layer already disengages itself when the player is directly engageable).
            float desiredX;
            if (hasPlanStep)
            {
                float planBoost = isLowHealth ? 1.25f : 1.1f;
                if (absTargetDx > TileSize)
                {
                    desiredX = data.Facing * effectiveMaxSpeed * planBoost;
                }
                else
                {
                    desiredX = 0f;
                }
            }
            else if (data.CrossingOver)
            {
                //Committed to crossing through to the far side: keep going in the crossover direction until
                //well past the player, then resume kiting from the new side (and favor an attack there).
                desiredX = data.CrossOverDir * effectiveMaxSpeed * 1.15f;
                bool past = (data.CrossOverDir > 0 && npc.Center.X > player.Center.X + CrossOverExitTiles * TileSize)
                          || (data.CrossOverDir < 0 && npc.Center.X < player.Center.X - CrossOverExitTiles * TileSize);
                if (past)
                {
                    data.CrossingOver = false;
                    data.CrossOverCooldown = CrossOverCooldownTicks;
                    data.AttackCooldown = Math.Min(data.AttackCooldown, 15); //strike soon from the new side
                }
            }
            else if (absTargetDx <= activeKiteRange * TileSize)
            {
                //In kite range: stop advancing (don't back up), hold and attack. Occasionally commit to a cross.
                desiredX = 0f;
                if (absTargetDx <= CrossOverTriggerTiles * TileSize && data.CrossOverCooldown <= 0
                    && data.TailStab == GreatSerpentHead.TailStabState.None && Main.rand.NextBool(CrossOverTriggerRoll)
                    && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    data.CrossingOver = true;
                    data.CrossOverDir = data.Facing; //forward, through the player, to the far side
                    npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
                }
            }
            else
            {
                //Closing distance: scales up to activeFarBoostMax extra speed (much stronger below 50% HP).
                float extraTiles = MathHelper.Clamp(absTargetDx / TileSize - activeKiteRange, 0f, FarSpeedBoostRangeTiles);
                float boost = 1f + (extraTiles / FarSpeedBoostRangeTiles) * activeFarBoostMax;
                desiredX = data.Facing * effectiveMaxSpeed * boost;
            }

            int moveDir = data.Facing;
            if (desiredX != 0f)
            {
                moveDir = Math.Sign(desiredX);
            }

            int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int centerTileX = (int)(npc.Center.X / TileSize);

            //Obstacle handling only matters when actually advancing. Check both 1 and 2 tiles ahead so a 50px-wide
            //head never hits a 1-2 tile step undetected.
            int obstacleHeight = 0;
            if (desiredX != 0f)
            {
                int obsNear = GetObstacleHeightAhead(centerTileX + moveDir, feetTileY, MaxClimbHeightTiles + 2);
                int obsFar = GetObstacleHeightAhead(centerTileX + moveDir * SmallStepTiles, feetTileY, MaxClimbHeightTiles + 2);
                obstacleHeight = Math.Max(obsNear, obsFar);
                if (obstacleHeight > SmallStepTiles)
                {
                    int obstacleHeightNext = GetObstacleHeightAhead(centerTileX + moveDir * (SmallStepTiles + 1), feetTileY, MaxClimbHeightTiles + 2);
                    if (obstacleHeightNext <= SmallStepTiles)
                    {
                        obstacleHeight = 0; //ignore lone 1-wide spikes
                    }
                }
            }

            //Headroom above the head: if solid sits right above us we must NOT keep rising, or we bore straight
            //up into a ceiling and end up marooned in the room above.
            int headTileY = (int)(npc.position.Y / TileSize);
            bool hasHeadroom = !IsSolidTile(centerTileX, headTileY - 1) && !IsSolidTile(centerTileX, headTileY - 2);
            bool isSmallStep = desiredX != 0f && obstacleHeight >= 1 && obstacleHeight <= SmallStepTiles && hasHeadroom;

            if (isSmallStep)
            {
                //Step smoothly up over 1-2 tile steps so BlockHeadAtTerrain never wedges horizontal motion.
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, desiredX, horizontalLerp);
                npc.velocity.Y = -ClimbRiseSpeed;
                data.LastAction = isLowHealth ? "pursue-enraged" : "pursue";
            }
            else if (obstacleHeight > SmallStepTiles && obstacleHeight <= MaxClimbHeightTiles && hasHeadroom && data.ClimbBudget > 0)
            {
                //Climbing: rise while advancing. Budget-limited so a mis-read wall can't send it into orbit.
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, desiredX * 0.7f, horizontalLerp);
                npc.velocity.Y = -ClimbRiseSpeed * (isLowHealth ? 1.25f : 1f);
                data.ClimbBudget--;
                data.LastAction = "climb";
            }
            else if (obstacleHeight > SmallStepTiles && steerTarget.Y < npc.Center.Y - 4 * TileSize
                     && !IsSolidTile(centerTileX, headTileY - 1))
            {
                //Tall wall AND the target (the player, or a plan waypoint on the ledge) is up above it: climb
                //the wall face (hug it, rise), not just give up.
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, moveDir * 1.6f, 0.12f);
                npc.velocity.Y = -WallClimbSpeed;
                bool wallStillAtHead = IsSolidTile(centerTileX + moveDir, headTileY) || IsSolidTile(centerTileX + moveDir, headTileY + 1);
                if (!wallStillAtHead)
                {
                    npc.velocity.X = moveDir * effectiveMaxSpeed; //cleared the lip -> move onto the ledge
                    npc.velocity.Y = -1.5f;
                }
                data.LastAction = "wallclimb";
            }
            else if (obstacleHeight > SmallStepTiles)
            {
                //Can't (or shouldn't) climb this: hold and fight from here. Never reverses.
                desiredX = 0f;
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, 0f, 0.1f);
                SnapHeadToGround(npc, GroundFollowLerp);
                data.LastAction = hasHeadroom ? "blocked-tall" : "blocked-ceiling";
            }
            else if (hasPlanStep && stepKind == SmartSerpent4AI.StepKind.SpanGap)
            {
                //SpanGap: bridge the gap with the body -- advance LEVEL toward the far span instead of
                //ground-snapping.
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, desiredX, horizontalLerp);
                npc.velocity.Y = MathHelper.Clamp((steerTarget.Y - npc.Center.Y) * 0.08f, -2f, 2f);
                data.ClimbBudget = ClimbBudgetTicks;
                data.LastAction = "spangap";
            }
            else
            {
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, desiredX, horizontalLerp);
                SnapHeadToGround(npc, GroundFollowLerp);
                //Recharge the climb allowance whenever we're travelling normally on the flat.
                data.ClimbBudget = ClimbBudgetTicks;
                if (desiredX == 0f)
                {
                    data.LastAction = data.CrossingOver ? "cross" : "kite";
                }
                else if (hasPlanStep)
                {
                    data.LastAction = "plan-" + stepKind.ToString().ToLowerInvariant();
                }
                else
                {
                    data.LastAction = isLowHealth ? "pursue-enraged" : "pursue";
                }
            }

            //Head orientation: face travel direction, tilt with the slope, clamped level (never upside-down).
            float aimLookX = data.Facing;
            if (Math.Abs(npc.velocity.X) > 0.4f)
            {
                aimLookX = npc.velocity.X;
            }
            AimHead(npc, aimLookX, npc.velocity.Y);

            //Repeating slither sound -- only while actually moving near its current top speed (not on every twitch).
            if (Math.Abs(npc.velocity.X) >= maxSpeed * MoveSoundSpeedFraction)
            {
                if (data.MoveSoundTimer <= 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item151 with { Volume = 0.35f, PitchVariance = 0.15f }, npc.Center);
                    data.MoveSoundTimer = MoveSoundIntervalTicks;
                }
                else
                {
                    data.MoveSoundTimer--;
                }
            }
            else
            {
                data.MoveSoundTimer = 0; //next time it reaches speed, the cue plays right away
            }

            //Idle flourish
            if (data.RippleCooldown <= 0 && data.RippleTimer <= 0)
            {
                data.RippleTimer = RippleDurationTicks;
                data.RippleCooldown = RippleIdleCooldownTicks;
            }
            if (data.RippleTimer > 0)
            {
                data.RippleTimer--;
            }

            float distanceToPlayer = Vector2.Distance(npc.Center, player.Center);
            float playerAbsDx = Math.Abs(player.Center.X - npc.Center.X);
            int activeChargeRoll = isLowHealth ? ChargeTriggerRoll / 2 : ChargeTriggerRoll;

            //Charge: from medium-far range when not mid-plan or crossing over.
            if (!hasPlanStep && !data.CrossingOver && data.ChargeCooldown <= 0
                && distanceToPlayer >= ChargeMinDistanceTiles * TileSize && Main.rand.NextBool(activeChargeRoll)
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                data.ChargeTelegraphTimer = ChargeTelegraphTicks;
                data.ChargeDirection = new Vector2(data.Facing, 0f);
                data.ChargeCooldown = isLowHealth ? (int)(ChargeCooldownTicks * 0.65f) : ChargeCooldownTicks;
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
            }

            //Tail attack: uses 50% of the body to curve toward the player so the tail can get in range.
            int activeTailRoll = isLowHealth ? Math.Max(8, TailStabTriggerRoll / 2) : TailStabTriggerRoll;
            if (data.TailStab == GreatSerpentHead.TailStabState.None && data.Attack == GreatSerpentHead.AttackState.None
                && data.TailStabCooldown <= 0 && Main.rand.NextBool(activeTailRoll)
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC anchorSeg = FindTailAnchor(npc);
                NPC tail = FindTail(npc);
                Vector2 anchorPos = anchorSeg != null ? anchorSeg.Center : npc.Center;
                float distFromAnchor = Vector2.Distance(anchorPos, player.Center);
                bool hasHeadLOS = Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height);
                bool hasAnchorLOS = anchorSeg != null && Collision.CanHit(anchorSeg.position, anchorSeg.width, anchorSeg.height, player.position, player.width, player.height);

                bool inAnchorReach = !hasPlanStep && distFromAnchor <= TailStabReachTiles * TileSize
                    && playerAbsDx <= (TailStabReachTiles - 2f) * TileSize && (hasHeadLOS || hasAnchorLOS);

                bool straddleCase = false;
                if (tail != null)
                {
                    bool between = (player.Center.X - npc.Center.X) * (player.Center.X - tail.Center.X) < 0f;
                    straddleCase = between && (distFromAnchor <= TailStabReachTiles * TileSize || Vector2.Distance(tail.Center, player.Center) <= TailStabReachTiles * TileSize);
                }

                if (inAnchorReach || straddleCase)
                {
                    StartTailStab(npc, data, player);
                    return;
                }
            }

            TryStartAttack(npc, data, player, distanceToPlayer);
        }

        ///<summary>Shortest-path angle lerp (handles the +/-2pi wrap so the head never spins the long way round).
        ///Proportional to the remaining gap -- fine for cosmetic settling, but NOT for chain joints (see
        ///RotateTowardsAngle): a lerp can still whip an arbitrarily large angle in one tick if the gap is big.</summary>
        static float LerpAngle(float from, float to, float t)
        {
            float delta = MathHelper.WrapAngle(to - from);
            return from + delta * t;
        }

        ///<summary>Turns `from` toward `to` by AT MOST `maxDelta` radians this tick, full stop -- a fixed max
        ///angular speed regardless of how large the mismatch is. This (not LerpAngle) is the correct primitive
        ///for a physical joint: a real hose/neck can't instantly whip around just because its target moved far,
        ///it has to visibly catch up over multiple ticks.</summary>
        static float RotateTowardsAngle(float from, float to, float maxDelta)
        {
            float delta = MathHelper.WrapAngle(to - from);
            delta = MathHelper.Clamp(delta, -maxDelta, maxDelta);
            return from + delta;
        }

        ///<summary>Inverse of the `heading.ToRotation() + 1.57f` convention used throughout this file: given a
        ///rotation, returns the unit "forward" direction it represents.</summary>
        static Vector2 DirectionFromRotation(float rotation)
        {
            float a = rotation - 1.57f;
            return new Vector2((float)Math.Cos(a), (float)Math.Sin(a));
        }

        //The head sprite must never tilt past this from horizontal, or it renders upside-down (the reported bug).
        const float MaxHeadTilt = 0.5f; //radians (~29 deg)

        ///<summary>
        ///Orient the head: face `lookX` horizontally (spriteDirection) and tilt toward `lookY`, but CLAMP the
        ///tilt so the head stays near level and can never flip upside-down. Used for both movement and attacks --
        ///attack projectiles still aim freely at the player, but the head sprite only tilts a little.
        ///<para/>
        ///Turn rate is ALWAYS JointMaxTurnRadians, no per-state exceptions (there used to be a per-caller `ease` lerp
        ///fraction here; that let the head snap its facing far faster than the neck behind it could keep up during fast
        ///states like lunges/charges, which is exactly the "head moves independently of the body" bug).
        ///</summary>
        static void AimHead(NPC npc, float lookX, float lookY)
        {
            int face = Math.Abs(lookX) > 0.05f ? (lookX >= 0f ? 1 : -1) : (npc.spriteDirection == 1 ? -1 : 1);
            npc.spriteDirection = -face; //worm-head convention: facing right -> spriteDirection -1
            float xMag = Math.Max(Math.Abs(lookX), 1f);
            float maxRise = xMag * (float)Math.Tan(MaxHeadTilt);
            float cy = MathHelper.Clamp(lookY, -maxRise, maxRise);
            Vector2 heading = new Vector2(face * xMag, cy);
            npc.rotation = RotateTowardsAngle(npc.rotation, heading.ToRotation() + 1.57f, JointMaxTurnRadians);
        }

        //Support the complete collision footprint, not only its center. Otherwise clearing a step
        //switches to the lower floor too early and alternates climb-up with a downward correction.
        static void SnapHeadToGround(NPC npc, float lerp)
        {
            int leftColumn = (int)((npc.position.X + 1f) / TileSize);
            int rightColumn = (int)((npc.position.X + npc.width - 1f) / TileSize);
            int feetRow = (int)((npc.position.Y + npc.height) / TileSize);
            int startRow = feetRow - GroundSnapToleranceTiles;
            int searchRows = FarGroundSearchTiles + GroundSnapToleranceTiles;
            int supportRow = SerpentChain.FindHeadSupportSurface(leftColumn, rightColumn, startRow, searchRows, HeadSupportProbe);
            if (supportRow < 0)
            {
                npc.velocity.Y = 0f;
                return;
            }

            float targetTop = supportRow * TileSize - npc.height;
            float verticalCorrection = (targetTop - npc.position.Y) * lerp;
            npc.velocity.Y = MathHelper.Clamp(verticalCorrection, -ClimbRiseSpeed, MaxFallbackDescendSpeed);
        }
        //-- AcidBody --

        ///<summary>Below 50% HP: cycle 10s of acid-trailing on / 10s off. Body segments read AcidBodyTimer.</summary>
        static void UpdateAcidBody(NPC npc, GreatSerpentHead data)
        {
            if (npc.life > npc.lifeMax / 2)
            {
                return;
            }
            if (data.AcidBodyTimer > 0)
            {
                data.AcidBodyTimer--;
                if (data.AcidBodyTimer == 0)
                {
                    data.AcidBodyCooldown = AcidBodyCooldownTicks;
                }
            }
            else if (data.AcidBodyCooldown > 0)
            {
                data.AcidBodyCooldown--;
            }
            else
            {
                data.AcidBodyTimer = AcidBodyActiveTicks;
                npc.netUpdate = true;
            }
        }

        ///<summary>Body segments while AcidBody is active: purple dust (throttled -- see AcidBodyDustSkipChance),
        ///and drop a 6s AcidPool on the ground tile under the segment each time it slides onto a new column
        ///AND no pool already covers that spot. localAI[1] remembers the last column this segment dropped on
        ///(localAI[0] is the AIWorm sync counter -- don't touch it).</summary>
        static void ApplyAcidBodyTrail(NPC npc)
        {
            if (!Main.dedServ && Main.rand.NextBool(AcidBodyDustSkipChance))
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.AncientLight, 0f, -0.5f, 150, Color.Purple, 1.0f);
                Main.dust[dust].noGravity = true;
                //AncientLight dust lights up its surroundings on its own (cyan, scaled by dust size). The head is the only part
                //that should glow, so the body's acid trail is kept as particles only.
                Main.dust[dust].noLight = true;
                Main.dust[dust].noLightEmittence = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int centerTileX = (int)(npc.Center.X / TileSize);
            if (centerTileX == (int)npc.localAI[1])
            {
                return;
            }

            //Only drop on ground the segment is actually slithering on (surface within 2 tiles of its bottom)
            int bottomTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int groundTileY = FindGroundSurfaceTileY(centerTileX, bottomTileY - 1, 3);
            if (groundTileY < 0)
            {
                return;
            }

            npc.localAI[1] = centerTileX;

            Vector2 poolCenter = new Vector2(centerTileX * TileSize + TileSize / 2f, groundTileY * TileSize - AcidPool.PoolHeight / 2f + AcidPoolGroundOffset);
            if (AcidPoolExistsNear(poolCenter))
            {
                return; //ground here is already covered -- don't stack another pool (and another dust source) on top
            }
            Projectile.NewProjectile(npc.GetSource_FromAI(), poolCenter, Vector2.Zero, ModContent.ProjectileType<AcidPool>(), AcidBodyPoolDamage, 0f, Main.myPlayer, AcidBodyPoolLifetime);
        }

        ///<summary>True if a live AcidPool projectile already sits within AcidPoolDedupRadiusTiles of `center` --
        ///the 48-segment body slithers over nearly the same ground, so without this every segment independently
        ///re-dropped an overlapping pool (the reported "ground already has acid but it keeps reapplying" bug,
        ///and a real chunk of the lag: every live pool also spawns its own dust every tick).</summary>
        static bool AcidPoolExistsNear(Vector2 center)
        {
            int poolType = ModContent.ProjectileType<AcidPool>();
            float radius = AcidPoolDedupRadiusTiles * TileSize;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == poolType && Vector2.DistanceSquared(p.Center, center) <= radius * radius)
                {
                    return true;
                }
            }
            return false;
        }

        //-- TailStab: above-ground tail strike using 50% of the body, two modes (OverheadC / LowArch) --
        //The head holds on the ground (HoldGround) while the rear 50% of the body (24 body segments + tail)
        //curves from the grounded anchor to the driven tail tip. Coil -> Aim -> Stab -> (Recover -> Aim)* -> Retract.

        const int TailStabCoilTicks = 120;     //slow rise into one stable arch
        const int TailStabAimTicks = 60;       //hold the threat pose, then lock for the final 24 ticks
        const int TailStabLockTicks = 24;
        const int TailStabStabTicks = 24;      //the fast thrust through the target
        const int TailStabLiveTicks = 18;      //six harmless acceleration ticks, then the blade's actual arrival
        const int TailStabRecoverTicks = 66;   //unhurried return to the same perch between stings
        const int TailStabRetractTicks = 120;  //lower back to the original tail position
        const int TailStabMaxCombo = 3;
        const int TailStabCooldownTicks = 240;
        const int TailStabTriggerRoll = 25;
        const int TailStabDamage = 45;
        const float TailStabReachTiles = 32f; //512px from the anchor, proven with the same 24 body links + tail; 592px cannot reach
        const float TailStabOverheadHeight = 175f; //C perch height above the head/player
        const float TailStabCoilLerp = 0.055f;
        const float TailStabStrikeLerp = 0.42f;    //whip-fast extension during Stabbing so the tip reaches and pierces the target
        const float TailStabSettleSpeed = 6f;     //maximum driven-target movement during raise/recovery/lowering
        const float TailStabStrikeSpeed = 26f;
        const float TailStabLowRise = 72f;     //lower poised tip when the player is alongside the rear half

        static void StartTailStab(NPC npc, GreatSerpentHead data, Player player)
        {
            NPC tail = FindTail(npc);
            NPC anchorSeg = FindTailAnchor(npc);

            //A straddled player may be close to the tail but outside the section's actual reach from
            //its anchor. Reject that start rather than clamping the warning and stinging an empty spot.
            if (tail == null || anchorSeg == null || Vector2.Distance(anchorSeg.Center, player.Center) > TailStabReachTiles * TileSize + 1f)
            {
                return;
            }

            data.TailStabAnchor = anchorSeg.Center;

            //A player alongside the rear section gets a lower perch. Both modes retain the same
            //single arch instead of re-coiling the whole rear body into alternating S lobes.
            bool straddles = (player.Center.X - npc.Center.X) * (player.Center.X - tail.Center.X) < 0f;
            bool cRoom = HasOverheadClearance(npc, (int)(TailStabOverheadHeight / TileSize) + 2);
            if (straddles || !cRoom)
            {
                data.TailStabMode = GreatSerpentHead.TailStabKind.LowArch;
            }
            else
            {
                data.TailStabMode = GreatSerpentHead.TailStabKind.OverheadC;
            }

            data.TailStab = GreatSerpentHead.TailStabState.Coiling;
            data.TailStabTimer = TailStabCoilTicks;
            data.TailStabCombo = 0;
            data.TailStabDamaging = false;
            data.RippleTimer = 0;
            Vector2 tailForward = DirectionFromRotation(tail.rotation);
            data.TailStabTip = tail.Center - tailForward * (tail.height * 0.5f - 2f);
            data.TailStabTarget = player.Center;
            data.TailStabRestOffset = data.TailStabTip - data.TailStabAnchor;
            Vector2 perch = TailStabPerch(npc, data, player);
            data.TailStabPerchOffset = perch - data.TailStabAnchor;
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
        }

        ///<summary>True if there's `tiles` of open (non-solid) space straight above the head -- room for the C.</summary>
        static bool HasOverheadClearance(NPC npc, int tiles)
        {
            int tx = (int)(npc.Center.X / TileSize);
            int topTileY = (int)(npc.position.Y / TileSize);
            for (int i = 1; i <= tiles; i++)
            {
                if (IsSolidTile(tx, topTileY - i))
                {
                    return false;
                }
            }
            return true;
        }

        ///<summary>The "cocked/perched" tip position the tail rises to during Coiling/Aiming/Recover.</summary>
        static Vector2 TailStabPerch(NPC head, GreatSerpentHead data, Player player)
        {
            Vector2 anchor = data.TailStabAnchor == Vector2.Zero ? head.Center : data.TailStabAnchor;
            float dirToPlayer = player.Center.X >= anchor.X ? 1f : -1f;

            if (data.TailStabMode == GreatSerpentHead.TailStabKind.OverheadC)
            {
                //Poise the tail high above the strike zone, curving 50% of the body toward the player and cocked
                //slightly back from the player's X so the downward/forward stab drives straight through them.
                float targetOffsetX = MathHelper.Clamp(player.Center.X - anchor.X - dirToPlayer * 75f, -260f, 260f);
                float highestRefY = Math.Min(head.Center.Y, Math.Min(anchor.Y, player.Center.Y));
                return new Vector2(anchor.X + targetOffsetX, highestRefY - TailStabOverheadHeight);
            }

            //The low-clearance variant uses the same poised arch, closer to the strike lane. Cocking
            //110px away made a distant horizontal sting spend its entire live window just crossing back.
            float horizontalOffset = MathHelper.Clamp(player.Center.X - anchor.X - dirToPlayer * 75f, -260f, 260f);
            return anchor + new Vector2(horizontalOffset, -TailStabLowRise);
        }

        static void UpdateTailStab(NPC npc, GreatSerpentHead data, Player player)
        {
            GreatSerpentHead.TailStabState previousState = data.TailStab;
            bool authoritative = Main.netMode != NetmodeID.MultiplayerClient;
            data.TailStabTimer--;
            data.TailStabDamaging = false;

            NPC anchorSegment = FindTailAnchor(npc);
            if (anchorSegment != null)
            {
                data.TailStabAnchor = anchorSegment.Center;
            }

            Vector2 anchor = data.TailStabAnchor;
            Vector2 perch = anchor + data.TailStabPerchOffset;
            Vector2 tipTarget = perch;
            float tipLerp = TailStabCoilLerp;
            float tipSpeed = TailStabSettleSpeed;

            bool cancellable = data.TailStab == GreatSerpentHead.TailStabState.Coiling
                || data.TailStab == GreatSerpentHead.TailStabState.Aiming
                || data.TailStab == GreatSerpentHead.TailStabState.Recover;
            float distanceToPlayer = Vector2.Distance(anchor, player.Center);
            if (authoritative && cancellable && distanceToPlayer > (TailStabReachTiles + 1f) * TileSize)
            {
                data.TailStab = GreatSerpentHead.TailStabState.Retracting;
                data.TailStabTimer = TailStabRetractTicks;
            }

            switch (data.TailStab)
            {
                case GreatSerpentHead.TailStabState.Coiling:
                {
                    if (!Main.dedServ && data.TailStabTimer % 3 == 0)
                    {
                        int coilDust = Dust.NewDust(data.TailStabTip - new Vector2(8f, 8f), 16, 16, DustID.AncientLight, 0f, -1.5f, 120, Color.Purple, 1.25f);
                        Main.dust[coilDust].noGravity = true;
                    }

                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStab = GreatSerpentHead.TailStabState.Aiming;
                        data.TailStabTimer = TailStabAimTicks;
                        if (authoritative)
                        {
                            data.TailStabTarget = player.Center;
                        }
                    }

                    break;
                }

                case GreatSerpentHead.TailStabState.Aiming:
                {
                    //Only the server tracks the player. The last 24 ticks use one locked target on every
                    //machine; the perched body does not chase that target or rebuild its arch between stings.
                    if (authoritative && data.TailStabTimer >= TailStabLockTicks)
                    {
                        Vector2 desiredTarget = Vector2.Lerp(data.TailStabTarget, player.Center, 0.3f);
                        Vector2 fromAnchor = desiredTarget - anchor;
                        float maxTargetReach = TailStabReachTiles * TileSize;
                        if (fromAnchor.Length() > maxTargetReach)
                        {
                            desiredTarget = anchor + Vector2.Normalize(fromAnchor) * maxTargetReach;
                        }

                        data.TailStabTarget = desiredTarget;
                        if (data.TailStabTimer % 6 == 0)
                        {
                            npc.netUpdate = true;
                        }
                    }

                    if (!Main.dedServ && data.TailStabCombo == 0 && data.TailStabTimer == TailStabLockTicks + 12)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item151 with { Volume = 0.65f, Pitch = 0.3f }, data.TailStabTip);
                    }

                    if (!Main.dedServ && data.TailStabTimer % 3 == 0)
                    {
                        Vector2 dustPosition = data.TailStabTarget + new Vector2(Main.rand.NextFloat(-20f, 20f), -Main.rand.NextFloat(16f, 54f));
                        int targetDust = Dust.NewDust(dustPosition, 6, 6, DustID.AncientLight, 0f, 2f, 120, Color.Purple, 1.2f);
                        Main.dust[targetDust].noGravity = true;

                        int tipDust = Dust.NewDust(data.TailStabTip - new Vector2(6f, 6f), 12, 12, DustID.Venom, 0f, -1f, 110, default, 1.15f);
                        Main.dust[tipDust].noGravity = true;
                    }

                    if (data.TailStabTimer == TailStabLockTicks)
                    {
                        tsorcRevampAIs.SpawnTelegraphFlash(npc, Color.Purple, data.TailStabTip);
                        tsorcRevampAIs.SpawnTelegraphFlash(npc, Color.Purple, data.TailStabTarget);
                        if (authoritative)
                        {
                            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
                        }
                    }

                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStab = GreatSerpentHead.TailStabState.Stabbing;
                        data.TailStabTimer = TailStabStabTicks;
                        if (!Main.dedServ)
                        {
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.85f, Pitch = -0.25f }, data.TailStabTip);
                        }
                    }

                    break;
                }

                case GreatSerpentHead.TailStabState.Stabbing:
                {
                    Vector2 strikeDirection = data.TailStabTarget - perch;
                    if (strikeDirection.LengthSquared() > 1f)
                    {
                        strikeDirection.Normalize();
                    }

                    tipTarget = data.TailStabTarget + strikeDirection * 22f;
                    tipLerp = TailStabStrikeLerp;
                    tipSpeed = TailStabStrikeSpeed;
                    data.TailStabDamaging = data.TailStabTimer > 0 && data.TailStabTimer <= TailStabLiveTicks;

                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStab = GreatSerpentHead.TailStabState.Recover;
                        data.TailStabTimer = TailStabRecoverTicks;
                        data.TailStabDamaging = false;
                    }

                    break;
                }

                case GreatSerpentHead.TailStabState.Recover:
                {
                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStabCombo++;
                        if (data.TailStabCombo < TailStabMaxCombo)
                        {
                            data.TailStab = GreatSerpentHead.TailStabState.Aiming;
                            data.TailStabTimer = TailStabAimTicks;
                            if (authoritative)
                            {
                                data.TailStabTarget = player.Center;
                            }
                        }
                        else
                        {
                            data.TailStab = GreatSerpentHead.TailStabState.Retracting;
                            data.TailStabTimer = TailStabRetractTicks;
                        }
                    }

                    break;
                }

                case GreatSerpentHead.TailStabState.Retracting:
                {
                    tipTarget = anchor + data.TailStabRestOffset;
                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStab = GreatSerpentHead.TailStabState.None;
                        data.TailStabCooldown = TailStabCooldownTicks;
                        if (npc.life <= npc.lifeMax / 2)
                        {
                            data.TailStabCooldown = (int)(TailStabCooldownTicks * 0.65f);
                        }
                    }

                    break;
                }
            }

            Vector2 tipDelta = (tipTarget - data.TailStabTip) * tipLerp;
            float tipDistance = tipDelta.Length();
            if (tipDistance > tipSpeed)
            {
                tipDelta *= tipSpeed / tipDistance;
            }

            data.TailStabTip += tipDelta;
            if (authoritative && data.TailStab != previousState)
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
            }
        }

        //Pose after the anchor NPC updates. The pure solver below uses the sprites' overlapping end
        //sockets, not average center spacing, so the 98px tail stays connected while it changes angle.
        static void PoseTailStabArc(NPC head, GreatSerpentHead data)
        {
            int tailType = ModContent.NPCType<GreatSerpentTail>();
            int attackStart = GreatSerpentHead.BodySegmentCount - GreatSerpentHead.TailAttackSegmentCount;
            NPC anchorSegment = null;
            System.Collections.Generic.List<NPC> rear = new System.Collections.Generic.List<NPC>();

            NPC current = head;
            for (int hops = 0; hops < GreatSerpentHead.TotalSegmentCount + 2; hops++)
            {
                int nextIndex = (int)current.ai[0];
                if (nextIndex <= 0 || nextIndex >= Main.npc.Length)
                {
                    break;
                }

                NPC next = Main.npc[nextIndex];
                if (!next.active)
                {
                    break;
                }

                current = next;
                bool isTail = current.type == tailType;
                int segmentIndex = (int)current.ai[2];
                if (!isTail && segmentIndex == attackStart - 1)
                {
                    anchorSegment = current;
                }

                if (isTail || segmentIndex >= attackStart)
                {
                    rear.Add(current);
                }
            }

            if (anchorSegment == null || rear.Count == 0)
            {
                return;
            }

            data.TailStabAnchor = anchorSegment.Center;
            int count = rear.Count;
            float[] heights = new float[count];
            float[] widths = new float[count];
            Vector2[] centers = new Vector2[count];
            float[] headings = new float[count];
            float chainLength = anchorSegment.height * 0.5f - SegmentOverlap * 0.5f;

            for (int i = 0; i < count; i++)
            {
                heights[i] = rear[i].height;
                widths[i] = rear[i].width;
                centers[i] = rear[i].Center;
                headings[i] = rear[i].rotation - MathHelper.PiOver2;
                float boneLength = heights[i] - SegmentOverlap;
                if (i == count - 1)
                {
                    boneLength = heights[i] - SegmentOverlap * 0.5f - 2f;
                }

                chainLength += Math.Max(2f, boneLength);
            }

            Vector2 tip = data.TailStabTip;
            Vector2 anchorToTip = tip - anchorSegment.Center;
            //Retraction must reach a nearly straight resting chain. The old 94% clamp forced 37px
            //of permanent slack into this 621px section, leaving an unsupported arch above flat ground.
            float maxChord = Math.Max(2f, chainLength - 1f);
            if (anchorToTip.Length() > maxChord)
            {
                tip = anchorSegment.Center + Vector2.Normalize(anchorToTip) * maxChord;
            }

            float archEnvelope = 1f;
            if (data.TailStab == GreatSerpentHead.TailStabState.Coiling)
            {
                float raiseProgress = 1f - data.TailStabTimer / (float)TailStabCoilTicks;
                archEnvelope = MathHelper.SmoothStep(0f, 1f, raiseProgress);
            }
            else if (data.TailStab == GreatSerpentHead.TailStabState.Retracting)
            {
                float remainingRaise = data.TailStabTimer / (float)TailStabRetractTicks;
                archEnvelope = MathHelper.SmoothStep(0f, 1f, remainingRaise);
            }

            Vector2 control1;
            Vector2 control2;
            SerpentChain.BuildTailStabCurve(anchorSegment.Center, tip, chainLength, archEnvelope, anchorSegment.rotation - MathHelper.PiOver2, out control1, out control2);

            float turnLimit = SerpentChain.TailSettleTurnRadians;
            if (data.TailStab == GreatSerpentHead.TailStabState.Stabbing)
            {
                turnLimit = SerpentChain.TailStrikeTurnRadians;
            }

            SerpentChain.PoseAlongCurve(anchorSegment.Center, anchorSegment.height, anchorSegment.width, anchorSegment.rotation - MathHelper.PiOver2,
                heights, widths, centers, headings, 1.55f, control1, control2, tip, BodyTerrainProbe, turnLimit);

            for (int i = 0; i < count; i++)
            {
                NPC segment = rear[i];
                if (segment.ModNPC is GreatSerpentTail tailData)
                {
                    tailData.PreviousStrikeCenter = segment.Center;
                    tailData.PreviousStrikeRotation = segment.rotation;
                    tailData.SweepReady = segment.damage > 0 && data.TailStabDamaging;
                }

                segment.rotation = headings[i] + MathHelper.PiOver2;
                segment.Center = centers[i];
                segment.velocity = Vector2.Zero;
                segment.damage = 0;
                if (segment.type == tailType && data.TailStabDamaging)
                {
                    segment.damage = TailStabDamage;
                }
            }
        }

        ///<summary>Walk the chain to the grounded anchor segment just ahead of the posed 50% tail section.</summary>
        static NPC FindTailAnchor(NPC head)
        {
            int anchorIndex = GreatSerpentHead.BodySegmentCount - GreatSerpentHead.TailAttackSegmentCount - 1;
            int tailType = ModContent.NPCType<GreatSerpentTail>();
            NPC current = head;

            for (int hops = 0; hops < GreatSerpentHead.TotalSegmentCount + 2; hops++)
            {
                if (current.ai[0] <= 0f || (int)current.ai[0] >= Main.npc.Length)
                {
                    break;
                }
                NPC next = Main.npc[(int)current.ai[0]];
                if (!next.active)
                {
                    break;
                }
                current = next;
                if (current.type != tailType && (int)current.ai[2] == anchorIndex)
                {
                    return current;
                }
            }

            return null;
        }

        ///<summary>Walk the chain from the head to its last piece (the tail), or null if the chain is incomplete.</summary>
        static NPC FindTail(NPC head)
        {
            NPC current = head;
            for (int hops = 0; hops < GreatSerpentHead.TotalSegmentCount + 2; hops++)
            {
                if (current.ai[0] <= 0f || (int)current.ai[0] >= Main.npc.Length)
                {
                    break;
                }
                NPC next = Main.npc[(int)current.ai[0]];
                if (!next.active)
                {
                    break;
                }
                current = next;
            }
            return current == head ? null : current;
        }

        //-- Charge --

        static void RunCharge(NPC npc, GreatSerpentHead data, float maxSpeed)
        {
            if (data.ChargeTelegraphTimer > 0)
            {
                data.ChargeTelegraphTimer--;
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, 0f, 0.2f); //brief pull-back so the burst reads as telegraphed
                if (data.ChargeTelegraphTimer == 0)
                {
                    data.ChargeTimer = ChargeDurationTicks;
                }
            }
            else
            {
                npc.velocity.X = data.ChargeDirection.X * maxSpeed * ChargeSpeedMultiplier;
                data.ChargeTimer--;
                npc.damage = ChargeContactDamage; //the charge is a telegraphed ram -> contact hurts during the dash
                SnapHeadToGround(npc, GroundFollowLerp);
            }

            npc.direction = data.ChargeDirection.X >= 0 ? 1 : -1;
            AimHead(npc, data.ChargeDirection.X, npc.velocity.Y);
        }

        //-- Swim --

        static bool IsInWater(NPC npc)
        {
            int tileX = (int)(npc.Center.X / TileSize);
            int tileY = (int)(npc.Center.Y / TileSize);
            if (tileX < 0 || tileY < 0 || tileX >= Main.maxTilesX || tileY >= Main.maxTilesY)
            {
                return false;
            }
            Tile tile = Main.tile[tileX, tileY];
            return tile.LiquidAmount > 64 && tile.LiquidType == LiquidID.Water;
        }

        static void RunSwim(NPC npc, GreatSerpentHead data, Player player, float maxSpeed)
        {
            Vector2 toPlayer = player.Center - npc.Center;
            float dist = toPlayer.Length();
            if (dist > 1f)
            {
                toPlayer /= dist;
            }

            //Water kiting: keep a distance so the head doesn't try to overlap the player; hold once inside it and
            //let the spit/breath attacks do the work. Never lurches to ram.
            Vector2 desired;
            if (dist <= WaterKiteRangeTiles * TileSize)
            {
                desired = Vector2.Zero;
            }
            else
            {
                desired = toPlayer * maxSpeed * SwimSpeedMultiplier;
            }
            npc.velocity = Vector2.Lerp(npc.velocity, desired, 0.05f);

            data.SwimWaveTimer += SwimSineFrequency;
            npc.velocity.Y += (float)Math.Sin(data.SwimWaveTimer) * SwimSineAmplitude * 0.02f;

            if (Math.Abs(npc.velocity.X) > 0.3f)
            {
                data.Facing = npc.velocity.X >= 0f ? 1 : -1;
            }
            npc.direction = data.Facing;
            AimHead(npc, Math.Abs(npc.velocity.X) > 0.3f ? npc.velocity.X : data.Facing, npc.velocity.Y);

            //Still attacks in water -- especially spitting. (RunAttack handles the actual attack; the lunges are
            //auto-slowed while submerged, see the lunge cases.)
            if (data.AttackCooldown <= 0)
            {
                TryStartAttack(npc, data, player, dist);
            }
        }

        //-- Stagger flop --

        static void RunStaggerFlop(NPC npc)
        {
            npc.velocity.X = 0f; //the poise system's ApplyStaggerMovement drives the knockback slide; we just settle Y/rotation
            SnapHeadToGround(npc, GroundFollowLerp * 1.5f); //velocity-based -> settles onto the surface without sinking
            AimHead(npc, -npc.spriteDirection, 0f); //settle level (flopped on the ground)
        }

        ///<summary>Called via IStaggerable so a poise break cancels any in-progress SerpentAI special state
        ///instead of leaving it stuck mid-attack once the flop ends.</summary>
        public static void OnStagger(NPC npc)
        {
            GreatSerpentHead data = npc.ModNPC as GreatSerpentHead;
            if (data == null)
            {
                return;
            }

            data.ChargeTimer = 0;
            data.ChargeTelegraphTimer = 0;
            data.RippleTimer = 0;
            data.CrossingOver = false;
            data.TailFlickWanted = false; //a rest flick still curled up lays back down instead of holding through the flop

            //Cancel an in-progress attack (windup or active) and put it on cooldown so the flop isn't
            //immediately followed by the attack it interrupted.
            data.Attack = GreatSerpentHead.AttackState.None;
            data.AttackTimer = 0;
            data.MouthTransitionTimer = 0;
            data.AttackCooldown = AttackCooldownBaseTicks;
            npc.damage = 0;
            ClearAttackPoise(npc.GetGlobalNPC<tsorcRevampGlobalNPC>());

            //Cancel an in-progress overhead tail stab -- the rear half falls back into the chain on its own once
            //TailStab clears (normal follow/ground-snap resumes). Half cooldown as the stagger tax.
            if (data.TailStab != GreatSerpentHead.TailStabState.None)
            {
                data.TailStab = GreatSerpentHead.TailStabState.None;
                data.TailStabCooldown = TailStabCooldownTicks / 2;
            }
        }

        //-- Attacks --

        //SnakeBite: small neck arch, white eye flash, then a Leonhard-style locked lunge (fairly dodgeable).
        const int BiteTelegraphTicks = 60;
        const int BiteLungeTicks = 22;
        const int BiteRecoverTicks = 42;
        const float BiteArchPixels = 50f;
        const float BiteLungeSpeed = 13f;   //slower strike to match the slower boss (still a quick lunge)
        const int BiteContactDamage = 60;

        //SnakePounce: forebody raises high into a true S, purple mouth dust, then a bigger lunge.
        const int PounceTelegraphTicks = 85;
        const int PounceLungeTicks = 26;
        const int PounceRecoverTicks = 55;
        const float PounceRaisePixels = 160f;
        const float PounceLungeSpeed = 15f;
        const int PounceContactDamage = 50;

        //Sweeping fire breath.
        const int BreathTelegraphTicks = 65;
        const int BreathSweepTicks = 90;
        const int BreathRecoverTicks = 60;
        const float BreathArchPixels = 54f;
        const float BreathSweepHalfAngle = 0.5f; //radians each side of the initial aim
        const int BreathDamage = 45;

        //Venom spit.
        const int SpitTelegraphTicks = 25; //purple flash fires at telegraph start = 25 ticks before the combo
        const int SpitDamage = 35;
        const float SpitStraightSpeed = 12f;
        const float SpitLobSpeed = 12f;
        const float SpitLobGravity = 0.25f;

        //The last TelegraphCommitTicks of a telegraph are hyper-armor (white flash fires here); before that the
        //windup is stagger-cancellable, matching the FighterAI AddAttack telegraph/commit convention.
        const int TelegraphCommitTicks = 25;

        //AcidBody (below 50% HP): body pieces trail acid pools for 10s, then 10s cooldown, cycling.
        const int AcidBodyActiveTicks = 10 * 60;
        const int AcidBodyCooldownTicks = 10 * 60;
        const int AcidBodyPoolLifetime = 6 * 60;
        const int AcidBodyPoolDamage = 10;
        //1-in-N chance per eligible tick for a body segment to spawn its trailing dust -- was unconditional
        //every tick for all 48 body segments (~2880 dust/sec), which is what was "killing the computer".
        const int AcidBodyDustSkipChance = 8;
        //Don't drop a new pool within this many tiles of an existing live one -- the 48-segment body slithers
        //over nearly the same columns, so without this every segment independently re-dropped an overlapping
        //pool on ground that was already covered (both the reported "keeps reapplying" bug and a big chunk of
        //the pool-count-driven lag, since each pool ALSO spawns its own dust every tick in AcidPool.AI()).
        const float AcidPoolDedupRadiusTiles = 2f;
        //A "few pixels lower" per feedback -- the pool previously sat exactly on the surface tile's top edge.
        const float AcidPoolGroundOffset = 6f;


        const int AttackCooldownBaseTicks = 75;
        const int LowHealthAttackCooldownBaseTicks = 45;
        const int AttackTriggerRoll = 10;
        const int LowHealthAttackTriggerRoll = 6;
        const int BiteRangeTiles = 28;
        const int FarAttackRangeTiles = 60;

        //One spit-cadence event: fire `count` shots at combo-tick `tick`, aimed at the player plus `angleDeg`,
        //fanned across `spreadDeg` when count > 1, lobbed (arcing, VenomSpit gravity) when `lob`.
        readonly struct SpitEvent
        {
            public readonly int Tick; public readonly int Count; public readonly float AngleDeg; public readonly float SpreadDeg; public readonly bool Lob;
            public SpitEvent(int tick, int count, float angleDeg, float spreadDeg, bool lob)
            { Tick = tick; Count = count; AngleDeg = angleDeg; SpreadDeg = spreadDeg; Lob = lob; }
        }

        static readonly SpitEvent[][] SpitPatterns = BuildSpitPatterns();
        static readonly int[] SpitPatternRecovery = { 90, 60, 60, 90, 60, 75 };

        static SpitEvent[][] BuildSpitPatterns()
        {
            //V0: S-shaped spray of 12, 5 ticks apart -- aim angle snakes above/below the player line.
            var sSpray = new SpitEvent[12];
            for (int i = 0; i < 12; i++)
            {
                float angle = (float)Math.Sin(i * 0.55f) * 22f;
                sSpray[i] = new SpitEvent(i * 5, 1, angle, 0f, false);
            }

            //V1: three single lobs, 30 ticks apart.
            var tripleLob = new SpitEvent[]
            {
                new SpitEvent(0, 1, 0f, 0f, true),
                new SpitEvent(30, 1, 0f, 0f, true),
                new SpitEvent(60, 1, 0f, 0f, true),
            };

            //V2: two fanned 3-lob bursts, 60 ticks apart.
            var doubleBurst = new SpitEvent[]
            {
                new SpitEvent(0, 3, 0f, 24f, true),
                new SpitEvent(60, 3, 0f, 24f, true),
            };

            //V3: the long combo -- 3 lobs 30 apart, a 60-tick breather, then a fanned burst and 2 more lobs.
            var longCombo = new SpitEvent[]
            {
                new SpitEvent(0, 1, 0f, 0f, true),
                new SpitEvent(30, 1, 0f, 0f, true),
                new SpitEvent(60, 1, 0f, 0f, true),
                new SpitEvent(120, 3, 0f, 24f, true),
                new SpitEvent(150, 1, 0f, 0f, true),
                new SpitEvent(180, 1, 0f, 0f, true),
            };

            //V4: rapid straight 6-shot burst, 8 ticks apart, slight alternating spread.
            var rapidBurst = new SpitEvent[6];
            for (int i = 0; i < 6; i++)
            {
                rapidBurst[i] = new SpitEvent(i * 8, 1, (i % 2 == 0 ? -1 : 1) * 5f, 0f, false);
            }

            //V5: 8 shots alternating lob/straight, 20 ticks apart.
            var alternating = new SpitEvent[8];
            for (int i = 0; i < 8; i++)
            {
                alternating[i] = new SpitEvent(i * 20, 1, 0f, 0f, i % 2 == 0);
            }

            return new SpitEvent[][] { sSpray, tripleLob, doubleBurst, longCombo, rapidBurst, alternating };
        }

        //The head's forward direction (where the mouth points), derived from its rotation so projectiles/dust
        //emit from the actual head tip regardless of how the head is tilted/facing. Convention: rotation R has
        //the sprite's up-local rotated by R, so forward = (sin R, -cos R). (rot 1.57 -> +X, rot 4.71 -> -X.)
        static Vector2 HeadForward(NPC npc) => new Vector2((float)Math.Sin(npc.rotation), -(float)Math.Cos(npc.rotation));
        static Vector2 MouthPosition(NPC npc) => npc.Center + HeadForward(npc) * MouthForwardPixels;
        static Vector2 EyePosition(NPC npc) => npc.Center + HeadForward(npc) * EyeForwardPixels;
        const float MouthForwardPixels = 48f; //rounded socket y=64 to the new slender muzzle at y=16
        const float EyeForwardPixels = 22f;

        static void TryStartAttack(NPC npc, GreatSerpentHead data, Player player, float distanceToPlayer)
        {
            //Server picks the attack. Which one it is decides the telegraph the player reads and, for bite/pounce,
            //where the damaging lunge goes — a client picking its own would show a tell for an attack that never
            //comes. Clients receive Attack/AttackTimer and run the same state machine from there.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool isLowHealth = npc.life < npc.lifeMax / 2;
            int triggerRoll = AttackTriggerRoll;

            if (isLowHealth)
            {
                triggerRoll = LowHealthAttackTriggerRoll;
            }

            if (data.AttackCooldown > 0 || !Main.rand.NextBool(triggerRoll))
            {
                return;
            }

            if (!Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height))
            {
                return;
            }

            float distTiles = distanceToPlayer / TileSize;
            GreatSerpentHead.AttackState chosen;
            int telegraph;

            if (distTiles <= BiteRangeTiles)
            {
                int roll = Main.rand.Next(100);
                if (roll < 50)
                {
                    chosen = GreatSerpentHead.AttackState.BiteTelegraph;
                    telegraph = BiteTelegraphTicks;
                }
                else if (roll < 70)
                {
                    chosen = GreatSerpentHead.AttackState.PounceTelegraph;
                    telegraph = PounceTelegraphTicks;
                }
                else if (roll < 85)
                {
                    chosen = GreatSerpentHead.AttackState.BreathTelegraph;
                    telegraph = BreathTelegraphTicks;
                }
                else
                {
                    chosen = GreatSerpentHead.AttackState.SpitTelegraph;
                    telegraph = SpitTelegraphTicks;
                }
            }
            else if (distTiles <= FarAttackRangeTiles)
            {
                int roll = Main.rand.Next(100);
                if (roll < 30)
                {
                    chosen = GreatSerpentHead.AttackState.BreathTelegraph;
                    telegraph = BreathTelegraphTicks;
                }
                else if (roll < 70)
                {
                    chosen = GreatSerpentHead.AttackState.SpitTelegraph;
                    telegraph = SpitTelegraphTicks;
                }
                else
                {
                    chosen = GreatSerpentHead.AttackState.PounceTelegraph;
                    telegraph = PounceTelegraphTicks;
                }
            }
            else
            {
                return;
            }

            data.Attack = chosen;
            data.AttackTimer = telegraph;
            data.MouthTransitionTimer = GreatSerpentHead.MouthTransitionTicks;
            data.AttackAnchorY = npc.position.Y;

            if (chosen == GreatSerpentHead.AttackState.SpitTelegraph)
            {
                data.SpitVariation = Main.rand.Next(SpitPatterns.Length);
                //Purple flash right away -- the whole 25-tick telegraph is the warning window
                tsorcRevampAIs.SpawnTelegraphFlash(npc, Color.Purple, EyePosition(npc));
            }
            else if (chosen == GreatSerpentHead.AttackState.BreathTelegraph)
            {
                data.BreathBaseAngle = (player.Center - MouthPosition(npc)).ToRotation();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.7f, Pitch = -0.35f }, npc.Center);
            }

            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
        }

        static void RunAttack(NPC npc, GreatSerpentHead data, tsorcRevampGlobalNPC poise)
        {
            Player player = Main.player[npc.target];
            data.AttackTimer--;

            //Cancel a WIND-UP (only the *Telegraph phases) if the player has left range/LOS -- the actual strike
            //(lunge / sweep / spit combo) is committed and always finishes. This is the "cancel attacks when the
            //player goes out of range unless already committed" behaviour.
            bool telegraphing = data.Attack == GreatSerpentHead.AttackState.BiteTelegraph
                || data.Attack == GreatSerpentHead.AttackState.PounceTelegraph
                || data.Attack == GreatSerpentHead.AttackState.BreathTelegraph
                || data.Attack == GreatSerpentHead.AttackState.SpitTelegraph;
            if (telegraphing)
            {
                bool inRange = Vector2.Distance(npc.Center, player.Center) <= (FarAttackRangeTiles + 8) * TileSize
                    && Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height);
                if (!inRange)
                {
                    EndAttack(npc, data, poise);
                    RunRecovery(npc);
                    return;
                }
            }

            switch (data.Attack)
            {
                case GreatSerpentHead.AttackState.BiteTelegraph:
                {
                    SetTelegraphPoise(poise, data.AttackTimer);
                    HoldArch(npc, data, player, BiteArchPixels);
                    if (data.AttackTimer == TelegraphCommitTicks)
                    {
                        tsorcRevampAIs.SpawnTelegraphFlash(npc, Color.White, EyePosition(npc));
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit23, npc.Center);
                    }
                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = GreatSerpentHead.AttackState.BiteLunge;
                        data.AttackTimer = StartLunge(npc, data, player, BiteLungeSpeed, BiteLungeTicks);
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, PitchVariance = 0.2f }, npc.Center);
                    }
                    break;
                }
                case GreatSerpentHead.AttackState.BiteLunge:
                {
                    SetCommittedPoise(poise);
                    npc.velocity = data.LungeVelocity * (IsInWater(npc) ? WaterLungeMultiplier : 1f);
                    npc.damage = BiteContactDamage; //head contact damage is ON only during the lunge
                    AimHead(npc, npc.velocity.X, npc.velocity.Y);
                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = GreatSerpentHead.AttackState.BiteRecover;
                        data.AttackTimer = BiteRecoverTicks;
                    }
                    break;
                }
                case GreatSerpentHead.AttackState.BiteRecover:
                {
                    ClearAttackPoise(poise);
                    RunRecovery(npc);
                    if (data.AttackTimer <= 0)
                    {
                        EndAttack(npc, data, poise);
                    }
                    break;
                }

                case GreatSerpentHead.AttackState.PounceTelegraph:
                {
                    SetTelegraphPoise(poise, data.AttackTimer);
                    HoldArch(npc, data, player, PounceRaisePixels);
                    SpawnMouthVenomDust(npc);
                    if (data.AttackTimer == TelegraphCommitTicks)
                    {
                        tsorcRevampAIs.SpawnTelegraphFlash(npc, Color.White, EyePosition(npc));
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit23, npc.Center);
                    }
                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = GreatSerpentHead.AttackState.PounceLunge;
                        data.AttackTimer = StartLunge(npc, data, player, PounceLungeSpeed, PounceLungeTicks);
                        npc.damage = PounceContactDamage;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.9f, Pitch = -0.2f }, npc.Center);
                    }
                    break;
                }
                case GreatSerpentHead.AttackState.PounceLunge:
                {
                    SetCommittedPoise(poise);
                    npc.velocity = data.LungeVelocity * (IsInWater(npc) ? WaterLungeMultiplier : 1f);
                    npc.damage = PounceContactDamage; //ON every tick of the lunge (baseline is reset to 0 each tick)
                    AimHead(npc, npc.velocity.X, npc.velocity.Y);
                    SpawnMouthVenomDust(npc);
                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = GreatSerpentHead.AttackState.PounceRecover;
                        data.AttackTimer = PounceRecoverTicks;
                    }
                    break;
                }
                case GreatSerpentHead.AttackState.PounceRecover:
                {
                    ClearAttackPoise(poise);
                    RunRecovery(npc);
                    if (data.AttackTimer <= 0)
                    {
                        EndAttack(npc, data, poise);
                    }
                    break;
                }

                case GreatSerpentHead.AttackState.BreathTelegraph:
                {
                    SetTelegraphPoise(poise, data.AttackTimer);
                    HoldArch(npc, data, player, BreathArchPixels);

                    Vector2 mouthPos = MouthPosition(npc);

                    // Track aim until commit so the player can read the locked sweep axis during the final 25 ticks.
                    if (data.AttackTimer > TelegraphCommitTicks)
                    {
                        data.BreathBaseAngle = (player.Center - mouthPos).ToRotation();
                    }

                    bool isSuperHardMode = tsorcRevampWorld.SuperHardMode;

                    if (!Main.dedServ)
                    {
                        float chargeProgress = 1f - data.AttackTimer / (float)BreathTelegraphTicks;
                        float chargeScale = MathHelper.Lerp(1.1f, 2.1f, chargeProgress);
                        Vector2 aimDir = data.BreathBaseAngle.ToRotationVector2();

                        // Converging inhale ring pulling into the jaws so the breath windup is unmistakable.
                        for (int i = 0; i < 2; i++)
                        {
                            float ringAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                            float ringDist = Main.rand.NextFloat(26f, 56f) * (1.15f - 0.55f * chargeProgress);
                            Vector2 spawnPos = mouthPos + ringAngle.ToRotationVector2() * ringDist;
                            Vector2 pullVel = (mouthPos - spawnPos) * 0.14f + npc.velocity;

                            if (isSuperHardMode)
                            {
                                int dustType = Main.rand.NextBool(3) ? DustID.Wraith : DustID.TintableDustLighted;
                                Color dustColor = dustType == DustID.Wraith ? default : new Color(255, 64, 140);
                                Dust inhaleDust = Dust.NewDustPerfect(spawnPos, dustType, pullVel, 90, dustColor, chargeScale * 0.85f);
                                inhaleDust.noGravity = true;
                            }
                            else
                            {
                                int dustType = Main.rand.NextBool(3) ? DustID.InfernoFork : DustID.Torch;
                                Dust inhaleDust = Dust.NewDustPerfect(spawnPos, dustType, pullVel, 80, default, chargeScale);
                                inhaleDust.noGravity = true;
                            }
                        }

                        // Growing flame core at the mouth plus short-range warning sparks along the aim vector.
                        Vector2 sparkVel = aimDir.RotatedByRandom(0.22f) * Main.rand.NextFloat(2.5f, 6.5f);
                        if (isSuperHardMode)
                        {
                            Dust coreDust = Dust.NewDustPerfect(mouthPos, DustID.Blood, sparkVel * 0.5f, 70, default, chargeScale);
                            coreDust.noGravity = true;
                            Lighting.AddLight(mouthPos, 0.85f * chargeProgress, 0.12f * chargeProgress, 0.35f * chargeProgress);
                        }
                        else
                        {
                            Dust coreDust = Dust.NewDustPerfect(mouthPos, DustID.Torch, sparkVel, 70, default, chargeScale * 1.15f);
                            coreDust.noGravity = true;
                            Lighting.AddLight(mouthPos, 0.9f * chargeProgress, 0.45f * chargeProgress, 0.1f * chargeProgress);
                        }
                    }

                    if (data.AttackTimer == TelegraphCommitTicks)
                    {
                        Color flashColor = Color.OrangeRed;
                        if (isSuperHardMode)
                        {
                            flashColor = new Color(232, 30, 86);
                        }

                        tsorcRevampAIs.SpawnTelegraphFlash(npc, flashColor, mouthPos);
                    }

                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = GreatSerpentHead.AttackState.BreathSweep;
                        data.AttackTimer = BreathSweepTicks;
                    }
                    break;
                }
                case GreatSerpentHead.AttackState.BreathSweep:
                {
                    SetCommittedPoise(poise);
                    npc.velocity *= 0.9f; //hold roughly still while sweeping
                    float progress = 1f - data.AttackTimer / (float)BreathSweepTicks;
                    float angle = data.BreathBaseAngle + MathHelper.Lerp(-BreathSweepHalfAngle, BreathSweepHalfAngle, progress);
                    //Head faces the breath horizontally (clamped level); the breath itself sweeps up/down freely.
                    AimHead(npc, (float)Math.Cos(angle), (float)Math.Sin(angle));

                    bool isSuperHardMode = tsorcRevampWorld.SuperHardMode;

                    if (data.AttackTimer % 3 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 breathVelocity = angle.ToRotationVector2() * 10f;
                        if (isSuperHardMode)
                        {
                            // ai[1] = 1f selects breath-stream mode on DestinedDeathBreath (inflicts DestinedDeath debuff).
                            Projectile.NewProjectile(
                                npc.GetSource_FromThis(),
                                MouthPosition(npc),
                                breathVelocity,
                                ModContent.ProjectileType<DestinedDeathBreath>(),
                                BreathDamage,
                                1f,
                                Main.myPlayer,
                                0f,
                                1f);
                        }
                        else
                        {
                            Projectile.NewProjectile(
                                npc.GetSource_FromThis(),
                                MouthPosition(npc),
                                breathVelocity,
                                ModContent.ProjectileType<FireBreath>(),
                                BreathDamage,
                                1f,
                                Main.myPlayer);
                        }
                    }
                    if (data.AttackTimer % 12 == 0)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.3f, Pitch = -0.5f }, npc.Center);
                    }
                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = GreatSerpentHead.AttackState.BreathRecover;
                        data.AttackTimer = BreathRecoverTicks;
                    }
                    break;
                }
                case GreatSerpentHead.AttackState.BreathRecover:
                {
                    ClearAttackPoise(poise);
                    RunRecovery(npc);
                    if (data.AttackTimer <= 0)
                    {
                        EndAttack(npc, data, poise);
                    }
                    break;
                }

                case GreatSerpentHead.AttackState.SpitTelegraph:
                {
                    //The whole 25-tick window is the commit (flash already fired at entry)
                    SetCommittedPoise(poise);
                    HoldArch(npc, data, player, BreathArchPixels);
                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = GreatSerpentHead.AttackState.SpitCombo;
                        data.SpitTick = 0;
                        SpitEvent[] pattern = SpitPatterns[data.SpitVariation];
                        data.AttackTimer = pattern[pattern.Length - 1].Tick + 1;
                    }
                    break;
                }
                case GreatSerpentHead.AttackState.SpitCombo:
                {
                    SetCommittedPoise(poise);
                    npc.velocity *= 0.9f;
                    int dirToPlayer = player.Center.X >= npc.Center.X ? 1 : -1;
                    npc.direction = dirToPlayer;
                    AimHead(npc, player.Center.X - npc.Center.X, player.Center.Y - npc.Center.Y);

                    SpitEvent[] pattern = SpitPatterns[data.SpitVariation];
                    foreach (SpitEvent evt in pattern)
                    {
                        if (evt.Tick == data.SpitTick)
                        {
                            FireSpitEvent(npc, player, evt);
                        }
                    }
                    data.SpitTick++;
                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = GreatSerpentHead.AttackState.SpitRecover;
                        data.AttackTimer = SpitPatternRecovery[data.SpitVariation];
                    }
                    break;
                }
                case GreatSerpentHead.AttackState.SpitRecover:
                {
                    ClearAttackPoise(poise);
                    RunRecovery(npc);
                    if (data.AttackTimer <= 0)
                    {
                        EndAttack(npc, data, poise);
                    }
                    break;
                }
            }
        }

        ///<summary>Telegraph windup: cancellable by a poise break until the last TelegraphCommitTicks, then hyper-armor.
        ///Mirrors SimpleProjectile's telegraph/commit split so the serpent plays by the same rules as FighterAI enemies.</summary>
        static void SetTelegraphPoise(tsorcRevampGlobalNPC poise, int ticksRemaining)
        {
            poise.AttackTelegraphing = ticksRemaining > TelegraphCommitTicks;
            poise.AttackCommitted = ticksRemaining <= TelegraphCommitTicks;
        }

        static void SetCommittedPoise(tsorcRevampGlobalNPC poise)
        {
            poise.AttackTelegraphing = false;
            poise.AttackCommitted = true;
        }

        static void ClearAttackPoise(tsorcRevampGlobalNPC poise)
        {
            poise.AttackTelegraphing = false;
            poise.AttackCommitted = false;
        }

        ///<summary>Neck arch: brake horizontally and ease the head up toward the anchor height minus archPixels.
        ///The front-free chain segments trace the raised curve on their own -- the taller the raise, the more S it reads.</summary>
        static void HoldArch(NPC npc, GreatSerpentHead data, Player player, float archPixels)
        {
            npc.velocity.X *= 0.9f;
            npc.velocity.Y = 0f;
            npc.position.Y = MathHelper.Lerp(npc.position.Y, data.AttackAnchorY - archPixels, 0.08f);
            npc.direction = player.Center.X >= npc.Center.X ? 1 : -1;
            AimHead(npc, player.Center.X - npc.Center.X, player.Center.Y - npc.Center.Y);
        }

        //A lunge that started far away must still travel far enough to actually reach the player -- otherwise
        //it visibly stops short and never connects (the reported "stops right before touching me at distance").
        //Overshoot a little past the exact required distance (so the player isn't saved by rounding) and cap
        //the extension so a lunge from very far away doesn't turn into an absurdly long committed dash.
        const float LungeOvershoot = 1.15f;
        const int MaxLungeExtraTicks = 60;

        ///<summary>Locks a straight-line lunge at the player's CURRENT position (no homing -- dodge by moving off
        ///the line), scaling how long the lunge runs so it actually covers the distance at launch. Returns the
        ///duration (in ticks) the caller should hold the Lunge state for -- normally `baseDuration`, longer only
        ///if the base speed/duration wouldn't otherwise reach.</summary>
        static int StartLunge(NPC npc, GreatSerpentHead data, Player player, float speed, int baseDuration)
        {
            Vector2 toPlayer = player.Center - npc.Center;
            float dist = toPlayer.Length();
            if (dist < 1f)
            {
                toPlayer = new Vector2(npc.direction, 0f);
                dist = 0f;
            }
            else
            {
                toPlayer /= dist;
            }
            data.LungeVelocity = toPlayer * speed; //locked at launch, no homing -- dodge by moving off the line

            float reach = speed * baseDuration;
            if (dist <= reach)
            {
                return baseDuration;
            }
            int extended = (int)Math.Ceiling(dist / speed * LungeOvershoot);
            return Math.Min(extended, baseDuration + MaxLungeExtraTicks);
        }

        static void RunRecovery(NPC npc)
        {
            npc.velocity.X *= 0.9f;
            SnapHeadToGround(npc, GroundFollowLerp); //drives velocity.Y -- keeps the head on the surface, no sink
            AimHead(npc, -npc.spriteDirection, 0f); //settle level, keep current facing
        }

        static void EndAttack(NPC npc, GreatSerpentHead data, tsorcRevampGlobalNPC poise)
        {
            data.Attack = GreatSerpentHead.AttackState.None;
            data.MouthTransitionTimer = 0;
            //Clients run this transition too (it is timer-driven), but only the server rolls the gap to the next
            //attack — that roll decides when the next telegraph starts, and it arrives with the snapshot.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                bool isLowHealth = npc.life < npc.lifeMax / 2;
                if (isLowHealth)
                {
                    data.AttackCooldown = LowHealthAttackCooldownBaseTicks + Main.rand.Next(30);
                }
                else
                {
                    data.AttackCooldown = AttackCooldownBaseTicks + Main.rand.Next(45);
                }
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestNetworkSnapshot();
            }
            ClearAttackPoise(poise);
        }

        static void SpawnMouthVenomDust(NPC npc)
        {
            Vector2 mouth = MouthPosition(npc);
            int dust = Dust.NewDust(mouth - new Vector2(6f, 6f), 12, 12, DustID.AncientLight, 0f, -0.5f, 150, Color.Purple, 1.1f);
            Main.dust[dust].noGravity = true;
            if (Main.rand.NextBool(2))
            {
                int bubble = Dust.NewDust(mouth - new Vector2(6f, 6f), 12, 12, DustID.Venom, 0f, -0.8f, 120, default, 1.0f);
                Main.dust[bubble].noGravity = true;
            }
        }

        static void FireSpitEvent(NPC npc, Player player, SpitEvent evt)
        {
            SpawnMouthVenomDust(npc);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.5f, PitchVariance = 0.2f }, npc.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 mouth = MouthPosition(npc);
            for (int i = 0; i < evt.Count; i++)
            {
                //Fan multi-shot events evenly across SpreadDeg; vary lob targets so arcs don't stack
                float fan = evt.Count > 1 ? MathHelper.Lerp(-evt.SpreadDeg, evt.SpreadDeg, i / (float)(evt.Count - 1)) : 0f;
                Vector2 velocity;
                float gravity;
                if (evt.Lob)
                {
                    Vector2 target = player.Center + new Vector2(fan * 8f, 0f); //spread lobs land a few tiles apart
                    velocity = UsefulFunctions.BallisticTrajectory(mouth, target, SpitLobSpeed, SpitLobGravity, false, true);
                    gravity = SpitLobGravity;
                }
                else
                {
                    float angle = (player.Center - mouth).ToRotation() + MathHelper.ToRadians(evt.AngleDeg + fan);
                    velocity = angle.ToRotationVector2() * SpitStraightSpeed;
                    gravity = 0f;
                }
                Projectile.NewProjectile(npc.GetSource_FromThis(), mouth, velocity, ModContent.ProjectileType<VenomSpit>(), SpitDamage, 1f, Main.myPlayer, gravity);
            }
        }

        //-- Diagnostics --

        ///<summary>Flip to false to silence the log once the boss is dialled in.</summary>
        public static bool DebugLogging = true;
        const int LogIntervalTicks = 5; //finer than before -- diagnosing a FAST rear-body rotation oscillation needs more resolution than a 12-tick (0.2s) sample can show
        static int _lastLogTick;

        ///<summary>
        ///Writes head state to Logs/tsorcRevamp-serpent.log (same convention as the SF4 nav log).
        ///Key tells: `act` = what the head decided this tick; `gY` = the ground row it sensed (-1 = none found,
        ///i.e. it thinks it's over a void); `obs` = obstacle height ahead; `room` = headroom above (false while
        ///it's under a ceiling); `climb` = remaining climb budget; `stuck`/`unstick` = the failsafe.
        ///</summary>
        ///<summary>
        ///Debug-only: samples one body piece deep in the "last third" of the chain (index BodySegmentCount-6,
        ///well inside the reported oscillation zone) so a fresh log can show its rotation frame-to-frame instead
        ///of only ever seeing the head's (sane-looking) numbers. Walks the chain by hop count each call --
        ///O(segments), fine at the log's throttled interval, don't call this every tick.
        ///</summary>
        static string RearSegmentSample(NPC head)
        {
            int targetIdx = Math.Max(0, GreatSerpentHead.BodySegmentCount - 6);
            NPC current = head;
            for (int hops = 0; hops <= targetIdx; hops++)
            {
                if (current.ai[0] <= 0f || (int)current.ai[0] >= Main.npc.Length)
                {
                    return " rear=n/a";
                }
                NPC next = Main.npc[(int)current.ai[0]];
                if (!next.active)
                {
                    return " rear=n/a";
                }
                current = next;
            }
            bool isTailAttackSeg = (int)current.ai[2] >= GreatSerpentHead.BodySegmentCount - GreatSerpentHead.TailAttackSegmentCount;
            GreatSerpentHead headData = head.ModNPC as GreatSerpentHead;
            bool posed = headData != null && headData.TailStab != GreatSerpentHead.TailStabState.None && isTailAttackSeg;
            return $" rear[{targetIdx}]=pos({current.Center.X / TileSize:F1},{current.Center.Y / TileSize:F1})rot={current.rotation:F2}eligible={isTailAttackSeg}posed={posed}";
        }

        ///<summary>Debug-only: the actual TAIL piece's position/rotation (not the rear[N] body sample above) --
        ///added to directly verify the tail-stab tangent fix (the tail's rotation used to freeze/go stale for
        ///the whole stab because its own segDir sample degenerated to zero; see PoseTailStabArc).</summary>
        static string TailSample(NPC head)
        {
            NPC tail = FindTail(head);
            if (tail == null)
            {
                return " tail=n/a";
            }
            return $" tail=pos({tail.Center.X / TileSize:F1},{tail.Center.Y / TileSize:F1})rot={tail.rotation:F2}dmg={tail.damage}";
        }

        static void SerpentLog(NPC npc, GreatSerpentHead data, Player player)
        {
            if (!DebugLogging || Main.dedServ)
            {
                return;
            }
            int now = (int)Main.GameUpdateCount;
            if (now - _lastLogTick < LogIntervalTicks)
            {
                return;
            }
            _lastLogTick = now;

            try
            {
                int centerTileX = (int)(npc.Center.X / TileSize);
                int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
                int headTileY = (int)(npc.position.Y / TileSize);
                int groundY = FindGroundSurfaceTileYSmoothed(centerTileX, feetTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 2);
                int obs = GetObstacleHeightAhead(centerTileX + data.Facing * SmallStepTiles, feetTileY, MaxClimbHeightTiles + 2);
                bool room = !IsSolidTile(centerTileX, headTileY - 1) && !IsSolidTile(centerTileX, headTileY - 2);
                bool los = Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height);
                float distTiles = Vector2.Distance(npc.Center, player.Center) / TileSize;
                //Remaining lunge travel (speed * ticks left) -- only meaningful while act=attack-BiteLunge/
                //PounceLunge; lets a log confirm a lunge's total reach actually covers distTiles at launch
                //instead of stopping short (the reported "doesn't connect at range" bug).
                float lungeReachTiles = data.LungeVelocity.Length() * data.AttackTimer / TileSize;

                string sep = System.IO.Path.DirectorySeparatorChar.ToString();
                string dir = Main.SavePath + sep + "Logs";
                System.IO.Directory.CreateDirectory(dir);
                string path = dir + sep + "tsorcRevamp-serpent.log";

                string line = $"[{DateTime.Now:HH:mm:ss}] head#{npc.whoAmI} tick={Main.GameUpdateCount}"
                    + $" pos=({npc.Center.X / TileSize:F1},{npc.Center.Y / TileSize:F1})"
                    + $" player=({player.Center.X / TileSize:F1},{player.Center.Y / TileSize:F1}) dist={distTiles:F1}"
                    + $" vel=({npc.velocity.X:F2},{npc.velocity.Y:F2})"
                    + $" face={data.Facing} rot={npc.rotation:F2} los={los}"
                    + $" act={data.LastAction}"
                    + $" gY={groundY} feetY={feetTileY} obs={obs} room={room} climb={data.ClimbBudget}"
                    + $" stuck={data.StuckTimer} stuckWander={data.StuckWanderTimer} cloak={data.CloakAlphaTarget}/{data.CloakCooldown}"
                    + $" plan={SmartSerpent4AI.DescribePlan(data)} planRes=\"{data.LastPlanResult}\""
                    + $" cross={data.CrossingOver}/{data.CrossOverCooldown}"
                    + $" atk={data.Attack}/{data.AttackTimer} atkCD={data.AttackCooldown} lungeReach={lungeReachTiles:F1}"
                    + $" stab={data.TailStab}/{data.TailStabTimer} stabCD={data.TailStabCooldown} combo={data.TailStabCombo}"
                    + $" acid={data.AcidBodyTimer}"
                    + $" bodyMaxStep={data.DebugBodyMaxStep:F3}px@{data.DebugBodyMaxStepIndex}/t{data.DebugBodyMaxStepTick}"
                    + $" bodyMaxTurn={MathHelper.ToDegrees(data.DebugBodyMaxTurn):F2}deg"
                    + RearSegmentSample(npc)
                    + TailSample(npc);
                System.IO.File.AppendAllText(path, line + Environment.NewLine);
                data.DebugBodyMaxStep = 0f;
                data.DebugBodyMaxTurn = 0f;
            }
            catch
            {
                //Diagnostics must not interrupt the fight if the log file is temporarily unavailable.
            }
        }

        //-- Tile sensing --

        ///<summary>
        ///What the serpent treats as terrain: REAL solid blocks only. Platforms (and other jump-through /
        ///solid-top tiles) are deliberately excluded.
        ///<para/>
        ///This is the fix for the boss snagging on platforms and its body zig-zagging: Main.tileSolid is true for
        ///platforms, so every segment was independently snapping to whatever platform layer happened to be nearest
        ///it, and the head read platform columns as climbable walls. A 22-segment serpent that already phases
        ///through tiles (noTileCollide + behindTiles) should just ignore them and ride the real floor.
        ///</summary>
        internal static bool IsSolidTile(int tileX, int tileY)
        {
            if (tileX < 0 || tileY < 0 || tileX >= Main.maxTilesX || tileY >= Main.maxTilesY)
            {
                return true; //treat out-of-world as blocking rather than as an open gap
            }
            Tile tile = Main.tile[tileX, tileY];
            if (!tile.HasTile || tile.IsActuated || !Main.tileSolid[tile.TileType])
            {
                return false;
            }
            if (TileID.Sets.Platforms[tile.TileType] || Main.tileSolidTop[tile.TileType])
            {
                return false; //platforms are not terrain to this boss
            }
            return true;
        }

        ///<summary>Height (in tiles) of the contiguous solid column at tileX, scanning up from feetTileY.</summary>
        internal static int GetObstacleHeightAhead(int tileX, int feetTileY, int maxUpTiles)
        {
            if (!IsSolidTile(tileX, feetTileY))
            {
                return 0;
            }
            int height = 0;
            for (int h = 0; h < maxUpTiles; h++)
            {
                if (IsSolidTile(tileX, feetTileY - h))
                {
                    height = h + 1;
                }
                else
                {
                    break;
                }
            }
            return height;
        }

        ///<summary>First solid tile row found scanning down from startTileY, or -1 if none within maxDownTiles.</summary>
        internal static int FindGroundSurfaceTileY(int tileX, int startTileY, int maxDownTiles)
        {
            for (int d = 0; d <= maxDownTiles; d++)
            {
                int y = startTileY + d;
                if (IsSolidTile(tileX, y))
                {
                    return y;
                }
            }
            return -1;
        }

        ///<summary>
        ///Samples centerTileX-1/0/+1 and returns a smoothed surface row that ignores 1-tile notches without ever
        ///reporting a floor deeper than the solid column directly under centerTileX (which would pull segments
        ///down into the solid tile they sit on).
        ///</summary>
        static int FindGroundSurfaceTileYSmoothed(int centerTileX, int startTileY, int maxDownTiles)
        {
            int leftY = FindGroundSurfaceTileY(centerTileX - 1, startTileY, maxDownTiles);
            int centerY = FindGroundSurfaceTileY(centerTileX, startTileY, maxDownTiles);
            int rightY = FindGroundSurfaceTileY(centerTileX + 1, startTileY, maxDownTiles);

            int validCount = 0;
            int firstY = -1;
            int secondY = -1;
            int thirdY = -1;

            if (leftY >= 0)
            {
                firstY = leftY;
                validCount++;
            }

            if (centerY >= 0)
            {
                if (validCount == 0)
                {
                    firstY = centerY;
                }
                else
                {
                    secondY = centerY;
                }
                validCount++;
            }

            if (rightY >= 0)
            {
                if (validCount == 0)
                {
                    firstY = rightY;
                }
                else if (validCount == 1)
                {
                    secondY = rightY;
                }
                else
                {
                    thirdY = rightY;
                }
                validCount++;
            }

            if (validCount == 0)
            {
                return -1;
            }

            if (validCount == 1)
            {
                return firstY;
            }

            if (validCount == 2)
            {
                int shallower = Math.Min(firstY, secondY);
                return shallower;
            }

            // Median of three valid columns, clamped so we never sink below the center column's actual surface.
            int minVal = Math.Min(firstY, Math.Min(secondY, thirdY));
            int maxVal = Math.Max(firstY, Math.Max(secondY, thirdY));
            int medianY = firstY + secondY + thirdY - minVal - maxVal;

            if (centerY >= 0 && medianY > centerY)
            {
                return centerY;
            }

            return medianY;
        }

        ///<summary>
        ///When the obstacle ahead is too tall to climb, look both directions (preferring the player's side)
        ///within NavSearchRadius for a column whose obstacle height IS climbable. Returns 0 if nothing found.
        ///</summary>
        static int FindShorterPathDirection(int centerTileX, int feetTileY, int preferredDir)
        {
            int firstDir = preferredDir >= 0 ? 1 : -1;
            int secondDir = -firstDir;
            for (int d = 1; d <= NavSearchRadius; d++)
            {
                int xFirst = centerTileX + firstDir * d;
                if (GetObstacleHeightAhead(xFirst, feetTileY, MaxClimbHeightTiles + 2) <= MaxClimbHeightTiles)
                {
                    return firstDir;
                }
                int xSecond = centerTileX + secondDir * d;
                if (GetObstacleHeightAhead(xSecond, feetTileY, MaxClimbHeightTiles + 2) <= MaxClimbHeightTiles)
                {
                    return secondDir;
                }
            }
            return 0;
        }
    }

    ///<summary>
    ///The serpent's hose-chain joint maths with no Terraria state in it, so the game (SerpentAI.RunBodyFollow and
    ///PoseTailStabArc) and the offline rig (.agents/tools/SerpentRig) run literally the same code and cannot drift.
    ///<para/>
    ///Heading convention: a piece's heading is the direction from it to the piece AHEAD of it (toward the head).
    ///Its sprite rotation is heading + 90 degrees.
    ///<para/>
    ///Bend rule (how stiff the body is) is a guide that adapts, not a fixed angle: each joint has a COMFORT bend worked out
    ///from the sprites (a piece may curve no tighter than MinBendRadiusInWidths x its own thickness, so the thin tapering
    ///rear is more flexible than the fat front), scaled by a flexibility the head passes in (loose when relaxed, stiff in a
    ///lunge). Past the comfort bend the joint resists smoothly (SoftLimit) up to HardBendFactor x comfort, so curves never
    ///sit at one identical clamped angle (no faceted polygon look) and nothing kinks.
    ///</summary>
    public static class SerpentChain
    {
        ///<summary>How tight a piece may curve in comfort, as a multiple of its own thickness: minimum bend radius = this x
        ///width. Per-joint comfort angle = linkLength / that radius (~15-17 deg for the fat front pieces, ~28 deg for Body2,
        ///more for the thin tail end). Static fields so the rig can sweep them.</summary>
        public static float MinBendRadiusInWidths = 2.5f;

        ///<summary>The most a joint can ever bend, as a multiple of its comfort bend. Between 1x and this it resists smoothly.</summary>
        public static float HardBendFactor = 1.6f;

        ///<summary>Floor on the comfort bend for the first few neck pieces. Their links are very short (heavy head-glue overlap),
        ///so the width rule alone would make the neck stiffer than the body.</summary>
        public const float NeckComfortBendRadians = 0.35f;

        ///<summary>Comfort bend for the first body piece, which hangs off the head. The head sprite's heading is clamped
        ///near-horizontal (so it never renders upside-down) even while the neck climbs a wall, so it can't be tied to it tightly.</summary>
        public const float HeadJointComfortBendRadians = 1.0f;

        public const float ComfortBendFloorRadians = 0.12f;
        public const float ComfortBendCeilingRadians = 0.6f; //~34 deg comfort, so ~55 deg at the hard limit: the thin tail end flexes but never hooks

        ///<summary>Per-tick cap on how fast a heading chases its pull. Only a guard against one-tick teleports; the bend
        ///limit is what actually shapes the body.</summary>
        public const float MaxTurnPerTickRadians = 0.2f;
        public const float MaxBodyStepPixels = 8f;

        //A wide head remains supported by a ledge until its whole footprint clears it. Ignore solid
        //columns whose top is above the search window; a tall wall is not a floor to climb forever.
        public static int FindHeadSupportSurface(int leftColumn, int rightColumn, int startRow, int maxDownRows, Func<int, int, bool> isSolid)
        {
            int highestSurface = -1;
            for (int x = leftColumn; x <= rightColumn; x++)
            {
                for (int y = startRow; y <= startRow + maxDownRows; y++)
                {
                    if (!isSolid(x, y))
                    {
                        continue;
                    }

                    bool exposedTop = !isSolid(x, y - 1);
                    if (exposedTop && (highestSurface < 0 || y < highestSurface))
                    {
                        highestSurface = y;
                    }

                    break;
                }
            }

            return highestSurface;
        }

        ///<summary>Fraction of the gap between a joint's heading and its pull closed each tick. Below 1 it low-passes head
        ///jitter (ground-snap noise) so it doesn't whip down the whole body as a wiggle.</summary>
        public const float HeadingFollow = 0.5f;

        ///<summary>Extra bend a joint may use, only when the alternative is putting the piece inside terrain (25 degrees).</summary>
        public const float TerrainEscapeExtraBendRadians = 0.436f;

        const float TerrainSearchStepRadians = 0.05f; //candidate headings tried each side, nearest first
        const int TerrainSearchMaxSteps = 24;         //up to +-1.2 rad from the unblocked heading
        const float HeadingDeadzoneRadians = 0.008f;  //suppresses sub-degree micro-hunting when resting on slopes

        //Faster turn rate used when actively posing the tail along the tail-stab Bezier arch and thrust line.
        public const float TailSettleTurnRadians = 0.045f;
        public const float TailStrikeTurnRadians = 0.2f;

        //Follower spacing = average neighbour sprite-length minus a fixed pixel overlap, so the tight-trimmed sprites sit
        //SegmentOverlap px on top of each other for a seamless tube. The first HeadGlueSegments pieces overlap much more
        //so the head stays welded to the body on turns.
        public const float SegmentOverlap = 9f;
        public const int HeadGlueSegments = 3;
        public const float HeadGlueOverlap = 18f;

        //How strongly a ground-hugging joint's pull leans toward the sensed floor each tick. Soft on purpose: the lean
        //only tilts the joint's heading (inside the bend limit), so a gentle settle looks like a body easing onto the ground.
        public const float GroundHugLerp = 0.1f;

        ///<summary>
        ///The comfort bend (radians) of a joint between a piece and the one ahead of it: link length over the minimum bend
        ///radius the thinner of the two allows, floored for the neck, times the head's current flexibility, kept in sane bounds.
        ///</summary>
        public static float ComfortBend(float linkLength, float selfWidth, float aheadWidth, int bodyIndex, float flexibility)
        {
            if (bodyIndex == 0)
            {
                return HeadJointComfortBendRadians;
            }

            float thickness = Math.Min(selfWidth, aheadWidth);
            float comfort = linkLength / (MinBendRadiusInWidths * Math.Max(thickness, 1f));
            if (bodyIndex < HeadGlueSegments)
            {
                comfort = Math.Max(comfort, NeckComfortBendRadians);
            }

            comfort *= flexibility;

            return MathHelper.Clamp(comfort, ComfortBendFloorRadians, ComfortBendCeilingRadians);
        }

        ///<summary>
        ///Resistance curve: identity up to the comfort bend (free), then a smooth tanh roll-off that approaches
        ///comfort x HardBendFactor and never exceeds it. Continuous in value and slope at the comfort bend.
        ///</summary>
        public static float SoftLimit(float offset, float comfort)
        {
            float magnitude = Math.Abs(offset);
            if (magnitude <= comfort)
            {
                return offset;
            }

            float reserve = comfort * (HardBendFactor - 1f);
            float limited = comfort + reserve * (float)Math.Tanh((magnitude - comfort) / reserve);

            return Math.Sign(offset) * limited;
        }

        ///<summary>
        ///One ordinary body joint: the piece at bodyIndex (0 = first piece behind the head), its pull toward the piece ahead,
        ///then SolveLink. groundCenterY (NaN = none) leans the pull toward the floor; curling instead holds a fixed bend
        ///relative to the piece ahead (the passive tail flick). Heights are the sprites' along-chain lengths, widths their
        ///thickness, in pixels. flexibility scales the comfort bend (1 = normal). Returns the new heading; newCenter is where
        ///the piece goes.
        ///</summary>
        public static float SolveBodyJoint(Vector2 aheadCenter, float aheadHeight, float aheadWidth, float aheadHeading,
            Vector2 selfCenter, float selfHeight, float selfWidth, float selfHeading, int bodyIndex, float flexibility,
            bool curling, float curlJointRadians, float groundCenterY, Func<Vector2, bool> isBlocked, out Vector2 newCenter,
            float maxMovement = MaxBodyStepPixels)
        {
            float overlap = SegmentOverlap;
            if (bodyIndex < HeadGlueSegments)
            {
                overlap = HeadGlueOverlap;
            }

            float linkLength = (selfHeight + aheadHeight) * 0.5f - overlap;
            if (linkLength < 2f)
            {
                linkLength = 2f;
            }

            // Where the pull comes from: this piece's own centre, leaned toward the floor when hugging ground.
            Vector2 pullFrom = selfCenter;
            if (!float.IsNaN(groundCenterY))
            {
                pullFrom.Y = MathHelper.Lerp(pullFrom.Y, groundCenterY, GroundHugLerp);
            }

            Vector2 toAhead = aheadCenter - pullFrom;
            float desiredHeading = selfHeading;
            if (toAhead.LengthSquared() > 0.01f)
            {
                desiredHeading = (float)Math.Atan2(toAhead.Y, toAhead.X);
            }
            if (curling)
            {
                desiredHeading = aheadHeading + curlJointRadians;
            }

            float comfort = ComfortBend(linkLength, selfWidth, aheadWidth, bodyIndex, flexibility);

            return SolveLink(aheadCenter, aheadHeading, comfort, selfHeading, desiredHeading, linkLength, isBlocked, out newCenter,
                selfCenter, groundCenterY, maxMovement);
        }

        //One upward arch replaces the alternating S lobes. Arc-length matching gives the existing
        //chain enough room without changing its curvature plane whenever the target moves.
        public static void BuildTailStabCurve(Vector2 anchor, Vector2 tip, float chainLength, float envelope, float anchorHeading,
            out Vector2 control1, out Vector2 control2)
        {
            float lowBow = 0f;
            float highBow = chainLength;
            control1 = anchor;
            control2 = tip;
            float rootDirection = 1f;
            if (Math.Cos(anchorHeading) < 0f)
            {
                rootDirection = -1f;
            }

            for (int i = 0; i < 12; i++)
            {
                float bow = (lowBow + highBow) * 0.5f * envelope;
                float peakY = Math.Min(anchor.Y, tip.Y) - bow;
                control1 = new Vector2(anchor.X - rootDirection * bow * 1.1f, MathHelper.Lerp(anchor.Y, peakY, 0.65f));
                control2 = new Vector2(tip.X - rootDirection * bow * 0.1f, peakY);

                float arcLength = 0f;
                Vector2 previous = anchor;
                for (int j = 1; j <= 48; j++)
                {
                    Vector2 sample = CubicBezier(anchor, control1, control2, tip, j / 48f);
                    arcLength += Vector2.Distance(previous, sample);
                    previous = sample;
                }

                if (arcLength < chainLength)
                {
                    lowBow = (lowBow + highBow) * 0.5f;
                }
                else
                {
                    highBow = (lowBow + highBow) * 0.5f;
                }
            }
        }

        //Build a curve-shaped joint skeleton, solve its endpoint, then approach those angles once per
        //tick. Unlike a local pull toward each sample, this cannot chase its own error into small loops.
        public static void PoseAlongCurve(Vector2 anchorCenter, float anchorHeight, float anchorWidth, float anchorHeading,
            float[] heights, float[] widths, Vector2[] centers, float[] headings, float flexibility,
            Vector2 control1, Vector2 control2, Vector2 tip, Func<Vector2, bool> isBlocked,
            float turnLimit = TailSettleTurnRadians)
        {
            const int SampleCount = 96;
            int count = heights.Length;
            Vector2[] curve = new Vector2[SampleCount + 1];
            float[] arcLengths = new float[SampleCount + 1];
            float[] boneLengths = new float[count];
            Vector2[] joints = new Vector2[count + 1];
            float anchorSocketLength = Math.Max(1f, anchorHeight * 0.5f - SegmentOverlap * 0.5f);
            Vector2 root = anchorCenter - HeadingToDirection(anchorHeading) * anchorSocketLength;
            curve[0] = root;
            float totalLength = 0f;

            for (int i = 0; i < count; i++)
            {
                boneLengths[i] = Math.Max(2f, heights[i] - SegmentOverlap);
                if (i == count - 1)
                {
                    //The endpoint is the visible needle point, two pixels inside the PNG's last row.
                    boneLengths[i] = heights[i] - SegmentOverlap * 0.5f - 2f;
                }

                totalLength += boneLengths[i];
            }

            for (int i = 1; i <= SampleCount; i++)
            {
                curve[i] = CubicBezier(root, control1, control2, tip, i / (float)SampleCount);
                arcLengths[i] = arcLengths[i - 1] + Vector2.Distance(curve[i - 1], curve[i]);
            }

            joints[0] = root;
            float coveredLength = 0f;
            int sampleIndex = 1;
            for (int i = 0; i < count; i++)
            {
                coveredLength += boneLengths[i];
                float sampleDistance = arcLengths[SampleCount] * coveredLength / totalLength;
                while (sampleIndex < SampleCount && arcLengths[sampleIndex] < sampleDistance)
                {
                    sampleIndex++;
                }

                float sampleSpan = arcLengths[sampleIndex] - arcLengths[sampleIndex - 1];
                float sampleFraction = 0f;
                if (sampleSpan > 0.001f)
                {
                    sampleFraction = (sampleDistance - arcLengths[sampleIndex - 1]) / sampleSpan;
                }

                joints[i + 1] = Vector2.Lerp(curve[sampleIndex - 1], curve[sampleIndex], sampleFraction);
            }

            //Forward/backward length projection keeps the raised arch and places its terminal joint on
            //the committed tip target. Actual sprites still obey the per-tick turn budget below.
            for (int iteration = 0; iteration < 16; iteration++)
            {
                joints[count] = tip;
                for (int i = count - 1; i >= 0; i--)
                {
                    Vector2 towardRoot = joints[i] - joints[i + 1];
                    if (towardRoot.LengthSquared() < 0.001f)
                    {
                        towardRoot = HeadingToDirection(headings[i]);
                    }

                    towardRoot.Normalize();
                    joints[i] = joints[i + 1] + towardRoot * boneLengths[i];
                }

                joints[0] = root;
                for (int i = 0; i < count; i++)
                {
                    Vector2 towardTip = joints[i + 1] - joints[i];
                    if (towardTip.LengthSquared() < 0.001f)
                    {
                        towardTip = -HeadingToDirection(headings[i]);
                    }

                    towardTip.Normalize();
                    joints[i + 1] = joints[i] + towardTip * boneLengths[i];
                }
            }

            Vector2 socket = root;
            float aheadHeading = anchorHeading;
            float aheadHeight = anchorHeight;
            float aheadWidth = anchorWidth;
            for (int i = 0; i < count; i++)
            {
                Vector2 desiredDirection = joints[i] - joints[i + 1];
                float desiredHeading = (float)Math.Atan2(desiredDirection.Y, desiredDirection.X);
                float linkLength = (aheadHeight + heights[i]) * 0.5f - SegmentOverlap;
                float comfort = ComfortBend(linkLength, widths[i], aheadWidth, int.MaxValue, flexibility);
                float bend = MathHelper.WrapAngle(desiredHeading - aheadHeading);
                desiredHeading = aheadHeading + MathHelper.Clamp(bend, -comfort * HardBendFactor, comfort * HardBendFactor);
                float headingDelta = MathHelper.WrapAngle(desiredHeading - headings[i]);
                float jointTurnLimit = turnLimit * MathHelper.Lerp(0.5f, 1f, i / (float)Math.Max(1, count - 1));
                float headingFollow = 0.25f;
                if (turnLimit > TailSettleTurnRadians)
                {
                    headingFollow = 0.5f;
                }

                float headingStep = MathHelper.Clamp(headingDelta * headingFollow, -jointTurnLimit, jointTurnLimit);
                float newHeading = headings[i] + headingStep;
                float ownSocketLength = Math.Max(1f, heights[i] * 0.5f - SegmentOverlap * 0.5f);
                Vector2 newCenter = socket - HeadingToDirection(newHeading) * ownSocketLength;

                if (isBlocked != null && isBlocked(newCenter))
                {
                    for (int j = 1; j <= 8; j++)
                    {
                        int side = 1;
                        if (j % 2 == 0)
                        {
                            side = -1;
                        }

                        float candidateStep = headingStep + side * ((j + 1) / 2) * jointTurnLimit * 0.25f;
                        candidateStep = MathHelper.Clamp(candidateStep, -jointTurnLimit, jointTurnLimit);
                        float candidateHeading = headings[i] + candidateStep;
                        Vector2 candidateCenter = socket - HeadingToDirection(candidateHeading) * ownSocketLength;
                        if (!isBlocked(candidateCenter))
                        {
                            newHeading = candidateHeading;
                            newCenter = candidateCenter;
                            break;
                        }
                    }
                }

                centers[i] = newCenter;
                headings[i] = MathHelper.WrapAngle(newHeading);
                socket = newCenter - HeadingToDirection(newHeading) * ownSocketLength;
                aheadHeading = newHeading;
                aheadHeight = heights[i];
                aheadWidth = widths[i];
            }
        }

        //Bend and terrain corrections share the ordinary turn/motion budget. A later projection must
        //never undo that limit and whip the remainder of the chain across the arena in one tick.
        public static float SolveLink(Vector2 aheadCenter, float aheadHeading, float comfortBend, float selfHeading, float desiredHeading,
            float linkLength, Func<Vector2, bool> isBlocked, out Vector2 newCenter,
            Vector2? previousCenter = null, float floorCenterY = float.NaN, float maxMovement = MaxBodyStepPixels)
        {
            Vector2 oldCenter = aheadCenter - HeadingToDirection(selfHeading) * linkLength;
            if (previousCenter.HasValue)
            {
                oldCenter = previousCenter.Value;
            }

            float bend = MathHelper.WrapAngle(desiredHeading - aheadHeading);
            float constrainedHeading = aheadHeading + SoftLimit(bend, comfortBend);
            float gap = MathHelper.WrapAngle(constrainedHeading - selfHeading);
            if (Math.Abs(gap) < HeadingDeadzoneRadians)
            {
                gap = 0f;
            }

            float turn = MathHelper.Clamp(gap * HeadingFollow, -MaxTurnPerTickRadians, MaxTurnPerTickRadians);
            float goalHeading = selfHeading + turn;
            Vector2 toAhead = aheadCenter - oldCenter;
            float aheadDistance = toAhead.Length();
            float motionBearing = selfHeading;
            float motionHalfAngle = MathHelper.Pi;
            float movementBudget = maxMovement;
            if (aheadDistance > 0.001f)
            {
                //A valid old link plus a bounded parent always has a reachable point on this circle.
                //Only spawn overlap or an authoritative position correction can require a larger repair.
                float requiredRepair = Math.Abs(aheadDistance - linkLength);
                movementBudget = Math.Max(movementBudget, requiredRepair + 0.001f);
                motionBearing = (float)Math.Atan2(toAhead.Y, toAhead.X);
                float numerator = aheadDistance * aheadDistance + linkLength * linkLength - movementBudget * movementBudget;
                float denominator = 2f * aheadDistance * linkLength;
                float cosine = MathHelper.Clamp(numerator / denominator, -1f, 1f);
                motionHalfAngle = (float)Math.Acos(cosine);
            }

            float motionOffset = MathHelper.WrapAngle(goalHeading - motionBearing);
            goalHeading = motionBearing + MathHelper.Clamp(motionOffset, -motionHalfAngle, motionHalfAngle);
            float goalTurn = MathHelper.WrapAngle(goalHeading - selfHeading);
            goalHeading = selfHeading + MathHelper.Clamp(goalTurn, -MaxTurnPerTickRadians, MaxTurnPerTickRadians);
            newCenter = aheadCenter - HeadingToDirection(goalHeading) * linkLength;

            if (Vector2.DistanceSquared(newCenter, oldCenter) > movementBudget * movementBudget + 0.01f)
            {
                goalHeading = motionBearing;
                newCenter = aheadCenter - HeadingToDirection(goalHeading) * linkLength;
            }

            //Search only within this tick's allowed turn, retaining exact spacing and the motion cap.
            //If already embedded, keep the bounded proposal rather than snapping to a distant clear side.
            for (int i = 0; i < 26; i++)
            {
                float candidateHeading = goalHeading;
                if (i == 1)
                {
                    candidateHeading = selfHeading;
                }
                else if (i >= 2)
                {
                    int side = 1;
                    if (i % 2 != 0)
                    {
                        side = -1;
                    }

                    float fraction = ((i - 2) / 2 + 1) / 12f;
                    candidateHeading = selfHeading + side * MaxTurnPerTickRadians * fraction;
                }

                Vector2 candidateCenter = aheadCenter - HeadingToDirection(candidateHeading) * linkLength;
                float candidateStepSquared = Vector2.DistanceSquared(candidateCenter, oldCenter);
                bool withinMotionBudget = candidateStepSquared <= movementBudget * movementBudget + 0.01f;
                bool belowFloor = !float.IsNaN(floorCenterY) && candidateCenter.Y > floorCenterY + 0.5f;
                if (!withinMotionBudget || belowFloor)
                {
                    continue;
                }

                if (isBlocked != null && isBlocked(candidateCenter))
                {
                    continue;
                }

                newCenter = candidateCenter;
                return MathHelper.WrapAngle(candidateHeading);
            }

            return MathHelper.WrapAngle(goalHeading);
        }

        ///<summary>Unit vector for a heading angle (radians, +X = 0, +Y down as in Terraria).</summary>
        public static Vector2 HeadingToDirection(float heading)
        {
            return new Vector2((float)Math.Cos(heading), (float)Math.Sin(heading));
        }

        public static Vector2 CubicBezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            float u = 1f - t;
            return (u * u * u) * a + (3f * u * u * t) * b + (3f * u * t * t) * c + (t * t * t) * d;
        }
    }
}
