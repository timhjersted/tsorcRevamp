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

        //Follower spacing: <1 pulls each segment closer to the one ahead than its own width, so the (scale 1.3)
        //sprites overlap more and the body reads as a continuous smooth tube instead of separated beads.
        const float SegmentSpacingFactor = 0.82f;

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

        public static int[] BuildBodyTypes()
        {
            int body = ModContent.NPCType<OolacileSerpentBody>();
            int body2 = ModContent.NPCType<OolacileSerpentBody2>();
            int body3 = ModContent.NPCType<OolacileSerpentBody3>();

            return new int[]
            {
                body, body, body, body, body, body, body, body, body, body,
                body, body, body, body, body, body, body2, body2, body3, body3
            };
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
                bool isRearGrounded = isTail || segmentIndex >= OolacileSerpentHead.FrontFreeSegmentCount;

                OolacileSerpentHead headData = GetHeadData(npc);
                if (headData == null)
                {
                    RunBodyFollow(npc);
                    return;
                }

                //During the overhead tail stab the rear half (junction..tail) is POSED into a C by the head each
                //tick (PoseTailStabArc). Those pieces must not run their own follow/ground-snap or they'd fight
                //the pose. isPosedByArc mirrors the range PoseTailStabArc drives.
                bool isPosedByArc = headData.TailStab != OolacileSerpentHead.TailStabState.None && isRearGrounded;
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

        static OolacileSerpentHead GetHeadData(NPC npc)
        {
            int headIndex = (int)npc.ai[3];
            if (headIndex < 0 || headIndex >= Main.npc.Length || !Main.npc[headIndex].active)
            {
                return null;
            }
            return Main.npc[headIndex].ModNPC as OolacileSerpentHead;
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
                dist = (dist - npc.width * SegmentSpacingFactor) / dist;
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
        static void ApplyRippleOffset(NPC npc, int segmentIndex, OolacileSerpentHead headData)
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
            OolacileSerpentHead data = npc.ModNPC as OolacileSerpentHead;
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

            UpdateStuckDetector(npc, data, player);

            //Overhead tail stab is its own head-state: the head holds on the ground (body settles) while the
            //rear half rears up and arcs over to strike. Runs to completion before the head does anything else.
            if (data.TailStab != OolacileSerpentHead.TailStabState.None)
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

            if (data.Attack != OolacileSerpentHead.AttackState.None)
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
        static void UpdateStuckDetector(NPC npc, OolacileSerpentHead data, Player player)
        {
            if (data.UnstickTimer > 0)
            {
                data.UnstickTimer--;
                return;
            }

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
        static void RunUnstick(NPC npc, OolacileSerpentHead data, Player player)
        {
            data.LastAction = "unstick";

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

        static void RunGroundMovement(NPC npc, OolacileSerpentHead data, Player player, float maxSpeed)
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
                    && data.TailStab == OolacileSerpentHead.TailStabState.None && Main.rand.NextBool(CrossOverTriggerRoll))
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
                npc.position.Y -= ClimbRiseSpeed;
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

            //Stable heading: use velocity while actually moving, else point flat along the current facing.
            //(Deriving rotation from a near-zero velocity via Atan2 was a big source of the head "freaking out".)
            Vector2 heading = npc.velocity;
            if (heading.LengthSquared() < 0.25f)
            {
                heading = new Vector2(data.Facing, 0f);
            }
            float targetRot = heading.ToRotation() + 1.57f;
            npc.rotation = LerpAngle(npc.rotation, targetRot, 0.15f);

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

            //Overhead tail stab: when settled in kite range (not chasing/crossing), off cooldown, with LOS.
            if (data.TailStab == OolacileSerpentHead.TailStabState.None && data.TailStabCooldown <= 0
                && !data.CrossingOver && absDx <= KiteRangeTiles * TileSize
                && Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height)
                && Main.rand.NextBool(TailStabTriggerRoll))
            {
                StartTailStab(npc, data);
            }
        }

        ///<summary>Shortest-path angle lerp (handles the +/-2pi wrap so the head never spins the long way round).</summary>
        static float LerpAngle(float from, float to, float t)
        {
            float delta = MathHelper.WrapAngle(to - from);
            return from + delta * t;
        }

        ///<summary>Snap the head Y toward the sensed surface, and hard-clamp so it never sinks below it.</summary>
        static void SnapHeadToGround(NPC npc, float lerp)
        {
            int centerTileX = (int)(npc.Center.X / TileSize);
            int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int groundTileY = FindGroundSurfaceTileYSmoothed(centerTileX, feetTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 2);
            if (groundTileY < 0)
            {
                return; //over a gap -- glide across at current height
            }
            float targetY = (groundTileY * TileSize) - npc.height;
            npc.position.Y = MathHelper.Lerp(npc.position.Y, targetY, lerp);
            if (npc.position.Y > targetY)
            {
                npc.position.Y = targetY; //never let the head dip below the surface
            }
        }

        //-- AcidBody --

        ///<summary>Below 50% HP: cycle 10s of acid-trailing on / 10s off. Body segments read AcidBodyTimer.</summary>
        static void UpdateAcidBody(NPC npc, OolacileSerpentHead data)
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

        //-- TailStab: overhead C-shape strike --
        //The head holds on the ground (HoldGround) while the rear half (junction..tail) is posed each tick along
        //a quadratic Bezier that bows UP and over the head. The tail tip is driven Coil -> Aim -> Stab ->
        //(Recover -> Aim)* -> Retract. Entirely above ground -- no burrowing.

        const int TailStabCoilTicks = 55;      //rear up into the C (slow, deliberate)
        const int TailStabAimTicks = 42;       //warning window; target locks at aim start
        const int TailStabStabTicks = 20;      //the downward strike
        const int TailStabRecoverTicks = 26;   //rise back to the overhead perch between combo stabs
        const int TailStabRetractTicks = 30;   //lower back to the ground for a gentle hand-off
        const int TailStabMaxCombo = 3;
        const int TailStabCooldownTicks = 540;
        const int TailStabTriggerRoll = 150;
        const int TailStabDamage = 45;
        const float TailStabOverheadHeight = 150f; //perch height above the head
        const float TailStabTipLerp = 0.16f;       //how fast the tip chases its phase target (lower = smoother/slower)

        static void StartTailStab(NPC npc, OolacileSerpentHead data)
        {
            data.TailStab = OolacileSerpentHead.TailStabState.Coiling;
            data.TailStabTimer = TailStabCoilTicks;
            data.TailStabCombo = 0;
            data.TailStabDamaging = false;
            data.RippleTimer = 0;
            NPC tail = FindTail(npc);
            data.TailStabTip = tail != null ? tail.Center : npc.Center;
            npc.netUpdate = true;
        }

        static Vector2 TailStabOverhead(NPC head)
        {
            //Perch above the head, biased slightly to the tail side so the C reads as sweeping over from behind.
            return head.Center + new Vector2(-head.direction * 24f, -TailStabOverheadHeight);
        }

        static void UpdateTailStab(NPC npc, OolacileSerpentHead data, Player player)
        {
            data.TailStabTimer--;
            data.TailStabDamaging = false;
            Vector2 overhead = TailStabOverhead(npc);
            Vector2 tipTarget = overhead;

            switch (data.TailStab)
            {
                case OolacileSerpentHead.TailStabState.Coiling:
                    tipTarget = overhead;
                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStab = OolacileSerpentHead.TailStabState.Aiming;
                        data.TailStabTimer = TailStabAimTicks;
                        data.TailStabTarget = player.Center;
                    }
                    break;

                case OolacileSerpentHead.TailStabState.Aiming:
                    tipTarget = overhead;
                    //Warning: purple venom dust raining down onto the locked strike spot
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
                        data.TailStab = OolacileSerpentHead.TailStabState.Stabbing;
                        data.TailStabTimer = TailStabStabTicks;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, Pitch = -0.3f }, npc.Center);
                    }
                    break;

                case OolacileSerpentHead.TailStabState.Stabbing:
                    tipTarget = data.TailStabTarget;
                    data.TailStabDamaging = true;
                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStab = OolacileSerpentHead.TailStabState.Recover;
                        data.TailStabTimer = TailStabRecoverTicks;
                    }
                    break;

                case OolacileSerpentHead.TailStabState.Recover:
                    tipTarget = overhead;
                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStabCombo++;
                        if (data.TailStabCombo < TailStabMaxCombo)
                        {
                            data.TailStab = OolacileSerpentHead.TailStabState.Aiming;
                            data.TailStabTimer = TailStabAimTicks;
                            data.TailStabTarget = player.Center;
                        }
                        else
                        {
                            data.TailStab = OolacileSerpentHead.TailStabState.Retracting;
                            data.TailStabTimer = TailStabRetractTicks;
                        }
                    }
                    break;

                case OolacileSerpentHead.TailStabState.Retracting:
                    //Lower the tip back toward the ground line behind the head for a soft hand-off to chain-follow.
                    tipTarget = data.TailStabAnchor + new Vector2(-npc.direction * 40f, 0f);
                    if (data.TailStabTimer <= 0)
                    {
                        data.TailStab = OolacileSerpentHead.TailStabState.None;
                        data.TailStabCooldown = TailStabCooldownTicks;
                        npc.netUpdate = true;
                    }
                    break;
            }

            data.TailStabTip = Vector2.Lerp(data.TailStabTip, tipTarget, TailStabTipLerp);
            PoseTailStabArc(npc, data);
        }

        ///<summary>Pose the rear half (junction body segment .. tail) along a quadratic Bezier from the grounded
        ///anchor, bowing up over the head, to the driven tail tip. Sets each piece's position/rotation and the
        ///tail's contact damage. Runs in the head's AI; the posed pieces skip their own movement (see Run).</summary>
        static void PoseTailStabArc(NPC head, OolacileSerpentHead data)
        {
            int tailType = ModContent.NPCType<OolacileSerpentTail>();
            NPC anchorSeg = null;
            NPC tail = null;
            System.Collections.Generic.List<NPC> rear = new System.Collections.Generic.List<NPC>();

            NPC current = head;
            for (int hops = 0; hops < OolacileSerpentHead.TotalSegmentCount + 2; hops++)
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
                if (!isTail && idx == OolacileSerpentHead.FrontFreeSegmentCount - 1)
                {
                    anchorSeg = current;
                }
                if (isTail || idx >= OolacileSerpentHead.FrontFreeSegmentCount)
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

            //Keep the tip within reach so the posed body doesn't stretch into visible gaps.
            float maxChord = rear.Count * (tail != null ? tail.width : 44) * SegmentSpacingFactor * 0.85f;
            Vector2 av = tip - anchor;
            if (av.Length() > maxChord)
            {
                tip = anchor + Vector2.Normalize(av) * maxChord;
            }

            //Bow the control point high above the midpoint so the body arcs UP into a C.
            float bow = Math.Max(TailStabOverheadHeight * 0.9f, Vector2.Distance(anchor, tip) * 0.6f);
            Vector2 control = (anchor + tip) * 0.5f + new Vector2(0f, -bow);

            int count = rear.Count;
            Vector2 prev = anchor;
            for (int i = 0; i < count; i++)
            {
                float t = (i + 1) / (float)count;
                Vector2 pos = QuadBezier(anchor, control, tip, t);
                NPC seg = rear[i];
                seg.Center = pos;
                seg.velocity = Vector2.Zero;

                //Aim the tangent at the NEXT point (or the tip for the last segment) so rotation is the arc's
                //true local direction, and EASE it -- a snapped rotation off a jittery single-frame tip move was
                //part of the "violent shake". spriteDirection is intentionally left to Run()'s deadzoned block
                //(setting it here from segDir.X flickered every frame on the near-vertical part of the C).
                Vector2 nextPos = (i + 1 < count) ? QuadBezier(anchor, control, tip, (i + 2) / (float)count) : tip;
                Vector2 segDir = nextPos - pos;
                if (segDir.LengthSquared() > 0.01f)
                {
                    seg.rotation = LerpAngle(seg.rotation, segDir.ToRotation() + 1.57f, SegmentRotationLerp);
                }
                prev = pos;

                seg.damage = (seg == tail && data.TailStabDamaging) ? TailStabDamage : 0;
            }
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
            for (int hops = 0; hops < OolacileSerpentHead.TotalSegmentCount + 2; hops++)
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

        static void RunCharge(NPC npc, OolacileSerpentHead data, float maxSpeed)
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

        static void RunSwim(NPC npc, OolacileSerpentHead data, Player player, float maxSpeed)
        {
            Vector2 toPlayer = player.Center - npc.Center;
            if (toPlayer.LengthSquared() > 1f)
            {
                toPlayer.Normalize();
            }
            Vector2 desired = toPlayer * maxSpeed * SwimSpeedMultiplier;
            npc.velocity = Vector2.Lerp(npc.velocity, desired, 0.05f);

            data.SwimWaveTimer += SwimSineFrequency;
            npc.position.Y += (float)Math.Sin(data.SwimWaveTimer) * SwimSineAmplitude * 0.05f;

            npc.direction = npc.velocity.X >= 0 ? 1 : -1;
            npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
        }

        //-- Stagger flop --

        static void RunStaggerFlop(NPC npc)
        {
            npc.velocity.X = 0f; //the poise system's ApplyStaggerMovement drives the knockback slide; we just settle Y/rotation
            int centerTileX = (int)(npc.Center.X / TileSize);
            int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int groundTileY = FindGroundSurfaceTileYSmoothed(centerTileX, feetTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 2);
            if (groundTileY >= 0)
            {
                float targetY = (groundTileY * TileSize) - npc.height;
                npc.position.Y = MathHelper.Lerp(npc.position.Y, targetY, GroundFollowLerp * 1.5f);
            }
            npc.rotation = MathHelper.Lerp(npc.rotation, 0f, 0.15f);
        }

        ///<summary>Called via IStaggerable so a poise break cancels any in-progress SerpentAI special state
        ///instead of leaving it stuck mid-attack once the flop ends.</summary>
        public static void OnStagger(NPC npc)
        {
            OolacileSerpentHead data = npc.ModNPC as OolacileSerpentHead;
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
            data.Attack = OolacileSerpentHead.AttackState.None;
            data.AttackTimer = 0;
            data.MouthTransitionTimer = 0;
            data.AttackCooldown = AttackCooldownBaseTicks;
            npc.damage = 0;
            ClearAttackPoise(npc.GetGlobalNPC<tsorcRevampGlobalNPC>());

            //Cancel an in-progress overhead tail stab -- the rear half falls back into the chain on its own once
            //TailStab clears (normal follow/ground-snap resumes). Half cooldown as the stagger tax.
            if (data.TailStab != OolacileSerpentHead.TailStabState.None)
            {
                data.TailStab = OolacileSerpentHead.TailStabState.None;
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

        static void TryStartAttack(NPC npc, OolacileSerpentHead data, Player player, float distanceToPlayer)
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
            OolacileSerpentHead.AttackState chosen;
            int telegraph;
            if (distTiles <= BiteRangeTiles)
            {
                int roll = Main.rand.Next(100);
                if (roll < 50) { chosen = OolacileSerpentHead.AttackState.BiteTelegraph; telegraph = BiteTelegraphTicks; }
                else if (roll < 70) { chosen = OolacileSerpentHead.AttackState.PounceTelegraph; telegraph = PounceTelegraphTicks; }
                else if (roll < 85) { chosen = OolacileSerpentHead.AttackState.BreathTelegraph; telegraph = BreathTelegraphTicks; }
                else { chosen = OolacileSerpentHead.AttackState.SpitTelegraph; telegraph = SpitTelegraphTicks; }
            }
            else if (distTiles <= FarAttackRangeTiles)
            {
                int roll = Main.rand.Next(100);
                if (roll < 30) { chosen = OolacileSerpentHead.AttackState.BreathTelegraph; telegraph = BreathTelegraphTicks; }
                else if (roll < 70) { chosen = OolacileSerpentHead.AttackState.SpitTelegraph; telegraph = SpitTelegraphTicks; }
                else { chosen = OolacileSerpentHead.AttackState.PounceTelegraph; telegraph = PounceTelegraphTicks; }
            }
            else
            {
                return;
            }

            data.Attack = chosen;
            data.AttackTimer = telegraph;
            data.MouthTransitionTimer = OolacileSerpentHead.MouthTransitionTicks;
            data.AttackAnchorY = npc.position.Y;
            if (chosen == OolacileSerpentHead.AttackState.SpitTelegraph)
            {
                data.SpitVariation = Main.rand.Next(SpitPatterns.Length);
                //Purple flash right away -- the whole 25-tick telegraph is the warning window
                tsorcRevampAIs.SpawnTelegraphFlash(npc, Color.Purple, EyePosition(npc));
            }
            npc.netUpdate = true;
        }

        static void RunAttack(NPC npc, OolacileSerpentHead data, tsorcRevampGlobalNPC poise)
        {
            Player player = Main.player[npc.target];
            data.AttackTimer--;

            switch (data.Attack)
            {
                case OolacileSerpentHead.AttackState.BiteTelegraph:
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
                        data.Attack = OolacileSerpentHead.AttackState.BiteLunge;
                        data.AttackTimer = BiteLungeTicks;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.8f, PitchVariance = 0.2f }, npc.Center);
                    }
                    break;
                }
                case OolacileSerpentHead.AttackState.BiteLunge:
                {
                    SetCommittedPoise(poise);
                    npc.velocity = data.LungeVelocity;
                    npc.damage = BiteContactDamage; //head contact damage is ON only during the lunge
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = OolacileSerpentHead.AttackState.BiteRecover;
                        data.AttackTimer = BiteRecoverTicks;
                    }
                    break;
                }
                case OolacileSerpentHead.AttackState.BiteRecover:
                {
                    ClearAttackPoise(poise);
                    RunRecovery(npc);
                    if (data.AttackTimer <= 0)
                    {
                        EndAttack(npc, data, poise);
                    }
                    break;
                }

                case OolacileSerpentHead.AttackState.PounceTelegraph:
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
                        data.Attack = OolacileSerpentHead.AttackState.PounceLunge;
                        data.AttackTimer = PounceLungeTicks;
                        npc.damage = PounceContactDamage;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.9f, Pitch = -0.2f }, npc.Center);
                    }
                    break;
                }
                case OolacileSerpentHead.AttackState.PounceLunge:
                {
                    SetCommittedPoise(poise);
                    npc.velocity = data.LungeVelocity;
                    npc.damage = PounceContactDamage; //ON every tick of the lunge (baseline is reset to 0 each tick)
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
                    SpawnMouthVenomDust(npc);
                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = OolacileSerpentHead.AttackState.PounceRecover;
                        data.AttackTimer = PounceRecoverTicks;
                    }
                    break;
                }
                case OolacileSerpentHead.AttackState.PounceRecover:
                {
                    ClearAttackPoise(poise);
                    RunRecovery(npc);
                    if (data.AttackTimer <= 0)
                    {
                        EndAttack(npc, data, poise);
                    }
                    break;
                }

                case OolacileSerpentHead.AttackState.BreathTelegraph:
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
                        data.Attack = OolacileSerpentHead.AttackState.BreathSweep;
                        data.AttackTimer = BreathSweepTicks;
                    }
                    break;
                }
                case OolacileSerpentHead.AttackState.BreathSweep:
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
                        data.Attack = OolacileSerpentHead.AttackState.BreathRecover;
                        data.AttackTimer = BreathRecoverTicks;
                    }
                    break;
                }
                case OolacileSerpentHead.AttackState.BreathRecover:
                {
                    ClearAttackPoise(poise);
                    RunRecovery(npc);
                    if (data.AttackTimer <= 0)
                    {
                        EndAttack(npc, data, poise);
                    }
                    break;
                }

                case OolacileSerpentHead.AttackState.SpitTelegraph:
                {
                    //The whole 25-tick window is the commit (flash already fired at entry)
                    SetCommittedPoise(poise);
                    HoldArch(npc, data, player, BreathArchPixels);
                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = OolacileSerpentHead.AttackState.SpitCombo;
                        data.SpitTick = 0;
                        SpitEvent[] pattern = SpitPatterns[data.SpitVariation];
                        data.AttackTimer = pattern[pattern.Length - 1].Tick + 1;
                    }
                    break;
                }
                case OolacileSerpentHead.AttackState.SpitCombo:
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
                        data.Attack = OolacileSerpentHead.AttackState.SpitRecover;
                        data.AttackTimer = SpitPatternRecovery[data.SpitVariation];
                    }
                    break;
                }
                case OolacileSerpentHead.AttackState.SpitRecover:
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
        static void HoldArch(NPC npc, OolacileSerpentHead data, Player player, float archPixels)
        {
            npc.velocity.X *= 0.9f;
            npc.velocity.Y = 0f;
            npc.position.Y = MathHelper.Lerp(npc.position.Y, data.AttackAnchorY - archPixels, 0.08f);
            npc.direction = player.Center.X >= npc.Center.X ? 1 : -1;
            npc.rotation = (player.Center - npc.Center).ToRotation() + 1.57f;
        }

        static void StartLunge(NPC npc, OolacileSerpentHead data, Player player, float speed)
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
            npc.velocity *= 0.9f;
            int centerTileX = (int)(npc.Center.X / TileSize);
            int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int groundTileY = FindGroundSurfaceTileYSmoothed(centerTileX, feetTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 2);
            if (groundTileY >= 0)
            {
                float targetY = (groundTileY * TileSize) - npc.height;
                npc.position.Y = MathHelper.Lerp(npc.position.Y, targetY, GroundFollowLerp);
            }
            npc.rotation = MathHelper.Lerp(npc.rotation, 0f, 0.1f);
        }

        static void EndAttack(NPC npc, OolacileSerpentHead data, tsorcRevampGlobalNPC poise)
        {
            data.Attack = OolacileSerpentHead.AttackState.None;
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
        static void SerpentLog(NPC npc, OolacileSerpentHead data, Player player)
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
