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

        const float GroundFollowLerp = 0.2f;
        const float ClimbRiseSpeed = 1.2f;

        //-- Burrow-and-resurface --
        const int FleeSustainedTicks = 90;          // ~1.5s of the player's distance steadily increasing
        const int FleeMinDistanceTiles = 30;         // don't bother burrowing to close a gap this small
        const int LOSLostTriggerTicks = 360;         // 6s, per design
        const int BurrowSharedCooldownTicks = 1800;  // 30s -- kept rare on purpose, other worms already do this often
        const int BurrowDescendTicks = 30;
        const int BurrowAscendTicks = 30;
        const float BurrowSubmergeDepth = 48f;
        const int BurrowLeadTiles = 12;               // how far ahead of the player's movement it aims to resurface
        const int BurrowLandingSearchTiles = 20;
        const float BurrowShakeStrength = 3f;         // "very minimal" shake per design
        const int BurrowShakeFrames = 10;

        //-- Charge --
        const int ChargeTelegraphTicks = 45;
        const int ChargeDurationTicks = 70;
        const int ChargeCooldownTicks = 480;
        const float ChargeSpeedMultiplier = 2.5f;
        const int ChargeMinDistanceTiles = 20;
        const int ChargeTriggerRoll = 240;

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
                if (npc.position.X > Main.npc[(int)npc.ai[1]].position.X || Math.Abs(npc.position.X - Main.npc[(int)npc.ai[1]].position.X) < 0.1f)
                {
                    npc.spriteDirection = 1;
                }
                if (npc.position.X < Main.npc[(int)npc.ai[1]].position.X)
                {
                    npc.spriteDirection = -1;
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

                //While the pierce is driving the tail (tunneling out, stabbing, tunneling home), the tail is a
                //MOVER, not a follower -- it stays chained via the rope pass inside RunTailPierce.
                if (isTail && headData.Pierce != OolacileSerpentHead.PierceState.None
                    && headData.Pierce != OolacileSerpentHead.PierceState.Sinking)
                {
                    RunTailPierce(npc, headData);
                    return;
                }
                if (isTail && headData.Pierce == OolacileSerpentHead.PierceState.None)
                {
                    npc.damage = 0; //safety: never keep stab damage outside the pierce
                }

                RunBodyFollow(npc);

                if (isRearGrounded)
                {
                    if (headData.Pierce != OolacileSerpentHead.PierceState.None)
                    {
                        //Stay hidden under the terrain contour; exempt the segments right at the stab column
                        //while the tail is erupting -- they're the visible part of the strike.
                        bool erupting = (headData.Pierce == OolacileSerpentHead.PierceState.Stabbing
                            || headData.Pierce == OolacileSerpentHead.PierceState.Retracting)
                            && Math.Abs(npc.Center.X - headData.PierceTarget.X) < TileSize * 4;
                        if (!erupting)
                        {
                            ClampUnderTerrain(npc);
                        }
                    }
                    //Don't fight the manual submerge/emerge offset with a terrain correction mid-burrow.
                    else if (headData.Burrow == OolacileSerpentHead.BurrowPhase.None)
                    {
                        ApplyGroundSnap(npc);
                    }
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

                npc.rotation = (float)Math.Atan2((double)offsetY, (double)offsetX) + 1.57f;
                float dist = (float)Math.Sqrt((double)(offsetX * offsetX + offsetY * offsetY));
                dist = (dist - (float)npc.width) / dist;
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

            if (poise.StaggerTimer > 0)
            {
                RunStaggerFlop(npc);
                return;
            }

            //Semi-independent systems: these tick alongside whatever the head itself is doing.
            UpdateAcidBody(npc, data);
            UpdatePierce(npc, data);

            if (data.Burrow != OolacileSerpentHead.BurrowPhase.None)
            {
                RunBurrow(npc, data);
                return;
            }

            if (data.Attack != OolacileSerpentHead.AttackState.None)
            {
                RunAttack(npc, data, poise);
                return;
            }

            if (data.ChargeTelegraphTimer > 0 || data.ChargeTimer > 0)
            {
                RunCharge(npc, data, maxSpeed);
                return;
            }

            Player player = Main.player[npc.target];

            if (data.BurrowCooldown > 0)
            {
                data.BurrowCooldown--;
            }
            if (data.ChargeCooldown > 0)
            {
                data.ChargeCooldown--;
            }
            if (data.RippleCooldown > 0)
            {
                data.RippleCooldown--;
            }
            if (data.AttackCooldown > 0)
            {
                data.AttackCooldown--;
            }

            UpdateBurrowTracking(npc, data, player);
            if (TryStartBurrow(npc, data, player))
            {
                RunBurrow(npc, data);
                return;
            }

            if (IsInWater(npc))
            {
                RunSwim(npc, data, player, maxSpeed);
                return;
            }

            RunGroundMovement(npc, data, player, maxSpeed);
        }

        static void RunGroundMovement(NPC npc, OolacileSerpentHead data, Player player, float maxSpeed)
        {
            int dir = player.Center.X >= npc.Center.X ? 1 : -1;
            npc.direction = dir;

            int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int centerTileX = (int)(npc.Center.X / TileSize);
            int aheadTileX = centerTileX + dir * SmallStepTiles;

            int obstacleHeight = GetObstacleHeightAhead(aheadTileX, feetTileY, MaxClimbHeightTiles + 2);
            if (obstacleHeight > SmallStepTiles)
            {
                //A lone 1-wide spike (torch, small decoration, single dirt clump) shouldn't make a boss this
                //size rear up -- only react if the height persists into the next column too.
                int obstacleHeightNext = GetObstacleHeightAhead(aheadTileX + dir, feetTileY, MaxClimbHeightTiles + 2);
                if (obstacleHeightNext <= SmallStepTiles)
                {
                    obstacleHeight = 0;
                }
            }

            float desiredX = dir * maxSpeed;

            if (obstacleHeight > MaxClimbHeightTiles)
            {
                int steerDir = FindShorterPathDirection(centerTileX, feetTileY, dir);
                desiredX = steerDir != 0 ? steerDir * maxSpeed * 0.5f : 0f;
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, desiredX, 0.08f);
            }
            else if (obstacleHeight > SmallStepTiles)
            {
                //Climbing: ease upward while still advancing at a reduced pace so the rise reads clearly.
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, desiredX * 0.6f, 0.08f);
                npc.position.Y -= ClimbRiseSpeed;
            }
            else
            {
                npc.velocity.X = MathHelper.Lerp(npc.velocity.X, desiredX, 0.08f);

                int groundTileY = FindGroundSurfaceTileYSmoothed(centerTileX, feetTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 2);
                if (groundTileY >= 0)
                {
                    float targetY = (groundTileY * TileSize) - npc.height;
                    npc.position.Y = MathHelper.Lerp(npc.position.Y, targetY, GroundFollowLerp);
                }
                //else: no ground sensed nearby (a gap) -- hold current height and glide across.
            }

            npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;

            //Idle flourish: only while doing plain grounded movement, gated by its own cooldown.
            if (data.RippleCooldown <= 0 && data.RippleTimer <= 0)
            {
                data.RippleTimer = RippleDurationTicks;
                data.RippleCooldown = RippleIdleCooldownTicks;
            }
            if (data.RippleTimer > 0)
            {
                data.RippleTimer--;
            }

            //Charge trigger: only when there's room to make a burst meaningful.
            float distanceToPlayer = Vector2.Distance(npc.Center, player.Center);
            if (data.ChargeCooldown <= 0 && distanceToPlayer >= ChargeMinDistanceTiles * TileSize && Main.rand.NextBool(ChargeTriggerRoll))
            {
                data.ChargeTelegraphTimer = ChargeTelegraphTicks;
                data.ChargeDirection = new Vector2(dir, 0f);
                data.ChargeCooldown = ChargeCooldownTicks;
                npc.netUpdate = true;
            }

            TryStartAttack(npc, data, player, distanceToPlayer);

            //GroundPierce trigger: player close and NOT fleeing (so the snake isn't actively pursuing) --
            //the rear half sneaks underground for tail stabs while the front keeps fighting normally.
            if (data.Pierce == OolacileSerpentHead.PierceState.None && data.PierceCooldown <= 0
                && data.FleeingTimer == 0 && distanceToPlayer <= PierceTriggerRangeTiles * TileSize
                && data.Burrow == OolacileSerpentHead.BurrowPhase.None && Main.rand.NextBool(PierceTriggerRoll))
            {
                data.Pierce = OolacileSerpentHead.PierceState.Sinking;
                data.PierceTimer = PierceSinkTicks;
                data.PierceCombo = 0;
                npc.netUpdate = true;
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

        //-- GroundPierce --

        ///<summary>Head-side pierce state machine. Runs alongside normal head AI (the head keeps fighting);
        ///rear segments and the tail read data.Pierce to know how to behave. The tail never detaches from the
        ///chain -- it tunnels to the target (Traveling), and back afterwards (Returning), with the rear body
        ///paid out behind it by the rope pass in RunTailPierce.</summary>
        static void UpdatePierce(NPC npc, OolacileSerpentHead data)
        {
            if (data.PierceCooldown > 0)
            {
                data.PierceCooldown--;
            }
            if (data.Pierce == OolacileSerpentHead.PierceState.None)
            {
                return;
            }

            Player player = Main.player[npc.target];
            data.PierceTimer--;

            switch (data.Pierce)
            {
                case OolacileSerpentHead.PierceState.Sinking:
                    if (data.PierceTimer <= 0)
                    {
                        data.Pierce = OolacileSerpentHead.PierceState.Traveling;
                        data.PierceTimer = 0;              //no minimum gap before the first stab
                        data.PierceTravelTimer = 0;
                        npc.netUpdate = true;
                    }
                    break;

                case OolacileSerpentHead.PierceState.Traveling:
                {
                    //The tail (RunTailPierce) is tunneling toward beneath the player. We just watch for
                    //arrival, enforce the minimum gap between stabs, and give up on a timeout.
                    data.PierceTravelTimer++;
                    if (data.PierceTravelTimer > PierceTravelTimeoutTicks)
                    {
                        BeginPierceReturn(npc, data);
                        break;
                    }
                    NPC tail = FindTail(npc);
                    if (tail == null)
                    {
                        EndPierce(npc, data);
                        break;
                    }
                    bool arrived = Math.Abs(tail.Center.X - player.Center.X) < TileSize;
                    if (arrived && data.PierceTimer <= 0)
                    {
                        BeginPierceAim(npc, data, player);
                    }
                    break;
                }

                case OolacileSerpentHead.PierceState.Aiming:
                    //Warning: dirt dusts across the 3 tiles where the stab will erupt, for the whole 30-tick window
                    if (data.PierceTimer % 4 == 0)
                    {
                        for (int i = -1; i <= 1; i++)
                        {
                            Vector2 pos = new Vector2(data.PierceTarget.X + i * TileSize - 4f, data.PierceGroundY - 6f);
                            int dust = Dust.NewDust(pos, 8, 6, DustID.Dirt, 0f, -2f, 60, default, 1.4f);
                            Main.dust[dust].velocity.X *= 0.3f;
                        }
                    }
                    if (data.PierceTimer <= 0)
                    {
                        data.Pierce = OolacileSerpentHead.PierceState.Stabbing;
                        data.PierceTimer = PierceStabTicks;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.WormDig with { Volume = 0.8f, Pitch = 0.3f }, data.PierceTarget);
                    }
                    break;

                case OolacileSerpentHead.PierceState.Stabbing:
                    if (data.PierceTimer <= 0)
                    {
                        data.Pierce = OolacileSerpentHead.PierceState.Retracting;
                        data.PierceTimer = PierceRetractTicks;
                    }
                    break;

                case OolacileSerpentHead.PierceState.Retracting:
                    if (data.PierceTimer <= 0)
                    {
                        data.PierceCombo++;
                        float distance = Vector2.Distance(npc.Center, player.Center);
                        if (data.PierceCombo < PierceMaxCombo && distance <= PierceTriggerRangeTiles * TileSize * 1.5f)
                        {
                            //Tunnel to the player's new position for the next stab; travel time IS the gap
                            //(with PierceComboGapTicks as the floor so point-blank re-stabs aren't instant)
                            data.Pierce = OolacileSerpentHead.PierceState.Traveling;
                            data.PierceTimer = PierceComboGapTicks;
                            data.PierceTravelTimer = 0;
                        }
                        else
                        {
                            BeginPierceReturn(npc, data);
                        }
                    }
                    break;

                case OolacileSerpentHead.PierceState.Returning:
                {
                    //Tail tunnels back toward its natural chain slot; end when it's close (or on timeout)
                    data.PierceTravelTimer++;
                    NPC tail = FindTail(npc);
                    if (tail == null || data.PierceTravelTimer > PierceTravelTimeoutTicks)
                    {
                        EndPierce(npc, data);
                        break;
                    }
                    NPC ahead = Main.npc[(int)tail.ai[1]];
                    if (!ahead.active || Vector2.Distance(tail.Center, ahead.Center) < tail.width * 2f)
                    {
                        EndPierce(npc, data);
                    }
                    break;
                }
            }
        }

        ///<summary>Lock the stab target to the player's position NOW (the moment the warning dusts start) and
        ///find the ground surface below it. No ground nearby (player airborne over a chasm) -> go home.</summary>
        static void BeginPierceAim(NPC npc, OolacileSerpentHead data, Player player)
        {
            int targetTileX = (int)(player.Center.X / TileSize);
            int playerFeetTileY = (int)((player.position.Y + player.height) / TileSize);
            int groundTileY = FindGroundSurfaceTileY(targetTileX, playerFeetTileY - 2, 10);
            if (groundTileY < 0)
            {
                BeginPierceReturn(npc, data);
                return;
            }

            data.PierceTarget = player.Center;
            data.PierceGroundY = groundTileY * TileSize;
            data.Pierce = OolacileSerpentHead.PierceState.Aiming;
            data.PierceTimer = PierceAimTicks;
            npc.netUpdate = true;
        }

        static void BeginPierceReturn(NPC npc, OolacileSerpentHead data)
        {
            data.Pierce = OolacileSerpentHead.PierceState.Returning;
            data.PierceTravelTimer = 0;
            npc.netUpdate = true;
        }

        static void EndPierce(NPC npc, OolacileSerpentHead data)
        {
            data.Pierce = OolacileSerpentHead.PierceState.None;
            data.PierceCooldown = PierceCooldownTicks;
            npc.netUpdate = true;
        }

        ///<summary>Walk the chain from the head to its last piece. Cheap (22 hops), called a few times per tick.</summary>
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

        ///<summary>Rear segments while a pierce is underway: never poke above the terrain surface. Unlike a
        ///hard snap-to-depth, this only pushes DOWN when a segment breaches (surface + PierceMinBuryPixels),
        ///so segments that followed the tail deeper (under a hill, down a dip) are left alone. This is what
        ///keeps the buried body hidden through uneven terrain.</summary>
        static void ClampUnderTerrain(NPC npc)
        {
            int centerTileX = (int)(npc.Center.X / TileSize);
            int bottomTileY = (int)((npc.position.Y + npc.height) / TileSize);
            int groundTileY = FindGroundSurfaceTileYSmoothed(centerTileX, bottomTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 2);
            if (groundTileY < 0)
            {
                return; //over a gap -- nothing to hide under; chain keeps it strung between neighbors
            }

            float surfaceY = groundTileY * TileSize;
            float buriedTopY = surfaceY + PierceMinBuryPixels; //segment top must be at/below this
            if (npc.position.Y < buriedTopY)
            {
                float before = npc.position.Y;
                npc.position.Y = MathHelper.Lerp(npc.position.Y, surfaceY + PierceSinkDepth - npc.height, GroundFollowLerp);

                //Dirt puff right at the surface while actually pushing through it
                if (Math.Abs(npc.position.Y - before) > 0.5f && Main.rand.NextBool(3))
                {
                    Vector2 pos = new Vector2(npc.Center.X - 8f, surfaceY - 6f);
                    int dust = Dust.NewDust(pos, 16, 6, DustID.Dirt, 0f, -1.5f, 80, default, 1.3f);
                    Main.dust[dust].velocity.X *= 0.4f;
                }
            }
        }

        ///<summary>
        ///Tail behavior during the driven pierce states. The tail is a MOVER here, not a follower: it tunnels
        ///along the terrain contour at PierceSinkDepth (so it wraps under hills and dips instead of cutting
        ///straight lines through them), and the rear body is dragged along behind it by TailRopePass. The time
        ///this takes is intentional -- repositioning the lower body is part of the attack's rhythm.
        ///</summary>
        static void RunTailPierce(NPC npc, OolacileSerpentHead data)
        {
            npc.velocity = Vector2.Zero;

            switch (data.Pierce)
            {
                case OolacileSerpentHead.PierceState.Traveling:
                {
                    Player player = Main.player[Main.npc[(int)npc.ai[3]].target];
                    TunnelToward(npc, player.Center.X);
                    npc.damage = 0;
                    break;
                }

                case OolacileSerpentHead.PierceState.Aiming:
                    //Hold buried under the locked target while the warning dusts play
                    npc.Center = new Vector2(
                        MathHelper.Lerp(npc.Center.X, data.PierceTarget.X, 0.2f),
                        MathHelper.Lerp(npc.Center.Y, data.PierceGroundY + PierceSinkDepth, 0.2f));
                    npc.damage = 0;
                    break;

                case OolacileSerpentHead.PierceState.Stabbing:
                {
                    float hiddenY = data.PierceGroundY + PierceSinkDepth;
                    float peakY = data.PierceTarget.Y - PierceStabHeight * 0.25f;
                    float progress = 1f - data.PierceTimer / (float)PierceStabTicks;
                    npc.Center = new Vector2(data.PierceTarget.X, MathHelper.Lerp(hiddenY, peakY, progress));
                    npc.rotation = 0f; //tip pointing straight up
                    npc.damage = PierceTailDamage;
                    if (Main.rand.NextBool(2))
                    {
                        Vector2 pos = new Vector2(data.PierceTarget.X - TileSize, data.PierceGroundY - 6f);
                        int dust = Dust.NewDust(pos, TileSize * 2, 6, DustID.Dirt, 0f, -2f, 60, default, 1.5f);
                        Main.dust[dust].velocity.X *= 0.5f;
                    }
                    break;
                }

                case OolacileSerpentHead.PierceState.Retracting:
                {
                    float hiddenY = data.PierceGroundY + PierceSinkDepth;
                    float peakY = data.PierceTarget.Y - PierceStabHeight * 0.25f;
                    float progress = 1f - data.PierceTimer / (float)PierceRetractTicks;
                    npc.Center = new Vector2(data.PierceTarget.X, MathHelper.Lerp(peakY, hiddenY, progress));
                    npc.rotation = 0f;
                    npc.damage = 0;
                    break;
                }

                case OolacileSerpentHead.PierceState.Returning:
                {
                    NPC ahead = Main.npc[(int)npc.ai[1]];
                    if (ahead.active)
                    {
                        TunnelToward(npc, ahead.Center.X);
                    }
                    npc.damage = 0;
                    break;
                }
            }

            //Drag the rear body along the tail's path (taut links only), keeping the chain connected
            TailRopePass(npc);
        }

        ///<summary>Tunnel horizontally toward targetX at PierceSinkDepth below the local terrain surface --
        ///following the contour, so the route dips under valleys and rises under hills.</summary>
        static void TunnelToward(NPC npc, float targetX)
        {
            float dx = targetX - npc.Center.X;
            float step = Math.Sign(dx) * Math.Min(Math.Abs(dx), PierceTravelSpeed);
            npc.position.X += step;

            int tileX = (int)(npc.Center.X / TileSize);
            int fromTileY = (int)(npc.Center.Y / TileSize) - 20;
            int groundTileY = FindGroundSurfaceTileY(tileX, fromTileY, 40);
            if (groundTileY >= 0)
            {
                //Ride the contour: tail center held PierceSinkDepth below the local surface
                float targetPosY = groundTileY * TileSize + PierceSinkDepth - npc.height / 2f;
                npc.position.Y = MathHelper.Lerp(npc.position.Y, targetPosY, 0.15f);
            }
            npc.rotation = (float)Math.Atan2(0f, step) + 1.57f;
        }

        ///<summary>
        ///Rope pass from the tail toward the head: each segment whose link to the piece BEHIND it (tail side)
        ///has gone taut gets pulled to link distance. Never pushes on slack links, so it does nothing while the
        ///chain has spare length and only "pays out" body when the tail actually needs it. Runs in the tail's
        ///AI (the last chain piece to update each tick), after the forward head-anchored follow has already run.
        ///</summary>
        static void TailRopePass(NPC tail)
        {
            NPC behind = tail; //"behind" in chain terms = closer to the tail
            for (int hops = 0; hops < OolacileSerpentHead.TotalSegmentCount; hops++)
            {
                if (behind.ai[1] <= 0f || (int)behind.ai[1] >= Main.npc.Length)
                {
                    break;
                }
                NPC segment = Main.npc[(int)behind.ai[1]];
                if (!segment.active || segment.ModNPC is OolacileSerpentHead)
                {
                    break; //never drag the head -- it's the other anchor
                }

                float link = behind.width; //forward follow spaces each follower at its own width
                Vector2 offset = segment.Center - behind.Center;
                float dist = offset.Length();
                if (dist > link)
                {
                    //Taut: pull this segment toward the tail-side piece, onto the link circle
                    segment.Center = behind.Center + offset * (link / dist);
                }
                else
                {
                    break; //slack from here on -- the disturbance has been fully absorbed
                }
                behind = segment;
            }
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

                int centerTileX = (int)(npc.Center.X / TileSize);
                int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
                int groundTileY = FindGroundSurfaceTileYSmoothed(centerTileX, feetTileY - GroundSnapToleranceTiles, GroundSnapToleranceTiles * 2);
                if (groundTileY >= 0)
                {
                    float targetY = (groundTileY * TileSize) - npc.height;
                    npc.position.Y = MathHelper.Lerp(npc.position.Y, targetY, GroundFollowLerp);
                }
            }

            npc.direction = data.ChargeDirection.X >= 0 ? 1 : -1;
            npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
        }

        //-- Burrow-and-resurface --

        static void UpdateBurrowTracking(NPC npc, OolacileSerpentHead data, Player player)
        {
            bool hasLOS = Collision.CanHit(npc.position, npc.width, npc.height, player.position, player.width, player.height);
            if (hasLOS)
            {
                data.NoLineOfSightTimer = 0;
            }
            else
            {
                data.NoLineOfSightTimer++;
            }

            float distance = Vector2.Distance(npc.Center, player.Center);
            if (distance > data.LastPlayerDistance + 0.25f)
            {
                data.FleeingTimer++;
            }
            else
            {
                data.FleeingTimer = 0;
            }
            data.LastPlayerDistance = distance;
        }

        static bool TryStartBurrow(NPC npc, OolacileSerpentHead data, Player player)
        {
            //Never relocate the chain while the rear half is committed underground for a pierce
            if (data.BurrowCooldown > 0 || data.Pierce != OolacileSerpentHead.PierceState.None)
            {
                return false;
            }

            float distance = Vector2.Distance(npc.Center, player.Center);
            bool fleeTrigger = data.FleeingTimer >= FleeSustainedTicks && distance >= FleeMinDistanceTiles * TileSize;
            bool losTrigger = data.NoLineOfSightTimer >= LOSLostTriggerTicks;

            if (!fleeTrigger && !losTrigger && !data.BurrowRequested)
            {
                return false;
            }
            data.BurrowRequested = false;

            Vector2 leadDir = player.velocity.LengthSquared() > 1f ? Vector2.Normalize(player.velocity) : new Vector2(Math.Sign(player.Center.X - npc.Center.X), 0f);
            Vector2 leadTarget = player.Center + leadDir * (BurrowLeadTiles * TileSize);

            if (!TryFindBurrowLanding(leadTarget, player, out Vector2 landing) && !TryFindBurrowLanding(player.Center, player, out landing))
            {
                return false; //nowhere safe to resurface right now -- the triggers will fire again later
            }

            data.Burrow = OolacileSerpentHead.BurrowPhase.Descending;
            data.BurrowTimer = BurrowDescendTicks;
            data.BurrowTargetHeadPos = landing;
            data.FleeingTimer = 0;
            data.NoLineOfSightTimer = 0;
            npc.netUpdate = true;
            return true;
        }

        static bool TryFindBurrowLanding(Vector2 near, Player player, out Vector2 landing)
        {
            int tileX = (int)(near.X / TileSize);
            int feetTileY = (int)((player.position.Y + player.height) / TileSize);
            int groundTileY = FindGroundSurfaceTileY(tileX, feetTileY - BurrowLandingSearchTiles, BurrowLandingSearchTiles * 2);
            if (groundTileY < 0)
            {
                landing = default;
                return false;
            }
            landing = new Vector2(tileX * TileSize + TileSize / 2f, groundTileY * TileSize);
            return true;
        }

        static void RunBurrow(NPC npc, OolacileSerpentHead data)
        {
            npc.velocity = Vector2.Zero;

            if (data.Burrow == OolacileSerpentHead.BurrowPhase.Descending)
            {
                data.BurrowTimer--;
                npc.position.Y += BurrowSubmergeDepth / BurrowDescendTicks;
                if (Main.rand.NextBool(3))
                {
                    SpawnBurrowDust(npc);
                }

                if (data.BurrowTimer <= 0)
                {
                    UsefulFunctions.ScreenShake(npc.Center, BurrowShakeStrength, BurrowShakeFrames);
                    Vector2 newHeadCenter = data.BurrowTargetHeadPos + new Vector2(0f, -npc.height / 2f + BurrowSubmergeDepth);
                    RelocateChain(npc, newHeadCenter);
                    SpawnBurrowDust(npc);

                    data.Burrow = OolacileSerpentHead.BurrowPhase.Ascending;
                    data.BurrowTimer = BurrowAscendTicks;
                }
            }
            else
            {
                data.BurrowTimer--;
                npc.position.Y -= BurrowSubmergeDepth / BurrowAscendTicks;
                if (Main.rand.NextBool(3))
                {
                    SpawnBurrowDust(npc);
                }

                if (data.BurrowTimer <= 0)
                {
                    UsefulFunctions.ScreenShake(npc.Center, BurrowShakeStrength, BurrowShakeFrames);
                    data.Burrow = OolacileSerpentHead.BurrowPhase.None;
                    data.BurrowCooldown = BurrowSharedCooldownTicks;
                    npc.netUpdate = true;
                }
            }
        }

        static void SpawnBurrowDust(NPC npc)
        {
            for (int i = 0; i < 6; i++)
            {
                Dust.NewDust(npc.position, npc.width, npc.height, DustID.Dirt, 0f, 0f, 100, default, 1.4f);
            }
        }

        ///<summary>Rigid-translate every piece by the same delta so the chain's shape (and per-segment
        ///distances) survives the relocation exactly -- no stretch, no re-catch-up frames needed.</summary>
        static void RelocateChain(NPC head, Vector2 newHeadCenter)
        {
            Vector2 delta = newHeadCenter - head.Center;
            NPC current = head;
            while (true)
            {
                current.position += delta;
                current.netUpdate = true;
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
        ///instead of leaving it stuck mid-burrow/mid-charge once the flop ends.</summary>
        public static void OnStagger(NPC npc)
        {
            OolacileSerpentHead data = npc.ModNPC as OolacileSerpentHead;
            if (data == null)
            {
                return;
            }

            bool wasBurrowed = data.Burrow != OolacileSerpentHead.BurrowPhase.None;
            data.Burrow = OolacileSerpentHead.BurrowPhase.None;
            data.ChargeTimer = 0;
            data.ChargeTelegraphTimer = 0;
            data.RippleTimer = 0;

            //Cancel an in-progress attack (windup or active) and put it on cooldown so the flop isn't
            //immediately followed by the attack it interrupted.
            data.Attack = OolacileSerpentHead.AttackState.None;
            data.AttackTimer = 0;
            data.MouthTransitionTimer = 0;
            data.AttackCooldown = AttackCooldownBaseTicks;
            npc.damage = 60;
            ClearAttackPoise(npc.GetGlobalNPC<tsorcRevampGlobalNPC>());

            //Cancel an in-progress ground pierce -- the rear half rises back out on its own once Pierce ends
            //(the segments' normal ground-snap pulls them back to the surface). Half cooldown as the stagger tax.
            if (data.Pierce != OolacileSerpentHead.PierceState.None)
            {
                data.Pierce = OolacileSerpentHead.PierceState.None;
                data.PierceCooldown = PierceCooldownTicks / 2;
            }

            if (wasBurrowed)
            {
                int centerTileX = (int)(npc.Center.X / TileSize);
                int feetTileY = (int)((npc.position.Y + npc.height) / TileSize);
                int groundTileY = FindGroundSurfaceTileYSmoothed(centerTileX, feetTileY - GroundSnapToleranceTiles * 2, GroundSnapToleranceTiles * 4);
                if (groundTileY >= 0)
                {
                    npc.position.Y = (groundTileY * TileSize) - npc.height;
                }
            }
        }

        //-- Attacks --

        //SnakeBite: small neck arch, white eye flash, then a Leonhard-style locked lunge (fairly dodgeable).
        const int BiteTelegraphTicks = 90;
        const int BiteLungeTicks = 22;
        const int BiteRecoverTicks = 60;
        const float BiteArchPixels = 50f;
        const float BiteLungeSpeed = 20f;

        //SnakePounce: forebody raises high into a true S, purple mouth dust, then a bigger lunge.
        const int PounceTelegraphTicks = 200;
        const int PounceLungeTicks = 26;
        const int PounceRecoverTicks = 90;
        const float PounceRaisePixels = 160f;
        const float PounceLungeSpeed = 22f;
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

        //GroundPierce: rear half burrows, the tail travels underground (still chained -- the rear body is
        //dragged along by a taut-link rope pass) to beneath the player, stabs up at a dust-telegraphed spot,
        //and travels back. The travel time is deliberately part of the telegraph/realism.
        const int PierceSinkTicks = 45;
        const int PierceAimTicks = 30;       //warning dusts fire at aim start; target locks then
        const int PierceStabTicks = 14;
        const int PierceRetractTicks = 14;
        const int PierceComboGapTicks = 90;  //minimum ticks between stabs (travel can take longer)
        const int PierceMaxCombo = 4;
        const int PierceCooldownTicks = 600;
        const int PierceTriggerRangeTiles = 25;
        const int PierceTriggerRoll = 90;
        const float PierceSinkDepth = 40f;    //how far below the surface the buried body rides
        const float PierceStabHeight = 80f;   //how far above the locked target the stab overshoots
        const int PierceTailDamage = 45;
        const float PierceTravelSpeed = 8f;   //px/tick the tail tunnels at
        const int PierceTravelTimeoutTicks = 360; //can't reach the spot in 6s -> give up and return
        const float PierceMinBuryPixels = 8f; //rear segments poking above surface+this get pushed back under

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
        static Vector2 MouthPosition(NPC npc) => npc.Center + new Vector2(npc.direction * 34f, 2f) * npc.scale;

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
                    npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
                    SpawnMouthVenomDust(npc);
                    if (data.AttackTimer <= 0)
                    {
                        data.Attack = OolacileSerpentHead.AttackState.PounceRecover;
                        data.AttackTimer = PounceRecoverTicks;
                        npc.damage = 60; //back to base bite-level contact damage
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

        //-- Tile sensing --

        static bool IsSolidTile(int tileX, int tileY)
        {
            if (tileX < 0 || tileY < 0 || tileX >= Main.maxTilesX || tileY >= Main.maxTilesY)
            {
                return true; //treat out-of-world as blocking rather than as an open gap
            }
            Tile tile = Main.tile[tileX, tileY];
            return tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
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
