using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.OolacileSerpent;
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
        const int SmallStepTiles = 2;

        //Tallest obstacle the serpent will rear up and climb over (~half its body length).
        const int MaxClimbHeightTiles = 11;

        //How far below/above a chain-follow-predicted position we'll look for ground before treating the
        //segment as spanning a gap (and skipping the ground-snap correction).
        const int GroundSnapToleranceTiles = 4;

        const float GroundFollowLerp = 0.25f;
        const float ClimbRiseSpeed = 1.2f;
        //Max consecutive ticks of climbing before it must travel flat again. At 1.2px/tick this caps a single
        //climb at ~11 tiles -- roughly the intended MaxClimbHeight -- so a mis-read wall can't lift it forever.
        const int ClimbBudgetTicks = 150;

        //Follower spacing = average neighbour sprite-length minus a fixed pixel overlap. The sprites are trimmed
        //to sit SegmentOverlap px on top of each other for a seamless body (bump this up if you still see seams).
        const float SegmentOverlap = 3f;
        //The first HeadGlueSegments body pieces overlap much more so the head stays welded to the body on turns.
        const int HeadGlueSegments = 3;
        const float HeadGlueOverlap = 12f;

        //Facing only flips once the neighbour is clearly to one side -- kills per-frame flicker on near-vertical
        //stretches of body (steep slopes, the overhead C). See the spriteDirection block in Run().
        const float SpriteFlipDeadzone = 6f;
        //Segment rotation is eased rather than snapped, so a single-frame position spike can't whip the sprite.
        const float SegmentRotationLerp = 0.35f;

        //-- Anti-stuck failsafe --
        const int StuckSampleTicks = 90;       //no meaningful movement for this long (while unable to engage) -> stuck
        const float StuckMoveThreshold = 32f;  //px of travel that counts as "made progress"
        const int UnstickDurationTicks = 240;  //how long it phases straight at the player to free itself
        const float UnstickSpeed = 3.5f;

        //-- Unreachable -> wander -> despawn --
        const float AggroRangeTiles = 90f;     //beyond this (or no LOS) it counts as "can't reach the player"
        const int WanderStartTicks = 12 * 60;  //12s unreachable -> stop hunting, wander
        const int DespawnTicks = 60 * 60;      //60s unreachable -> give up and despawn (per design)
        const float WanderSpeedMul = 0.6f;

        //-- Kiting: don't endlessly ram the player. Approach at slow speed, stop at kite range, attack from
        //there, never reverse. When it reaches the player it may cross THROUGH to the far side.
        const float KiteRangeTiles = 15f;        //stop advancing once the player is within this horizontally
        const float DirDeadzoneTiles = 4f;       //hysteresis: don't flip facing until the player is this far to one side
        const float CrossOverTriggerTiles = 6f;  //this close -> a chance to cross to the far side
        const float CrossOverExitTiles = 18f;    //keep crossing until this far past the player on the far side
        const int CrossOverCooldownTicks = 360;
        const int CrossOverTriggerRoll = 150;

        //-- Charge --
        const int ChargeTelegraphTicks = 45;
        const int ChargeDurationTicks = 70;
        const int ChargeCooldownTicks = 480;
        const float ChargeSpeedMultiplier = 2.5f;   //x the (slow) base speed -> a telegraphed burst
        const int ChargeMinDistanceTiles = 20;
        const int ChargeTriggerRoll = 240;
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
                if (i >= count - 2) { arr[i] = body3; }
                else if (i >= count - 4) { arr[i] = body2; }
                else { arr[i] = body; }
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
                //Flip only when the piece ahead is CLEARLY to one side. The old version flipped whenever the two
                //X's differed by any amount (and forced +1 when equal), so any near-vertical stretch of body --
                //a steep slope, and every frame of the overhead C -- made the segment flicker its facing every
                //tick. That flicker is what read as the body "shaking violently".
                float aheadDx = Main.npc[(int)npc.ai[1]].position.X - npc.position.X;
                if (Math.Abs(aheadDx) > SpriteFlipDeadzone)
                {
                    npc.spriteDirection = aheadDx < 0f ? 1 : -1;
                }
            }

            if (npc.ai[3] > 0f)
            {
                npc.realLife = (int)npc.ai[3];
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
                    for (int m = 0; m < wormLength - 1; m++)
                    {
                        int npcType = (m == wormLength - 2 ? tailType : bodyTypes[m]);
                        int newnpcID = NPC.NewNPC(npc.GetSource_FromAI(), (int)npc.Center.X, (int)npc.Center.Y, npcType, npc.whoAmI);

                        Main.npc[newnpcID].ai[3] = npc.whoAmI;
                        Main.npc[newnpcID].realLife = npc.whoAmI;
                        Main.npc[newnpcID].ai[1] = npcID;
                        Main.npc[newnpcID].ai[2] = m; //this piece's 0-based position among the body pieces (front-free/rear-grounded split)
                        Main.npc[npcID].ai[0] = newnpcID;
                        Main.npc[npcID].localAI[0] = 2 + (m * 2);

                        npcID = newnpcID;
                    }
                    npc.netUpdate = true;
                }

                if (npc.type != headType && (!Main.npc[(int)npc.ai[1]].active || Main.npc[(int)npc.ai[1]].aiStyle != npc.aiStyle))
                {
                    npc.life = 0;
                    npc.HitEffect(0, 10.0);
                    npc.active = false;
                }
                if (npc.type != tailType && (!Main.npc[(int)npc.ai[0]].active || Main.npc[(int)npc.ai[0]].aiStyle != npc.aiStyle))
                {
                    npc.life = 0;
                    npc.HitEffect(0, 10.0);
                    npc.active = false;
                }
            }

            //Enforce phasing through terrain every frame -- the serpent is drawn behind tiles and must never get
            //shoved out of a hill by any stray vanilla collision. Cheap insurance against clip/pop.
            npc.noTileCollide = true;
            npc.noGravity = true;

            if (npc.type == headType)
            {
                RunHeadLocomotion(npc, maxSpeed);
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

                //During a tail attack the tail-end pieces are POSED into a C/S by the head each tick
                //(PoseTailStabArc). Those pieces must not run their own follow/ground-snap or they'd fight the pose.
                bool isPosedByArc = headData.TailStab != GreatSerpentHead.TailStabState.None && isTailAttackSeg;
                if (isPosedByArc)
                {
                    if (!isTail)
                    {
                        npc.damage = 0;
                    }
                    return; //head already set our position/rotation/damage this tick
                }

                if (isTail)
                {
                    npc.damage = 0; //tail only deals damage while the head's stab is driving it
                }

                RunBodyFollow(npc);

                if (isRearGrounded)
                {
                    ApplyGroundSnap(npc);
                }
                else if (headData.RippleTimer > 0)
                {
                    ApplyRippleOffset(npc, segmentIndex, headData);
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

        ///<summary>Distance-pursuit chain-follow, ported verbatim from AIWorm (GlobalNPC.cs:5091-5106).</summary>
        static void RunBodyFollow(NPC npc)
        {
            if (npc.ai[1] > 0f && npc.ai[1] < (float)Main.npc.Length)
            {
                Vector2 npcCenter = npc.Center;
                float offsetX = Main.npc[(int)npc.ai[1]].Center.X - npcCenter.X;
                float offsetY = Main.npc[(int)npc.ai[1]].Center.Y - npcCenter.Y;

                float dist = (float)Math.Sqrt((double)(offsetX * offsetX + offsetY * offsetY));
                if (dist < 0.01f)
                {
                    return;
                }
                //Ease the rotation instead of snapping it, so one jittery frame can't whip the whole sprite.
                npc.rotation = LerpAngle(npc.rotation, (float)Math.Atan2(offsetY, offsetX) + 1.57f, SegmentRotationLerp);

                //Link length = the average of the two neighbouring sprite lengths minus a fixed pixel overlap, so
                //the tight-trimmed edges sit SegmentOverlap px on top of each other for a seamless tube (works
                //across the taper where neighbour heights differ). Height is the along-chain length: the sprites
                //are drawn rotated so their height axis runs along the chain. First few segments overlap more so
                //the head never detaches from the body on a turn.
                NPC ahead = Main.npc[(int)npc.ai[1]];
                float overlap = (int)npc.ai[2] < HeadGlueSegments ? HeadGlueOverlap : SegmentOverlap;
                float linkLen = (npc.height + ahead.height) * 0.5f - overlap;
                if (linkLen < 2f)
                {
                    linkLen = 2f;
                }
                dist = (dist - linkLen) / dist;
                offsetX *= dist;
                offsetY *= dist;
                npc.velocity = default;
                npc.position.X += offsetX;
                npc.position.Y += offsetY;
            }
        }

        ///<summary>
        ///Rear-grounded segments only: after the chain-follow position is set, pull Y toward sensed ground
        ///height so the tail hugs slopes/hills. If no ground is found nearby (a gap or open water), skip the
        ///correction and leave the segment at its pure chain-follow position -- the rigid distance-chain then
        ///holds it suspended between its grounded neighbors, naturally spanning the gap instead of clipping.
        ///</summary>
        static void ApplyGroundSnap(NPC npc)
        {
            int centerTileX = (int)(npc.Center.X / TileSize);
            int predictedBottomTileY = (int)((npc.position.Y + npc.height) / TileSize);

            int groundTileY = FindGroundSurfaceTileYSmoothed(centerTileX, predictedBottomTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 2);
            if (groundTileY < 0)
            {
                return;
            }

            float targetY = (groundTileY * TileSize) - npc.height;
            npc.position.Y = MathHelper.Lerp(npc.position.Y, targetY, GroundFollowLerp);
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

            //Contact damage is OFF by default; only specific attack states below turn it back on.
            npc.damage = 0;

            if (poise.StaggerTimer > 0)
            {
                RunStaggerFlop(npc);
                return;
            }

            UpdateAcidBody(npc, data);

            if (data.ChargeCooldown > 0) { data.ChargeCooldown--; }
            if (data.RippleCooldown > 0) { data.RippleCooldown--; }
            if (data.AttackCooldown > 0) { data.AttackCooldown--; }
            if (data.TailStabCooldown > 0) { data.TailStabCooldown--; }
            if (data.CrossOverCooldown > 0) { data.CrossOverCooldown--; }

            //While busy with a deliberate attack/hold, the head isn't "stuck" -- reset the detector so it doesn't
            //spuriously arm Unstick mid-attack (which the log showed happening during the tail stab).
            bool busy = data.TailStab != GreatSerpentHead.TailStabState.None
                || data.Attack != GreatSerpentHead.AttackState.None
                || data.ChargeTelegraphTimer > 0 || data.ChargeTimer > 0;
            if (busy)
            {
                data.StuckTimer = 0;
                data.StuckCheckPos = npc.Center;
            }

            //Unreachable tracking: if it can neither see nor get near the player, it eventually wanders and then
            //despawns. Only counts when NOT mid-attack (a committed attack is legitimate activity).
            bool canReach = Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height)
                && Vector2.Distance(npc.Center, player.Center) <= AggroRangeTiles * TileSize;
            if (canReach || busy)
            {
                data.UnreachableTimer = 0;
            }
            else
            {
                data.UnreachableTimer++;
            }

            if (data.UnreachableTimer >= DespawnTicks)
            {
                DespawnPoof(npc);
                return;
            }
            if (!busy && data.UnstickTimer <= 0 && data.UnreachableTimer >= WanderStartTicks)
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

            //Failsafe: wedged / marooned -> phase straight toward the player through terrain until free.
            if (data.UnstickTimer > 0)
            {
                RunUnstick(npc, data, player);
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
        ///wants to be doing something (i.e. it isn't legitimately kiting-with-LOS), arm the Unstick failsafe.
        ///Without this the serpent can be permanently marooned: it only ever rides the surface beneath it, so
        ///once it climbs into a room above the player there is no move that gets it back down.
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
                data.UnstickTimer = UnstickDurationTicks;
                data.ClimbBudget = ClimbBudgetTicks;
                npc.netUpdate = true;
            }
        }

        ///<summary>Unstick: swim straight at the player through terrain (it already phases through tiles and draws
        ///behind them). Ends early the moment it has a clear line to the player again.</summary>
        static void RunUnstick(NPC npc, GreatSerpentHead data, Player player)
        {
            data.LastAction = "unstick";
            data.UnstickTimer--; //RunUnstick owns its own countdown now (the dispatch returns before the detector)

            Vector2 toPlayer = player.Center - npc.Center;
            if (toPlayer.LengthSquared() > 1f)
            {
                toPlayer.Normalize();
            }
            npc.velocity = Vector2.Lerp(npc.velocity, toPlayer * UnstickSpeed, 0.08f);

            if (Math.Abs(npc.velocity.X) > 0.5f)
            {
                data.Facing = npc.velocity.X >= 0f ? 1 : -1;
            }
            npc.direction = data.Facing;
            npc.rotation = LerpAngle(npc.rotation, npc.velocity.ToRotation() + 1.57f, 0.15f);

            //Free once we can see the player again and we're roughly level with them.
            bool clear = Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height)
                && Math.Abs(player.Center.Y - npc.Center.Y) < KiteRangeTiles * TileSize;
            if (clear)
            {
                data.UnstickTimer = 0;
                data.StuckCheckPos = npc.Center;
                npc.netUpdate = true;
            }
        }

        ///<summary>Wander: slow ground pacing (no attacks) once the player is unreachable, turning at walls/cliffs.
        ///Buys time toward the despawn clock while looking alive rather than frozen.</summary>
        static void RunWander(NPC npc, GreatSerpentHead data, float maxSpeed = 4f)
        {
            data.LastAction = "wander";
            int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int centerTileX = (int)(npc.Center.X / TileSize);

            //Turn around at a wall we can't climb or at a cliff edge.
            int obs = GetObstacleHeightAhead(centerTileX + data.WanderDir * SmallStepTiles, feetTileY, MaxClimbHeightTiles + 2);
            int aheadGround = FindGroundSurfaceTileYSmoothed(centerTileX + data.WanderDir * 2, feetTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 3);
            if (obs > MaxClimbHeightTiles || aheadGround < 0)
            {
                data.WanderDir = -data.WanderDir;
            }

            data.Facing = data.WanderDir;
            npc.direction = data.WanderDir;
            npc.velocity.X = MathHelper.Lerp(npc.velocity.X, data.WanderDir * maxSpeed * WanderSpeedMul, 0.05f);
            SnapHeadToGround(npc, GroundFollowLerp);

            Vector2 heading = Math.Abs(npc.velocity.X) < 0.4f ? new Vector2(data.Facing, 0f) : npc.velocity;
            npc.rotation = LerpAngle(npc.rotation, heading.ToRotation() + 1.57f, 0.12f);
        }

        ///<summary>Give-up despawn: a dust poof and gone (kept separate from the boss's all-players-dead handler).</summary>
        static void DespawnPoof(NPC npc)
        {
            if (!Main.dedServ)
            {
                for (int i = 0; i < 40; i++)
                {
                    Vector2 v = Main.rand.NextVector2Circular(8f, 8f);
                    int d = Dust.NewDust(npc.position, npc.width, npc.height, DustID.AncientLight, v.X, v.Y, 150, Color.Purple, 2f);
                    Main.dust[d].noGravity = true;
                }
            }
            npc.active = false; //body pieces die on their next tick when the head is gone
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
            npc.rotation = MathHelper.Lerp(npc.rotation, 0f, 0.15f);
        }

        static void RunGroundMovement(NPC npc, GreatSerpentHead data, Player player, float maxSpeed)
        {
            float dx = player.Center.X - npc.Center.X;
            float absDx = Math.Abs(dx);

            //Facing hysteresis: only flip once the player is clearly to one side. Prevents the left/right
            //jitter (and the resulting body thrash) when the player sits right on top of the head's X.
            if (absDx > DirDeadzoneTiles * TileSize)
            {
                data.Facing = dx >= 0 ? 1 : -1;
            }
            npc.direction = data.Facing;

            //Decide horizontal intent: pursue / kite-stop / cross-over. It NEVER intentionally reverses.
            float desiredX;
            if (data.CrossingOver)
            {
                //Committed to crossing through to the far side: keep going in the crossover direction until
                //well past the player, then resume kiting from the new side (and favor an attack there).
                desiredX = data.CrossOverDir * maxSpeed;
                bool past = (data.CrossOverDir > 0 && npc.Center.X > player.Center.X + CrossOverExitTiles * TileSize)
                          || (data.CrossOverDir < 0 && npc.Center.X < player.Center.X - CrossOverExitTiles * TileSize);
                if (past)
                {
                    data.CrossingOver = false;
                    data.CrossOverCooldown = CrossOverCooldownTicks;
                    data.AttackCooldown = Math.Min(data.AttackCooldown, 20); //strike soon from the new side
                }
            }
            else if (absDx <= KiteRangeTiles * TileSize)
            {
                //In kite range: stop advancing (don't back up), hold and attack. Occasionally commit to a cross.
                desiredX = 0f;
                if (absDx <= CrossOverTriggerTiles * TileSize && data.CrossOverCooldown <= 0
                    && data.TailStab == GreatSerpentHead.TailStabState.None && Main.rand.NextBool(CrossOverTriggerRoll))
                {
                    data.CrossingOver = true;
                    data.CrossOverDir = data.Facing; //forward, through the player, to the far side
                }
            }
            else
            {
                desiredX = data.Facing * maxSpeed; //too far -> approach at slow pace
            }

            int moveDir = desiredX != 0f ? Math.Sign(desiredX) : data.Facing;
            int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int centerTileX = (int)(npc.Center.X / TileSize);

            //Obstacle handling only matters when actually advancing.
            int obstacleHeight = 0;
            if (desiredX != 0f)
            {
                int aheadTileX = centerTileX + moveDir * SmallStepTiles;
                obstacleHeight = GetObstacleHeightAhead(aheadTileX, feetTileY, MaxClimbHeightTiles + 2);
                if (obstacleHeight > SmallStepTiles)
                {
                    int obstacleHeightNext = GetObstacleHeightAhead(aheadTileX + moveDir, feetTileY, MaxClimbHeightTiles + 2);
                    if (obstacleHeightNext <= SmallStepTiles)
                    {
                        obstacleHeight = 0; //ignore lone 1-wide spikes
                    }
                }
            }

            //Headroom above the head: if solid sits right above us we must NOT keep rising, or we bore straight
            //up into a ceiling and end up marooned in the room above (exactly the reported bug).
            int headTileY = (int)(npc.position.Y / TileSize);
            bool hasHeadroom = !IsSolidTile(centerTileX, headTileY - 1) && !IsSolidTile(centerTileX, headTileY - 2);

            if (obstacleHeight > SmallStepTiles && obstacleHeight <= MaxClimbHeightTiles && hasHeadroom && data.ClimbBudget > 0)
            {
                //Climbing: rise while advancing. Budget-limited so a mis-read wall can't send it into orbit.
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, desiredX * 0.6f, 0.06f);
                npc.velocity.Y = -ClimbRiseSpeed;
                data.ClimbBudget--;
                data.LastAction = "climb";
            }
            else if (obstacleHeight > SmallStepTiles)
            {
                //Can't (or shouldn't) climb this: hold and fight from here. Never reverses.
                desiredX = 0f;
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, 0f, 0.1f);
                SnapHeadToGround(npc, GroundFollowLerp);
                data.LastAction = hasHeadroom ? "blocked-tall" : "blocked-ceiling";
            }
            else
            {
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, desiredX, 0.06f);
                SnapHeadToGround(npc, GroundFollowLerp);
                //Recharge the climb allowance whenever we're travelling normally on the flat.
                data.ClimbBudget = ClimbBudgetTicks;
                data.LastAction = desiredX == 0f ? (data.CrossingOver ? "cross" : "kite") : "pursue";
            }

            //Heading: lie along the direction of travel. velocity.Y is now real (small on flat, tracks the slope
            //we're following), so this no longer points the head into the ground. When nearly still, hold flat
            //along the current facing so it doesn't spin from Atan2(~0,~0).
            Vector2 heading;
            if (Math.Abs(npc.velocity.X) < 0.4f)
            {
                heading = new Vector2(data.Facing, MathHelper.Clamp(npc.velocity.Y, -1f, 1f));
            }
            else
            {
                heading = new Vector2(npc.velocity.X, MathHelper.Clamp(npc.velocity.Y, -Math.Abs(npc.velocity.X), Math.Abs(npc.velocity.X)));
            }
            npc.rotation = LerpAngle(npc.rotation, heading.ToRotation() + 1.57f, 0.12f);

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

            //Charge: only from range, and not while kiting/crossing.
            if (!data.CrossingOver && data.ChargeCooldown <= 0 && distanceToPlayer >= ChargeMinDistanceTiles * TileSize && Main.rand.NextBool(ChargeTriggerRoll))
            {
                data.ChargeTelegraphTimer = ChargeTelegraphTicks;
                data.ChargeDirection = new Vector2(data.Facing, 0f);
                data.ChargeCooldown = ChargeCooldownTicks;
                npc.netUpdate = true;
            }

            TryStartAttack(npc, data, player, distanceToPlayer);

            //Tail attack triggers in two situations (StartTailStab picks C vs S from the geometry):
            //  1. Settled in kite range with LOS -> head faces the player -> overhead C.
            //  2. The snake straddles the player (it slithered past, player now between head and tail) and the
            //     tail is close to the player -> horizontal S. This one can fire even while crossing over.
            if (data.TailStab == GreatSerpentHead.TailStabState.None && data.TailStabCooldown <= 0
                && Main.rand.NextBool(TailStabTriggerRoll))
            {
                bool kiteCase = !data.CrossingOver && absDx <= KiteRangeTiles * TileSize
                    && Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height);

                bool straddleCase = false;
                NPC tail = FindTail(npc);
                if (tail != null)
                {
                    bool between = (player.Center.X - npc.Center.X) * (player.Center.X - tail.Center.X) < 0f;
                    straddleCase = between && Vector2.Distance(tail.Center, player.Center) <= TailStabReachTiles * TileSize;
                }

                if (kiteCase || straddleCase)
                {
                    StartTailStab(npc, data, player);
                }
            }
        }

        ///<summary>Shortest-path angle lerp (handles the +/-2pi wrap so the head never spins the long way round).</summary>
        static float LerpAngle(float from, float to, float t)
        {
            float delta = MathHelper.WrapAngle(to - from);
            return from + delta * t;
        }

        ///<summary>
        ///Drive the head's vertical toward the sensed surface via VELOCITY, not by writing position directly.
        ///The engine applies npc.velocity to npc.position after AI, so setting position.Y here AND leaving a
        ///stale velocity.Y (which is exactly what was happening) double-moved the head down every frame -- that
        ///was the "segments sink into the ground" + "head rotates to look into the ground" bug (a stale
        ///velocity.Y also fed the rotation). Now velocity.Y IS the real vertical motion, so rotation from
        ///velocity is correct and the body rides the surface.
        ///</summary>
        static void SnapHeadToGround(NPC npc, float lerp)
        {
            int centerTileX = (int)(npc.Center.X / TileSize);
            int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int groundTileY = FindGroundSurfaceTileYSmoothed(centerTileX, feetTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 2);
            if (groundTileY < 0)
            {
                npc.velocity.Y = 0f; //over a gap -- glide flat, never fall
                return;
            }
            float targetY = (groundTileY * TileSize) - npc.height;
            npc.velocity.Y = (targetY - npc.position.Y) * lerp; //converge on the surface; engine applies it
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

        ///<summary>Body segments while AcidBody is active: purple dust, and drop a 6s AcidPool on the ground
        ///tile under the segment each time it slides onto a new column. localAI[1] remembers the last column
        ///this segment dropped on (localAI[0] is the AIWorm sync counter -- don't touch it).</summary>
        static void ApplyAcidBodyTrail(NPC npc)
        {
            int dust = Dust.NewDust(npc.position, npc.width, npc.height, DustID.AncientLight, 0f, -0.5f, 150, Color.Purple, 1.0f);
            Main.dust[dust].noGravity = true;

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
            Vector2 poolCenter = new Vector2(centerTileX * TileSize + TileSize / 2f, groundTileY * TileSize - Projectiles.Enemy.AcidPool.PoolHeight / 2f);
            Projectile.NewProjectile(npc.GetSource_FromAI(), poolCenter, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.AcidPool>(), AcidBodyPoolDamage, 0f, Main.myPlayer, AcidBodyPoolLifetime);
        }

        //-- TailStab: above-ground tail strike, two modes (OverheadC / HorizontalS) --
        //The head holds on the ground (HoldGround) while the tail-END pieces are posed each tick along a curve
        //from a grounded anchor to the driven tail tip. Coil -> Aim -> Stab -> (Recover -> Aim)* -> Retract.
        //  OverheadC   = quadratic Bezier bowing UP over the head; used when the head faces the player.
        //  HorizontalS = cubic Bezier wiggling sideways near the ground; used when the head faced away/past.

        const int TailStabCoilTicks = 70;      //rear/cock into the pose (slow, deliberate)
        const int TailStabAimTicks = 48;       //warning window; target locks at aim start
        const int TailStabStabTicks = 28;      //the strike
        const int TailStabRecoverTicks = 34;   //return to the perch between combo strikes
        const int TailStabRetractTicks = 36;   //lower back to the ground for a gentle hand-off
        const int TailStabMaxCombo = 3;
        const int TailStabCooldownTicks = 600;
        const int TailStabTriggerRoll = 180;
        const int TailStabDamage = 45;
        const float TailStabReachTiles = 22f;  //if the player gets farther than this (uncommitted), the strike aborts
        const float TailStabOverheadHeight = 170f; //C perch height above the head
        const float TailStabTipLerp = 0.11f;       //how fast the tip chases its phase target (lower = smoother/slower)
        //Horizontal-S specifics
        const float TailStabSCockDist = 100f;  //how far back the tail cocks before the lash
        const float TailStabSRise = 46f;        //slight raise of the S off the ground
        const float TailStabSAmplitude = 64f;   //perpendicular size of the S wiggle

        static void StartTailStab(NPC npc, GreatSerpentHead data, Player player)
        {
            NPC tail = FindTail(npc);

            //Mode by geometry: if the player is BETWEEN the head and the tail (the snake slithered past and now
            //straddles them), the tail is on the player's side -> horizontal S. Otherwise the whole snake is to
            //one side of the player, head nearest -> the tail arcs over the head in an overhead C. (Using the
            //head/tail span rather than head.Facing, which flips to re-face the player the instant it crosses.)
            bool straddles = tail != null
                && (player.Center.X - npc.Center.X) * (player.Center.X - tail.Center.X) < 0f;
            data.TailStabMode = straddles ? GreatSerpentHead.TailStabKind.HorizontalS : GreatSerpentHead.TailStabKind.OverheadC;

            data.TailStab = GreatSerpentHead.TailStabState.Coiling;
            data.TailStabTimer = TailStabCoilTicks;
            data.TailStabCombo = 0;
            data.TailStabDamaging = false;
            data.RippleTimer = 0;
            data.TailStabTip = tail != null ? tail.Center : npc.Center;
            npc.netUpdate = true;
        }

        ///<summary>The "cocked/perched" tip position the tail returns to between strikes, per mode.</summary>
        static Vector2 TailStabPerch(NPC head, GreatSerpentHead data, Player player)
        {
            if (data.TailStabMode == GreatSerpentHead.TailStabKind.OverheadC)
            {
                //Above the head, biased to the tail side so the C reads as sweeping over from behind.
                return head.Center + new Vector2(-head.direction * 24f, -TailStabOverheadHeight);
            }
            //S: cock away from the player (wind up), slightly raised, measured from the (last-known) anchor.
            Vector2 anchor = data.TailStabAnchor == Vector2.Zero ? head.Center : data.TailStabAnchor;
            float away = Math.Sign(anchor.X - player.Center.X);
            if (away == 0f) { away = -head.direction; }
            return anchor + new Vector2(away * TailStabSCockDist, -TailStabSRise);
        }

        static void UpdateTailStab(NPC npc, GreatSerpentHead data, Player player)
        {
            data.TailStabTimer--;
            data.TailStabDamaging = false;
            Vector2 perch = TailStabPerch(npc, data, player);
            Vector2 tipTarget = perch;

            //Committed = the strike itself (Stabbing). Everything before is cancellable: if the player has run out
            //of reach, bail to Retracting. Reach is to the CLOSER of head/tail so the S (head far, tail near the
            //player) isn't wrongly aborted by the head's distance.
            NPC reachTail = FindTail(npc);
            float reachDist = Vector2.Distance(npc.Center, player.Center);
            if (reachTail != null)
            {
                reachDist = Math.Min(reachDist, Vector2.Distance(reachTail.Center, player.Center));
            }
            bool inReach = reachDist <= TailStabReachTiles * TileSize;
            bool cancellable = data.TailStab == GreatSerpentHead.TailStabState.Coiling
                || data.TailStab == GreatSerpentHead.TailStabState.Aiming
                || data.TailStab == GreatSerpentHead.TailStabState.Recover;
            if (cancellable && !inReach)
            {
                data.TailStab = GreatSerpentHead.TailStabState.Retracting;
                data.TailStabTimer = TailStabRetractTicks;
            }

            switch (data.TailStab)
            {
                case GreatSerpentHead.TailStabState.Coiling:
                    tipTarget = perch;
                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStab = GreatSerpentHead.TailStabState.Aiming;
                        data.TailStabTimer = TailStabAimTicks;
                        data.TailStabTarget = player.Center;
                    }
                    break;

                case GreatSerpentHead.TailStabState.Aiming:
                    tipTarget = perch;
                    //Warning: purple venom dust converging on the locked strike spot
                    if (data.TailStabTimer % 4 == 0)
                    {
                        Vector2 p = data.TailStabTarget + new Vector2(Main.rand.NextFloat(-24f, 24f), -Main.rand.NextFloat(20f, 64f));
                        int d = Dust.NewDust(p, 6, 6, DustID.AncientLight, 0f, 2f, 120, Color.Purple, 1.1f);
                        Main.dust[d].noGravity = true;
                    }
                    if (data.TailStabTimer == 8)
                    {
                        tsorcRevampAIs.SpawnTelegraphFlash(npc, Color.Purple, data.TailStabTarget);
                    }
                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStab = GreatSerpentHead.TailStabState.Stabbing;
                        data.TailStabTimer = TailStabStabTicks;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = -0.3f }, npc.Center);
                    }
                    break;

                case GreatSerpentHead.TailStabState.Stabbing:
                    tipTarget = data.TailStabTarget;
                    data.TailStabDamaging = true;
                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStab = GreatSerpentHead.TailStabState.Recover;
                        data.TailStabTimer = TailStabRecoverTicks;
                    }
                    break;

                case GreatSerpentHead.TailStabState.Recover:
                    tipTarget = perch;
                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStabCombo++;
                        if (data.TailStabCombo < TailStabMaxCombo)
                        {
                            data.TailStab = GreatSerpentHead.TailStabState.Aiming;
                            data.TailStabTimer = TailStabAimTicks;
                            data.TailStabTarget = player.Center;
                        }
                        else
                        {
                            data.TailStab = GreatSerpentHead.TailStabState.Retracting;
                            data.TailStabTimer = TailStabRetractTicks;
                        }
                    }
                    break;

                case GreatSerpentHead.TailStabState.Retracting:
                    //Lower the tip back toward the ground line behind the anchor for a soft hand-off to chain-follow.
                    tipTarget = data.TailStabAnchor + new Vector2(-npc.direction * 40f, 0f);
                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStab = GreatSerpentHead.TailStabState.None;
                        data.TailStabCooldown = TailStabCooldownTicks;
                        npc.netUpdate = true;
                    }
                    break;
            }

            data.TailStabTip = Vector2.Lerp(data.TailStabTip, tipTarget, TailStabTipLerp);
            PoseTailStabArc(npc, data);
        }

        ///<summary>Pose the tail-END pieces along a curve from the grounded anchor to the driven tip -- a
        ///quadratic C (bow up over the head) or a cubic S (wiggle sideways), per data.TailStabMode. Sets each
        ///piece's position/rotation and the tail's contact damage. Runs in the head's AI; posed pieces skip
        ///their own movement (see Run). Only the last TailAttackSegmentCount pieces participate.</summary>
        static void PoseTailStabArc(NPC head, GreatSerpentHead data)
        {
            int tailType = ModContent.NPCType<GreatSerpentTail>();
            int attackStart = GreatSerpentHead.BodySegmentCount - GreatSerpentHead.TailAttackSegmentCount;
            NPC anchorSeg = null;
            NPC tail = null;
            System.Collections.Generic.List<NPC> rear = new System.Collections.Generic.List<NPC>();

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

                bool isTail = current.type == tailType;
                int idx = (int)current.ai[2];
                if (!isTail && idx == attackStart - 1)
                {
                    anchorSeg = current;
                }
                if (isTail || idx >= attackStart)
                {
                    rear.Add(current);
                    if (isTail)
                    {
                        tail = current;
                    }
                }
            }

            if (anchorSeg == null || rear.Count == 0)
            {
                return;
            }

            data.TailStabAnchor = anchorSeg.Center;
            Vector2 anchor = data.TailStabAnchor;
            Vector2 tip = data.TailStabTip;

            //Keep the tip within reach so the posed body doesn't stretch into visible gaps: total reach is the
            //sum of the posed links (each ~ segment length minus the overlap).
            float linkLen = (tail != null ? tail.height : 44) - SegmentOverlap;
            float maxChord = rear.Count * linkLen * 0.9f;
            Vector2 av = tip - anchor;
            if (av.Length() > maxChord)
            {
                tip = anchor + Vector2.Normalize(av) * maxChord;
            }

            bool sMode = data.TailStabMode == GreatSerpentHead.TailStabKind.HorizontalS;

            //Curve control points. C: one control high above the midpoint (quadratic). S: two controls offset
            //perpendicular in opposite directions (cubic), so the body snakes into an S along the anchor->tip axis.
            Vector2 control = (anchor + tip) * 0.5f + new Vector2(0f, -Math.Max(TailStabOverheadHeight * 0.9f, av.Length() * 0.6f));
            Vector2 sc1 = Vector2.Zero, sc2 = Vector2.Zero;
            if (sMode)
            {
                Vector2 axis = tip - anchor;
                Vector2 perp = new Vector2(-axis.Y, axis.X);
                perp = perp.LengthSquared() > 0.01f ? Vector2.Normalize(perp) : new Vector2(0f, -1f);
                sc1 = anchor + axis * 0.33f + perp * TailStabSAmplitude;
                sc2 = anchor + axis * 0.66f - perp * TailStabSAmplitude;
            }

            int count = rear.Count;
            for (int i = 0; i < count; i++)
            {
                float t = (i + 1) / (float)count;
                float tNext = (i + 2) / (float)count;
                Vector2 pos = sMode ? CubicBezier(anchor, sc1, sc2, tip, t) : QuadBezier(anchor, control, tip, t);
                Vector2 nextPos = (i + 1 < count)
                    ? (sMode ? CubicBezier(anchor, sc1, sc2, tip, tNext) : QuadBezier(anchor, control, tip, tNext))
                    : tip;

                NPC seg = rear[i];
                seg.Center = pos;
                seg.velocity = Vector2.Zero;

                //Aim the tangent at the NEXT point and EASE it (a snapped rotation off a jittery single-frame tip
                //move caused the "violent shake"). spriteDirection is left to Run()'s deadzoned block.
                Vector2 segDir = nextPos - pos;
                if (segDir.LengthSquared() > 0.01f)
                {
                    seg.rotation = LerpAngle(seg.rotation, segDir.ToRotation() + 1.57f, SegmentRotationLerp);
                }

                seg.damage = (seg == tail && data.TailStabDamaging) ? TailStabDamage : 0;
            }
        }

        static Vector2 CubicBezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t)
        {
            float u = 1f - t;
            return (u * u * u) * a + (3f * u * u * t) * b + (3f * u * t * t) * c + (t * t * t) * d;
        }

        static Vector2 QuadBezier(Vector2 a, Vector2 b, Vector2 c, float t)
        {
            float u = 1f - t;
            return (u * u) * a + (2f * u * t) * b + (t * t) * c;
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
            Vector2 chargeHeading = npc.velocity.LengthSquared() < 0.25f ? new Vector2(npc.direction, 0f) : npc.velocity;
            npc.rotation = LerpAngle(npc.rotation, chargeHeading.ToRotation() + 1.57f, 0.2f);
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
            Vector2 heading = npc.velocity.LengthSquared() < 0.2f ? new Vector2(data.Facing, 0f) : npc.velocity;
            npc.rotation = LerpAngle(npc.rotation, heading.ToRotation() + 1.57f, 0.1f);

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
            npc.rotation = LerpAngle(npc.rotation, 0f, 0.15f);
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
        const int BiteTelegraphTicks = 90;
        const int BiteLungeTicks = 22;
        const int BiteRecoverTicks = 60;
        const float BiteArchPixels = 50f;
        const float BiteLungeSpeed = 13f;   //slower strike to match the slower boss (still a quick lunge)
        const int BiteContactDamage = 60;

        //SnakePounce: forebody raises high into a true S, purple mouth dust, then a bigger lunge.
        const int PounceTelegraphTicks = 200;
        const int PounceLungeTicks = 26;
        const int PounceRecoverTicks = 90;
        const float PounceRaisePixels = 160f;
        const float PounceLungeSpeed = 15f;
        const int PounceContactDamage = 50;

        //Sweeping fire breath.
        const int BreathTelegraphTicks = 60;
        const int BreathSweepTicks = 90;
        const int BreathRecoverTicks = 120;
        const float BreathArchPixels = 40f;
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


        const int AttackCooldownBaseTicks = 240;
        const int AttackTriggerRoll = 45;
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
        static readonly int[] SpitPatternRecovery = { 150, 90, 90, 150, 90, 120 };

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

        static Vector2 EyePosition(NPC npc) => npc.Center + new Vector2(npc.direction * 22f, -14f) * npc.scale;
        //Mouth sits lower and further forward than the eye -- tuned so spit/breath/venom-dust emit from the jaw
        //tip, not above-and-behind it. (X = forward reach, Y = down from center.)
        static Vector2 MouthPosition(NPC npc) => npc.Center + new Vector2(npc.direction * 44f, 18f) * npc.scale;

        static void TryStartAttack(NPC npc, GreatSerpentHead data, Player player, float distanceToPlayer)
        {
            if (data.AttackCooldown > 0 || !Main.rand.NextBool(AttackTriggerRoll))
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
                if (roll < 50) { chosen = GreatSerpentHead.AttackState.BiteTelegraph; telegraph = BiteTelegraphTicks; }
                else if (roll < 70) { chosen = GreatSerpentHead.AttackState.PounceTelegraph; telegraph = PounceTelegraphTicks; }
                else if (roll < 85) { chosen = GreatSerpentHead.AttackState.BreathTelegraph; telegraph = BreathTelegraphTicks; }
                else { chosen = GreatSerpentHead.AttackState.SpitTelegraph; telegraph = SpitTelegraphTicks; }
            }
            else if (distTiles <= FarAttackRangeTiles)
            {
                int roll = Main.rand.Next(100);
                if (roll < 30) { chosen = GreatSerpentHead.AttackState.BreathTelegraph; telegraph = BreathTelegraphTicks; }
                else if (roll < 70) { chosen = GreatSerpentHead.AttackState.SpitTelegraph; telegraph = SpitTelegraphTicks; }
                else { chosen = GreatSerpentHead.AttackState.PounceTelegraph; telegraph = PounceTelegraphTicks; }
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
            npc.netUpdate = true;
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
                    }
                    if (data.AttackTimer <= 0)
                    {
                        StartLunge(npc, data, player, BiteLungeSpeed);
                        data.Attack = GreatSerpentHead.AttackState.BiteLunge;
                        data.AttackTimer = BiteLungeTicks;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, PitchVariance = 0.2f }, npc.Center);
                    }
                    break;
                }
                case GreatSerpentHead.AttackState.BiteLunge:
                {
                    SetCommittedPoise(poise);
                    npc.velocity = data.LungeVelocity * (IsInWater(npc) ? WaterLungeMultiplier : 1f);
                    npc.damage = BiteContactDamage; //head contact damage is ON only during the lunge
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
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
                    }
                    if (data.AttackTimer <= 0)
                    {
                        StartLunge(npc, data, player, PounceLungeSpeed);
                        data.Attack = GreatSerpentHead.AttackState.PounceLunge;
                        data.AttackTimer = PounceLungeTicks;
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
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
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
                    if (data.AttackTimer == TelegraphCommitTicks)
                    {
                        tsorcRevampAIs.SpawnTelegraphFlash(npc, Color.Orange, EyePosition(npc));
                    }
                    if (data.AttackTimer <= 0)
                    {
                        data.BreathBaseAngle = (player.Center - MouthPosition(npc)).ToRotation();
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
                    npc.rotation = angle + 1.57f;
                    if (data.AttackTimer % 3 == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 vel = angle.ToRotationVector2() * 10f;
                        Projectile.NewProjectile(npc.GetSource_FromThis(), MouthPosition(npc), vel, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), BreathDamage, 1f, Main.myPlayer);
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
                    npc.rotation = (player.Center - npc.Center).ToRotation() + 1.57f;

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
            npc.rotation = (player.Center - npc.Center).ToRotation() + 1.57f;
        }

        static void StartLunge(NPC npc, GreatSerpentHead data, Player player, float speed)
        {
            Vector2 toPlayer = player.Center - npc.Center;
            if (toPlayer.LengthSquared() < 1f)
            {
                toPlayer = new Vector2(npc.direction, 0f);
            }
            toPlayer.Normalize();
            data.LungeVelocity = toPlayer * speed; //locked at launch, no homing -- dodge by moving off the line
        }

        static void RunRecovery(NPC npc)
        {
            npc.velocity.X *= 0.9f;
            SnapHeadToGround(npc, GroundFollowLerp); //drives velocity.Y -- keeps the head on the surface, no sink
            npc.rotation = LerpAngle(npc.rotation, 0f, 0.1f);
        }

        static void EndAttack(NPC npc, GreatSerpentHead data, tsorcRevampGlobalNPC poise)
        {
            data.Attack = GreatSerpentHead.AttackState.None;
            data.MouthTransitionTimer = 0;
            data.AttackCooldown = AttackCooldownBaseTicks + Main.rand.Next(120);
            ClearAttackPoise(poise);
            npc.netUpdate = true;
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
                Projectile.NewProjectile(npc.GetSource_FromThis(), mouth, velocity, ModContent.ProjectileType<Projectiles.Enemy.VenomSpit>(), SpitDamage, 1f, Main.myPlayer, gravity);
            }
        }

        //-- Diagnostics --

        ///<summary>Flip to false to silence the log once the boss is dialled in.</summary>
        public static bool DebugLogging = true;
        const int LogIntervalTicks = 12;
        static int _lastLogTick;

        ///<summary>
        ///Writes head state to Logs/tsorcRevamp-serpent.log (same convention as the SF4 nav log).
        ///Key tells: `act` = what the head decided this tick; `gY` = the ground row it sensed (-1 = none found,
        ///i.e. it thinks it's over a void); `obs` = obstacle height ahead; `room` = headroom above (false while
        ///it's under a ceiling); `climb` = remaining climb budget; `stuck`/`unstick` = the failsafe.
        ///</summary>
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

                string sep = System.IO.Path.DirectorySeparatorChar.ToString();
                string dir = Main.SavePath + sep + "Logs";
                System.IO.Directory.CreateDirectory(dir);
                string path = dir + sep + "tsorcRevamp-serpent.log";

                string line = $"[{DateTime.Now:HH:mm:ss}] head#{npc.whoAmI}"
                    + $" pos=({npc.Center.X / TileSize:F1},{npc.Center.Y / TileSize:F1})"
                    + $" player=({player.Center.X / TileSize:F1},{player.Center.Y / TileSize:F1})"
                    + $" vel=({npc.velocity.X:F2},{npc.velocity.Y:F2})"
                    + $" face={data.Facing} rot={npc.rotation:F2} los={los}"
                    + $" act={data.LastAction}"
                    + $" gY={groundY} feetY={feetTileY} obs={obs} room={room} climb={data.ClimbBudget}"
                    + $" stuck={data.StuckTimer} unstick={data.UnstickTimer}"
                    + $" cross={data.CrossingOver}/{data.CrossOverCooldown}"
                    + $" atk={data.Attack}/{data.AttackTimer} atkCD={data.AttackCooldown}"
                    + $" stab={data.TailStab}/{data.TailStabTimer} stabCD={data.TailStabCooldown} combo={data.TailStabCombo}"
                    + $" acid={data.AcidBodyTimer}";
                System.IO.File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { }
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
        static bool IsSolidTile(int tileX, int tileY)
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
        static int GetObstacleHeightAhead(int tileX, int feetTileY, int maxUpTiles)
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
        static int FindGroundSurfaceTileY(int tileX, int startTileY, int maxDownTiles)
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
        ///Like FindGroundSurfaceTileY, but samples centerTileX-1/0/+1 and returns the deepest (largest tileY)
        ///reading. A lone 1-tile-wide bump or notch at exactly centerTileX gets smoothed away in favor of its
        ///flatter neighbors -- real multi-tile slopes/hills still come through since their neighbors agree.
        ///</summary>
        static int FindGroundSurfaceTileYSmoothed(int centerTileX, int startTileY, int maxDownTiles)
        {
            int result = -1;
            for (int dx = -1; dx <= 1; dx++)
            {
                int y = FindGroundSurfaceTileY(centerTileX + dx, startTileY, maxDownTiles);
                if (y > result)
                {
                    result = y;
                }
            }
            return result;
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
}
