using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    // Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
    public class Hydra : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Enemies/Hydra_Headless";

        private static ReLogic.Content.Asset<Texture2D> neckTexture;
        private static ReLogic.Content.Asset<Texture2D> headTexture;

        float npcAcSPD = 0.48f; // How fast they accelerate (20% slower).
        float npcSPD = 1.76f; // Max speed (20% slower).

        float npcEnrAcSPD = 0.72f; // How fast they accelerate, enraged.
        float npcEnrSPD = 4.0f; // Max speed, enraged.

        // ── Attack chooser state machine ───────────────────────────────────────
        private enum Phase
        {
            Chase,
            AimLock,
            SmiteMark,
            SmiteInterval,
            FireballCharge,
            BiteLunge,
        }

        private enum AttackID
        {
            ConsecratedLight,
            SmiteMark,
            FireballBarrage,
        }

        private static readonly AttackID[] AvailableAttacks = new AttackID[]
        {
            AttackID.ConsecratedLight,
            AttackID.SmiteMark,
            AttackID.FireballBarrage
        };

        const int AimLockTicks = 45;
        const int RecoveryTicks = 70;

        const int SmiteLockTick = 20;
        const int SmiteFireTick = 40;
        const int SmiteIntervalTicks = 15;
        const int MaxSmiteMarks = 5;
        const int SmiteDamage = 45;

        const int BiteTelegraphTicks = 90;
        const int BiteLungeTicks = 20;
        const int BiteHoldTicks = 60;   // Holds extended for 1 full second (60 ticks)
        const int BiteRetractTicks = 60;// Retracts slowly for 1 full second (60 ticks)
        const int BiteLungeDuration = BiteTelegraphTicks + BiteLungeTicks + BiteHoldTicks + BiteRetractTicks; // 230 ticks total
        const int BiteDamage = 60;

        Phase phase = Phase.Chase;
        int phaseTimer;
        AttackID currentAttack;
        int attackCooldown;
        int holyBeamCooldown;
        int lockedDirection = 1;

        int smiteMarksRemaining;
        int smiteMarkTimer;
        bool smiteMarkLocked;
        Vector2 smiteMarkPosition;

        // Independent middle head bite attack state
        private int biteLungeTimer = 0;
        private int biteCooldown = 120;
        private float biteLungeProgress = 0f;
        private Vector2 biteLockedLungeDir = Vector2.Zero;
        public Vector2 MiddleHeadWorldPosition = Vector2.Zero;
        public Vector2 FrontHeadWorldPosition = Vector2.Zero;
        public Vector2 BackHeadWorldPosition = Vector2.Zero;

        private enum MoveState { Pursue, Pause, BackwardPace }
        private MoveState moveState = MoveState.Pursue;
        private int moveStateTimer = 0;

        // Mouth transition progress per head: [0] = Back, [1] = Middle, [2] = Front
        private float[] mouthOpenProgress = new float[3];

        private bool wasInAir = false;

        public override void SetDefaults()
        {
            NPC.width = 170;
            NPC.height = 130;
            NPC.damage = 50;
            NPC.defense = 10;
            NPC.lifeMax = 2350;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 2400f;
            NPC.npcSlots = 100;
            NPC.scale = 1.1f;
            NPC.knockBackResist = 0.1f;
            Main.npcFrameCount[NPC.type] = 16;
            NPC.lavaImmune = true;
            NPC.buffImmune[BuffID.Venom] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.CursedInferno] = true;
            NPC.buffImmune[BuffID.OnFire] = true;

            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.NavSearchRadius = 24;
            g.MaxJumpPower = 6f; // Reduced jump power so it doesn't jump high
            g.MaxJumpBoost = 3f;
            g.BeastSinkMaxTiles = 2;
            EvasiveProfile.HeavyBeast(g);
            g.KiteRangeMin = 12f;
            g.KiteRangeMax = 30f;
            g.PatrolMode = NPCs.PatrolMode.Wander;

            attackCooldown = 90;
            biteCooldown = 120;
            holyBeamCooldown = 0;
            moveState = MoveState.Pursue;
            moveStateTimer = 0;
        }

        public override void FindFrame(int frameHeight)
        {
            if (NPC.velocity.Y == 0f && Math.Abs(NPC.velocity.X) > 0.1f)
            {
                // Walking animation frames cycle 20% slower (0.8 frameCounter step per tick)
                NPC.frameCounter += 0.8;
                if (NPC.frameCounter >= 3.0)
                {
                    NPC.frameCounter = 0;
                    int currentFrame = NPC.frame.Y / frameHeight;
                    int nextFrame = currentFrame + 1;
                    
                    // Seamless walking loop: frames 3 through 15 (skipping frame 0 idle and frame 2 air jump frame!)
                    if (nextFrame < 3 || nextFrame >= Main.npcFrameCount[NPC.type])
                    {
                        nextFrame = 3;
                    }
                    NPC.frame.Y = nextFrame * frameHeight;

                    // Footfall screen shake & footstep sound on ground impact frames (frames 3 and 11)
                    if (nextFrame == 3 || nextFrame == 11)
                    {
                        SoundEngine.PlaySound(SoundID.DeerclopsStep with { Volume = 0.4f, Pitch = -0.2f }, NPC.Bottom);
                        UsefulFunctions.ScreenShake(NPC.Bottom, 1.2f, 6, 4f, 300f);
                    }
                }
            }
            else if (NPC.velocity.Y != 0f)
            {
                NPC.frame.Y = 2 * frameHeight; // Jump/air frame
            }
            else
            {
                NPC.frame.Y = 0; // Idle standing frame
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo) { return 0f; }

        public float CanSpawnLegacy(NPCSpawnInfo s)
        {
            int x = s.SpawnTileX;
            int y = s.SpawnTileY;
            bool oSurface = (y >= (Main.maxTilesY * 0.1f) && y < (Main.maxTilesY * 0.2f));
            bool oUnderSurface = (y >= (Main.maxTilesY * 0.2f) && y < (Main.maxTilesY * 0.3f));
            bool oUnderground = (y >= (Main.maxTilesY * 0.3f) && y < (Main.maxTilesY * 0.4f));
            bool oCavern = (y >= (Main.maxTilesY * 0.4f) && y < (Main.maxTilesY * 0.6f));
            bool oUnderworld = (y >= (Main.maxTilesY * 0.8f));
            bool oBorders = (y < (Main.maxTilesY * 0.03f) || x < (Main.maxTilesX * 0.03f) || y > (Main.maxTilesY * 0.97f) || x > (Main.maxTilesX * 0.97f));
            Player p = s.Player;
            if ((p.townNPCs > 2f && !Main.bloodMoon) || Main.pumpkinMoon || Main.snowMoon || !p.ZoneJungle || oUnderworld || oBorders)
            {
                return 0f;
            }
            if (oSurface || oUnderSurface || oUnderground || oCavern)
            {
                if (Main.rand.Next(12000) == 1) return 1f;
                else if (Main.hardMode && Main.rand.Next(50) == 1) return 1f;
                else if ((oUnderground || oCavern) && Main.rand.Next(800) == 1) return 1f;
                else if (Main.hardMode && (oUnderground || oCavern) && Main.rand.Next(30) == 1) return 1f;
                else if (Main.bloodMoon && Main.rand.Next(120) == 1) return 1f;
                return 0f;
            }
            return 0f;
        }

        void TryStartAttack(Player player)
        {
            if (attackCooldown > 0 || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (NPC.velocity.Y != 0f)
            {
                return;
            }

            // Pick attack: enforce 4-second cooldown (240 ticks) on ConsecratedLight (holy beam)
            List<AttackID> validAttacks = new List<AttackID>();
            foreach (AttackID atk in AvailableAttacks)
            {
                if (atk == AttackID.ConsecratedLight && holyBeamCooldown > 0)
                {
                    continue;
                }
                validAttacks.Add(atk);
            }

            if (validAttacks.Count == 0)
            {
                return;
            }

            currentAttack = validAttacks[Main.rand.Next(validAttacks.Count)];
            NPC.netUpdate = true;

            switch (currentAttack)
            {
                case AttackID.ConsecratedLight:
                    lockedDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
                    phase = Phase.AimLock;
                    phaseTimer = 0;
                    holyBeamCooldown = 240; // 4 second cooldown on yellow beam attack!
                    break;
                case AttackID.SmiteMark:
                    smiteMarksRemaining = RollSmiteMarkCount();
                    BeginNextSmiteMark(player);
                    break;
                case AttackID.FireballBarrage:
                    lockedDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
                    phase = Phase.FireballCharge;
                    phaseTimer = 0;
                    break;
            }
        }

        void TickIndependentBite(Player player)
        {
            if (biteCooldown > 0)
            {
                biteCooldown--;
            }

            // Trigger bite attack independently if player is within 280px in front
            float distToPlayer = Vector2.Distance(player.Center, NPC.Center);
            bool playerInFront = (player.Center.X - NPC.Center.X) * NPC.direction > -40f;

            if (biteLungeTimer == 0 && biteCooldown == 0 && distToPlayer < 280f && playerInFront)
            {
                biteLungeTimer = 1;
                biteCooldown = 280; // Cooldown between bite attacks (~4.5s)
                NPC.netUpdate = true;
            }

            if (biteLungeTimer > 0)
            {
                biteLungeTimer++;

                if (biteLungeTimer <= BiteTelegraphTicks)
                {
                    // 1. Smooth 90-Tick Windup Telegraph (Ticks 1-90): rears back smoothly without snapping
                    float telegraphProgress = biteLungeTimer / (float)BiteTelegraphTicks;
                    float smoothEase = MathF.Sin(telegraphProgress * MathHelper.PiOver2);
                    biteLungeProgress = MathHelper.Lerp(0f, -0.25f, smoothEase);

                    Vector2 flashPos = MiddleHeadWorldPosition != Vector2.Zero ? MiddleHeadWorldPosition : NPC.Center;
                    if (biteLungeTimer == 1)
                    {
                        SoundEngine.PlaySound(SoundID.Zombie7 with { Volume = 0.75f, Pitch = -0.2f }, flashPos);
                    }
                    if (biteLungeTimer == BiteTelegraphTicks - 30)
                    {
                        // Standard white telegraph flash 30 ticks before lunge!
                        tsorcRevampAIs.SpawnTelegraphFlash(NPC, Color.White, flashPos);
                    }
                    if (biteLungeTimer == BiteTelegraphTicks)
                    {
                        // Lock lunge direction at end of 90t telegraph
                        biteLockedLungeDir = (player.Center - flashPos).SafeNormalize(NPC.direction == 1 ? Vector2.UnitX : -Vector2.UnitX);
                    }
                }
                else if (biteLungeTimer <= BiteTelegraphTicks + BiteLungeTicks)
                {
                    // 2. Fast Snappy Bite Lunge Phase (Ticks 91-110): thrusts 180px forward along locked lunge dir
                    float lungeProgress = (biteLungeTimer - BiteTelegraphTicks) / (float)BiteLungeTicks;
                    float smoothLungeEase = MathF.Sin(lungeProgress * MathHelper.PiOver2);
                    biteLungeProgress = MathHelper.Lerp(0.0f, 1.0f, smoothLungeEase);

                    if (biteLungeTimer == BiteTelegraphTicks + 10)
                    {
                        Vector2 bitePos = MiddleHeadWorldPosition != Vector2.Zero ? MiddleHeadWorldPosition : NPC.Center;
                        SoundEngine.PlaySound(SoundID.DD2_WyvernScream with { Volume = 0.85f, Pitch = -0.1f }, bitePos);
                        UsefulFunctions.ScreenShake(bitePos, 2.5f, 8, 6f, 400f);

                        // Check melee bite damage on player if within range of middle head
                        if (Vector2.Distance(player.Center, bitePos) < 130f && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int dir = player.Center.X >= NPC.Center.X ? 1 : -1;
                            player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(NPC.whoAmI), BiteDamage, dir);
                        }
                    }
                }
                else if (biteLungeTimer <= BiteTelegraphTicks + BiteLungeTicks + BiteHoldTicks)
                {
                    // 3. Extended Peak Hold (Ticks 51-110 = 1 full second): holds extended out biting at player
                    biteLungeProgress = 1.0f;
                }
                else if (biteLungeTimer <= BiteLungeDuration)
                {
                    // 4. Slow Organic Retraction (Ticks 111-170 = 1 full second): glides smoothly back to posture
                    float retractProgress = (biteLungeTimer - (BiteTelegraphTicks + BiteLungeTicks + BiteHoldTicks)) / (float)BiteRetractTicks;
                    // Cosine ease-out ensures a smooth, natural glide back without any sudden snap
                    float smoothRetractEase = 0.5f * (1f + MathF.Cos(retractProgress * MathHelper.Pi));
                    biteLungeProgress = MathHelper.Lerp(0.0f, 1.0f, smoothRetractEase);
                }
                else
                {
                    biteLungeTimer = 0;
                    biteLungeProgress = 0f;
                }
            }
        }

        void TickFireballCharge(Player player)
        {
            NPC.direction = lockedDirection;
            NPC.spriteDirection = lockedDirection;

            phaseTimer++;

            Vector2 mouthPos = BackHeadWorldPosition != Vector2.Zero ? BackHeadWorldPosition : NPC.Center;

            if (phaseTimer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.85f, Pitch = -0.1f }, mouthPos);
            }

            // 40-tick Torch Dust Converging Telegraph at Back Head Mouth
            if (phaseTimer <= 40)
            {
                float progress = phaseTimer / 40f;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 ringOffset = Main.rand.NextVector2CircularEdge(36f, 36f) * (1f - progress * 0.5f);
                    int d = Dust.NewDust(mouthPos + ringOffset, 4, 4, DustID.Torch, 0f, 0f, 100, default, 1.4f + progress * 0.4f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity = -ringOffset * 0.05f;
                }
            }

            if (phaseTimer == 40)
            {
                FireFireballBarrage(player, mouthPos);
                attackCooldown = RecoveryTicks;
                phase = Phase.Chase;
                phaseTimer = 0;
            }
        }

        void FireFireballBarrage(Player player, Vector2 spawnPos)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.9f, Pitch = -0.1f }, spawnPos);

            int projType = ModContent.ProjectileType<Projectiles.Enemy.Golem.SmallGolemFireball>();
            Vector2 toPlayerDir = (player.Center - spawnPos).SafeNormalize(Vector2.UnitX * NPC.direction);

            void SpawnFireball(Vector2 velocity, int damage)
            {
                Projectile p = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPos, velocity, projType, damage, 0f, Main.myPlayer);
                p.hostile = true;
                p.friendly = false;
                p.netUpdate = true;
            }

            // Roll 1 of 6 distinct firing patterns
            int pattern = Main.rand.Next(6);

            switch (pattern)
            {
                case 0: // Pattern 0: Direct Heavy Fast Fireball
                    SpawnFireball(toPlayerDir * 11f, 45);
                    break;

                case 1: // Pattern 1: 3-Shot Fan Spread (28 degree fan)
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 vel = toPlayerDir.RotatedBy(MathHelper.ToRadians(i * 14f)) * 9.5f;
                        SpawnFireball(vel, 35);
                    }
                    break;

                case 2: // Pattern 2: 5-Shot Arc Volley (48 degree wide arc)
                    for (int i = -2; i <= 2; i++)
                    {
                        Vector2 vel = toPlayerDir.RotatedBy(MathHelper.ToRadians(i * 12f)) * 8.5f;
                        SpawnFireball(vel, 30);
                    }
                    break;

                case 3: // Pattern 3: High Mortar Rain Arc
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 vel = new Vector2((player.Center.X - spawnPos.X) * 0.022f + i * 2.5f, -11f);
                        SpawnFireball(vel, 35);
                    }
                    break;

                case 4: // Pattern 4: 2-Shot Pincher Target (flanks left and right of player)
                    SpawnFireball(toPlayerDir.RotatedBy(MathHelper.ToRadians(-15f)) * 10f, 38);
                    SpawnFireball(toPlayerDir.RotatedBy(MathHelper.ToRadians(15f)) * 10f, 38);
                    break;

                case 5: // Pattern 5: V-Formation Crossfire (4 fireballs in V-shape)
                    float[] speeds = { 7.5f, 10.5f, 10.5f, 7.5f };
                    float[] angles = { -18f, -6f, 6f, 18f };
                    for (int i = 0; i < 4; i++)
                    {
                        SpawnFireball(toPlayerDir.RotatedBy(MathHelper.ToRadians(angles[i])) * speeds[i], 32);
                    }
                    break;
            }
        }

        void TickAimLock()
        {
            NPC.direction = lockedDirection;
            NPC.spriteDirection = lockedDirection;

            phaseTimer++;
            if (phaseTimer == 1)
            {
                tsorcRevampAIs.SpawnTelegraphFlash(NPC, new Color(255, 220, 90));
            }

            if (phaseTimer >= AimLockTicks)
            {
                FireCurrentAttack();
                attackCooldown = RecoveryTicks;
                phase = Phase.Chase;
                phaseTimer = 0;
            }
        }

        void FireCurrentAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            switch (currentAttack)
            {
                case AttackID.ConsecratedLight:
                    Vector2 spawnPos = FrontHeadWorldPosition != Vector2.Zero ? FrontHeadWorldPosition : NPC.Center;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, Vector2.UnitY, ModContent.ProjectileType<Projectiles.Enemy.EnemyConsecratedLight>(), 35, 0f, Main.myPlayer, 0f, NPC.whoAmI);
                    break;
            }
        }

        int RollSmiteMarkCount()
        {
            float lifeLostFraction = MathHelper.Clamp(1f - (NPC.life / (float)NPC.lifeMax), 0f, 1f);
            int count = 1;
            while (count < MaxSmiteMarks && Main.rand.NextFloat() < lifeLostFraction)
            {
                count++;
            }
            return count;
        }

        void BeginNextSmiteMark(Player player)
        {
            smiteMarkTimer = 0;
            smiteMarkLocked = false;
            smiteMarkPosition = player.Center;
            phase = Phase.SmiteMark;
            phaseTimer = 0;
            tsorcRevampAIs.SpawnTelegraphFlash(NPC, new Color(255, 220, 90));
        }

        void TickSmiteMark(Player player)
        {
            smiteMarkTimer++;

            if (!smiteMarkLocked)
            {
                smiteMarkPosition = player.Center;
                if (smiteMarkTimer >= SmiteFireTick - SmiteLockTick)
                {
                    smiteMarkLocked = true;
                    tsorcRevampAIs.SpawnTelegraphFlash(NPC, new Color(255, 220, 90), smiteMarkPosition);
                }
            }

            SpawnSmiteMarkDust(smiteMarkPosition, smiteMarkTimer / (float)SmiteFireTick);

            if (smiteMarkTimer >= SmiteFireTick)
            {
                FireConsecratedLightning(smiteMarkPosition);
                smiteMarksRemaining--;

                if (smiteMarksRemaining > 0)
                {
                    phase = Phase.SmiteInterval;
                    phaseTimer = 0;
                }
                else
                {
                    attackCooldown = RecoveryTicks;
                    phase = Phase.Chase;
                    phaseTimer = 0;
                }
            }
        }

        void TickSmiteInterval(Player player)
        {
            phaseTimer++;
            if (phaseTimer >= SmiteIntervalTicks)
            {
                BeginNextSmiteMark(player);
            }
        }

        void SpawnSmiteMarkDust(Vector2 position, float progress)
        {
            if (Main.netMode == NetmodeID.Server || Main.rand.NextFloat() >= 0.36f + progress * 0.6f)
            {
                return;
            }
            Vector2 edge = Main.rand.NextVector2CircularEdge(40f, 40f) * (1f - progress * 0.4f);
            int dust = Dust.NewDust(position + edge, 2, 2, DustID.GoldFlame, 0f, 0f, 100, default, 1.2f + progress * 0.2f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].velocity = -edge * 0.035f;

            if (Main.rand.NextBool(4))
            {
                int spark = Dust.NewDust(position + edge, 2, 2, DustID.WhiteTorch, 0f, 0f, 100, default, 1.1f);
                Main.dust[spark].noGravity = true;
            }
        }

        void FireConsecratedLightning(Vector2 position)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            // Shifted 32px lower (204f Y offset instead of 236f) so the bottom edge of lightning animation hits the ground
            Vector2 spawnPosition = position - new Vector2(0f, 204f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPosition, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.ConsecratedLightning>(), SmiteDamage, 0f, Main.myPlayer);
        }

        public override void AI()
        {
            NPC.TargetClosest(true);
            Player player = Main.player[NPC.target];

            float distToPlayer = Vector2.Distance(player.Center, NPC.Center);
            bool enraged = (NPC.life < (float)NPC.lifeMax * .2f);
            float accel = enraged ? npcEnrAcSPD : npcAcSPD;
            float topSpeed = enraged ? npcEnrSPD : npcSPD;

            // Movement state machine logic
            moveStateTimer++;

            if (moveState == MoveState.Pursue)
            {
                // Walk toward player using FighterAI
                tsorcRevampAIs.FighterAI(NPC, topSpeed: topSpeed, acceleration: accel, canTeleport: true, doorBreakingDamage: 2, minSurfaceWidth: 4, canWalkBackwards: false, canPounce: false);

                // If within 250px range of player, periodically choose to Pause (3-5s) or BackwardPace (3s)
                if (distToPlayer < 250f && moveStateTimer > 120 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    moveStateTimer = 0;
                    if (Main.rand.NextBool(3)) // 33% chance to back up 3x longer, 67% chance to pause/stand ground
                    {
                        moveState = MoveState.BackwardPace;
                    }
                    else
                    {
                        moveState = MoveState.Pause;
                    }
                    NPC.netUpdate = true;
                }
            }
            else if (moveState == MoveState.Pause)
            {
                // Stop moving on ground (pause for 3 to 5 seconds = 180 to 300 ticks)
                NPC.velocity.X *= 0.82f;
                if (Math.Abs(NPC.velocity.X) < 0.1f) NPC.velocity.X = 0f;

                // Resume pursuit if player retreats past 350px, or if 4 seconds elapse
                if (distToPlayer > 350f || moveStateTimer >= 240)
                {
                    moveState = MoveState.Pursue;
                    moveStateTimer = 0;
                    NPC.netUpdate = true;
                }
            }
            else if (moveState == MoveState.BackwardPace)
            {
                // Walk backwards for 3 seconds (180 ticks = 3x longer than standard pacing!)
                int backDir = player.Center.X >= NPC.Center.X ? -1 : 1;
                NPC.velocity.X = backDir * (topSpeed * 0.45f);
                NPC.direction = player.Center.X >= NPC.Center.X ? 1 : -1;
                NPC.spriteDirection = NPC.direction;

                // If blocked by a wall, 180 ticks elapse, or player retreats far away, return to pursue
                if (NPC.collideX || moveStateTimer >= 180 || distToPlayer > 380f)
                {
                    moveState = MoveState.Pursue;
                    moveStateTimer = 0;
                    NPC.netUpdate = true;
                }
            }

            // Jump landing detection & screen shake
            bool currentlyInAir = NPC.velocity.Y != 0f;
            if (wasInAir && !currentlyInAir)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = -0.4f }, NPC.Bottom);
                UsefulFunctions.ScreenShake(NPC.Bottom, 3.0f, 10, 8f, 500f);
            }
            wasInAir = currentlyInAir;

            if (attackCooldown > 0)
            {
                attackCooldown--;
            }

            if (holyBeamCooldown > 0)
            {
                holyBeamCooldown--;
            }

            switch (phase)
            {
                case Phase.Chase:
                    TryStartAttack(player);
                    break;
                case Phase.AimLock:
                    TickAimLock();
                    break;
                case Phase.SmiteMark:
                    TickSmiteMark(player);
                    break;
                case Phase.SmiteInterval:
                    TickSmiteInterval(player);
                    break;
                case Phase.FireballCharge:
                    TickFireballCharge(player);
                    break;
            }

            // Independent middle head bite attack tick
            TickIndependentBite(player);

            // Mouth open progress independently calculated per head:
            // Head 0 (Back Head / Fireball Barrage):
            float backMouth = (phase == Phase.FireballCharge) ? 1.0f : 0.0f;
            mouthOpenProgress[0] = MathHelper.Lerp(mouthOpenProgress[0], backMouth, 0.25f);

            // Head 1 (Middle Head / Independent Melee Bite):
            float middleMouth = (biteLungeTimer > 0) ? 1.0f : 0.0f;
            mouthOpenProgress[1] = MathHelper.Lerp(mouthOpenProgress[1], middleMouth, 0.25f);

            // Head 2 (Front Head / Holy Beam & Lightning):
            float frontMouth = (phase == Phase.AimLock || phase == Phase.SmiteMark || phase == Phase.SmiteInterval) ? 1.0f : 0.0f;
            mouthOpenProgress[2] = MathHelper.Lerp(mouthOpenProgress[2], frontMouth, 0.25f);
        }

        public override void OnKill()
        {
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore1").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore2").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore3").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore2").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore3").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore1").Type, 1.1f);
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("HydraGore1").Type, 1.1f);
        }

        // ── Drawing Engine ─────────────────────────────────────────────────────────────
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            neckTexture ??= ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/Hydra_Neck");
            headTexture ??= ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/Hydra_Head");

            // Base neck anchor offsets on body (relative to NPC.Bottom facing left)
            // Relative coordinates: -X is Front/Chest side (left when facing left), +X is Back/Tail side.
            // Neck 0 (Purple / Back): offset (-15, -72) — moved 20px DOWN
            // Neck 1 (Red / Middle): offset (-9, -73) — moved 20px DOWN
            // Neck 2 (Orange / Front): offset (-2, -92) — 10px higher
            Vector2[] neckBaseOffsetsLeft = new Vector2[]
            {
                new Vector2(-15f, -72f),
                new Vector2(-9f, -73f),
                new Vector2(-2f, -87f)
            };

            // 1. Draw Purple Neck & Head (Back layer)
            DrawNeckAndHead(spriteBatch, screenPos, drawColor, neckIndex: 0, neckBaseOffsetsLeft[0]);

            // 2. Draw Red Neck & Head (Middle layer)
            DrawNeckAndHead(spriteBatch, screenPos, drawColor, neckIndex: 1, neckBaseOffsetsLeft[1]);

            // 3. Draw Hydra_Headless Body (Center layer, aligned to NPC.Bottom, shifted 4px down into ground)
            DrawBody(spriteBatch, screenPos, drawColor);

            // 4. Draw Orange Neck & Head (Front layer)
            DrawNeckAndHead(spriteBatch, screenPos, drawColor, neckIndex: 2, neckBaseOffsetsLeft[2]);

            return false;
        }

        private void DrawBody(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Texture2D bodyTex = ModContent.Request<Texture2D>(Texture).Value;
            int frameHeight = bodyTex.Height / Main.npcFrameCount[NPC.type];
            Rectangle sourceRect = new Rectangle(0, NPC.frame.Y, bodyTex.Width, frameHeight);
            
            // Align feet to NPC.Bottom (Y = 177 in 180px frame height)
            Vector2 origin = new Vector2(bodyTex.Width / 2f, 177f);

            SpriteEffects effects = NPC.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            // Shifted 4px down into ground as requested
            Vector2 drawPos = NPC.Bottom - screenPos + new Vector2(0f, NPC.gfxOffY + 4f);

            spriteBatch.Draw(bodyTex, drawPos, sourceRect, drawColor, NPC.rotation, origin, NPC.scale, effects, 0f);
        }

        private void DrawNeckAndHead(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor, int neckIndex, Vector2 baseOffsetLeft)
        {
            if (neckTexture == null || headTexture == null) return;

            Texture2D neckTex = neckTexture.Value;
            Texture2D headTex = headTexture.Value;

            bool facingRight = NPC.spriteDirection == 1;
            Vector2 baseOffset = facingRight ? new Vector2(-baseOffsetLeft.X, baseOffsetLeft.Y) : baseOffsetLeft;

            Vector2 neckBasePos = NPC.Bottom + baseOffset + new Vector2(0f, NPC.gfxOffY + 4f);
            float time = (float)Main.timeForVisualEffects;

            // Out-of-sync idle sway & breathing parameters per neck
            float[] swayFreq = { 0.038f, 0.048f, 0.042f };
            float[] swayPhase = { 0.0f, 2.3f, 4.7f };
            float swayAngle = MathF.Sin(time * swayFreq[neckIndex] + swayPhase[neckIndex]) * 0.08f;

            // Initial direction extending rightward (+X) if facing left, or leftward (-X) if facing right
            float baseAngle = facingRight ? MathHelper.Pi : 0f;

            Player targetPlayer = Main.player[NPC.target];

            // Segment counts: Neck 0 = 26, Neck 1 = 42 (Middle neck ALWAYS has 42 segments for 200px+ reach!), Neck 2 = 32
            int segmentCount = neckIndex == 2 ? 32 : (neckIndex == 1 ? 42 : 26);

            // 4.8px step guarantees contiguous overlapping neck segments with ZERO visual gaps
            const float segmentLength = 4.8f;

            Vector2[] segmentPoints = new Vector2[segmentCount + 1];
            segmentPoints[0] = neckBasePos;

            // Standard Base Bend Angle per neck
            float baseBendDegrees = (neckIndex == 1) ? 190f : 175f;

            // Dynamic C-shape breathing flex (varies curve amplitude between 178deg and 202deg during idle)
            float breathingFlex = MathF.Sin(time * 0.025f + neckIndex * 1.5f) * 12f;
            baseBendDegrees += breathingFlex;

            if (neckIndex == 1 && biteLungeProgress != 0f)
            {
                // Smooth, continuous curve modulation during bite attack (no line interpolation, zero stretching, zero snapping!)
                if (biteLungeProgress < 0f)
                {
                    // Windup: rears back smoothly to 150deg
                    float ease = MathHelper.Clamp(-biteLungeProgress / 0.25f, 0f, 1f);
                    baseBendDegrees = MathHelper.Lerp(190f, 150f, ease);
                }
                else
                {
                    // Lunge / Peak Hold / Slow Retraction: sweeps forward up to 240deg
                    baseBendDegrees = MathHelper.Lerp(190f, 240f, biteLungeProgress);
                }
            }

            float totalTargetBend = facingRight ? MathHelper.ToRadians(baseBendDegrees) : -MathHelper.ToRadians(baseBendDegrees);
            float baseBendPerStep = totalTargetBend / segmentCount;
            float currentAngle = baseAngle + swayAngle;

            Vector2 currPos = neckBasePos;

            for (int i = 0; i < segmentCount; i++)
            {
                float stepProgress = (float)i / segmentCount;

                // Serpentine snake-like wave that undulates along the neck length
                float snakeWave = MathF.Sin(stepProgress * MathHelper.Pi * 2.4f - time * 0.05f + neckIndex * 2.2f) * 0.024f;
                if (facingRight) snakeWave = -snakeWave;

                // Organic mid-point bend
                float midFlex = MathF.Sin(stepProgress * MathHelper.Pi);
                float dynamicMidBend = midFlex * (0.014f + MathF.Sin(time * 0.035f + neckIndex * 1.8f) * 0.008f);
                if (facingRight) dynamicMidBend = -dynamicMidBend;

                currentAngle += baseBendPerStep + snakeWave + dynamicMidBend;
                segmentPoints[i + 1] = currPos;
                currPos += currentAngle.ToRotationVector2() * segmentLength;
            }

            // Apply forward lunge displacement during bite attack to reach 180+ pixels out without stretching
            if (neckIndex == 1 && biteLungeProgress > 0f)
            {
                float lungeDisplacement = MathHelper.Lerp(0f, 140f, MathHelper.Clamp(biteLungeProgress, 0f, 1f));
                Vector2 lungeVec = (biteLockedLungeDir != Vector2.Zero ? biteLockedLungeDir : (facingRight ? Vector2.UnitX : -Vector2.UnitX));
                for (int i = 1; i <= segmentCount; i++)
                {
                    float stepProgress = (float)i / segmentCount;
                    segmentPoints[i] += lungeVec * (lungeDisplacement * MathF.Sin(stepProgress * MathHelper.PiOver2));
                }
            }

            // Draw neck segments along segment points
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 drawPos = segmentPoints[i] - screenPos;
                Vector2 nextPos = segmentPoints[i + 1];
                float neckRotation = (nextPos - segmentPoints[i]).ToRotation();
                Vector2 neckOrigin = new Vector2(neckTex.Width / 2f, neckTex.Height / 2f);

                spriteBatch.Draw(neckTex, drawPos, null, drawColor, neckRotation, neckOrigin, NPC.scale, SpriteEffects.None, 0f);
            }

            Vector2 lastPos = segmentPoints[segmentCount];
            float endNeckAngle = (segmentPoints[segmentCount] - segmentPoints[segmentCount - 1]).ToRotation();

            // Record Back Head (Neck 0), Middle Head (Neck 1), and Front Head (Neck 2) positions
            if (neckIndex == 0)
            {
                BackHeadWorldPosition = lastPos;
            }
            else if (neckIndex == 1)
            {
                MiddleHeadWorldPosition = lastPos;
            }
            else if (neckIndex == 2)
            {
                FrontHeadWorldPosition = lastPos;
            }

            // Head frame selection per head: 0 = closed, 1 = half open, 2 = wide open
            int headFrame;
            if (mouthOpenProgress[neckIndex] < 0.25f)
                headFrame = 0;
            else if (mouthOpenProgress[neckIndex] < 0.75f)
                headFrame = 1;
            else
                headFrame = 2;

            int headFrameHeight = headTex.Height / 3;
            Rectangle headSourceRect = new Rectangle(0, headFrame * headFrameHeight, headTex.Width, headFrameHeight);

            // Calculate head targeting angle: during bite attack, head locks to neck vector without tracking player movement!
            float targetHeadAngle;
            if (neckIndex == 1 && biteLungeTimer > BiteTelegraphTicks)
            {
                targetHeadAngle = endNeckAngle;
            }
            else
            {
                Vector2 toPlayer = (targetPlayer?.active == true ? targetPlayer.Center : lastPos + endNeckAngle.ToRotationVector2()) - lastPos;
                float angleToPlayer = toPlayer.ToRotation();

                float angleDiff = MathHelper.WrapAngle(angleToPlayer - endNeckAngle);
                angleDiff = MathHelper.Clamp(angleDiff, -MathHelper.ToRadians(70f), MathHelper.ToRadians(70f));
                targetHeadAngle = endNeckAngle + angleDiff;
            }

            SpriteEffects headEffects = facingRight ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            // Shift headOrigin.X back to 13f (from 28f) so back skull socket swallows neck end with ZERO floating gap!
            Vector2 headOrigin = new Vector2(13f, headFrameHeight / 2f);
            
            // Head sprite rotation (authored facing left)
            float headRotation = facingRight ? targetHeadAngle : targetHeadAngle - MathHelper.Pi;

            Vector2 headDrawPos = lastPos - screenPos;

            spriteBatch.Draw(headTex, headDrawPos, headSourceRect, drawColor, headRotation, headOrigin, NPC.scale, headEffects, 0f);
        }
    }
}
