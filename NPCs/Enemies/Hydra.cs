using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Materials;

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
            HydraScream,
            LungeBite
        }

        private static readonly AttackID[] AvailableAttacks = new AttackID[]
        {
            AttackID.ConsecratedLight,
            AttackID.SmiteMark,
            AttackID.FireballBarrage,
            AttackID.HydraScream,
            AttackID.LungeBite
        };

        const int AimLockTicks = 45;
        const int RecoveryTicks = 70;

        const int SmiteLockTick = 20;
        const int SmiteFireTick = 40;
        const int SmiteIntervalTicks = 15;
        const int MaxSmiteMarks = 5;
        const int SmiteDamage = 45;

        // HydraScream Attack (Witchking purple vacuum pull + shockwave blast)
        const int ScreamTelegraphTicks = 90;
        const int ScreamReleaseTicks = 40;
        const int ScreamHoldTicks = 45;
        const int ScreamRetractTicks = 45;
        const int ScreamDuration = ScreamTelegraphTicks + ScreamReleaseTicks + ScreamHoldTicks + ScreamRetractTicks; // 220 ticks total
        const int ScreamDamage = 65;

        // LungeBite Attack (3x range physical snapping bite, no scream)
        const int LungeBiteTelegraphTicks = 40;
        const int LungeBiteAttackTicks = 20;
        const int LungeBiteHoldTicks = 45;
        const int LungeBiteRetractTicks = 50;
        const int LungeBiteDuration = LungeBiteTelegraphTicks + LungeBiteAttackTicks + LungeBiteHoldTicks + LungeBiteRetractTicks; // 155 ticks total
        const int LungeBiteDamage = 75;

        Phase phase = Phase.Chase;
        int phaseTimer;
        AttackID currentAttack;
        int attackCooldown;
        int holyBeamCooldown;
        int holyBeamActiveTimer;
        int lockedDirection = 1;

        int smiteMarksRemaining;
        int smiteMarkTimer;
        bool smiteMarkLocked;
        Vector2 smiteMarkPosition;

        // Independent middle head attack state
        private int middleAttackTimer = 0;
        private int middleAttackCooldown = 120;
        private AttackID currentMiddleAttack = AttackID.HydraScream;
        private float middleAttackProgress = 0f;
        private Vector2 middleLockedDir = Vector2.Zero;
        private int screamState = 0;
        private int screamSubTimer = 0;

        public Vector2 MiddleHeadWorldPosition = Vector2.Zero;
        public Vector2 FrontHeadWorldPosition = Vector2.Zero;
        public Vector2 BackHeadWorldPosition = Vector2.Zero;

        // MagicShield state & motion blur trail history (18 positions = 3x cache trails!)
        private bool magicShieldActive = false;
        private Vector2[] oldBodyPos = new Vector2[18];

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
            NPC.defense = 30;
            NPC.lifeMax = 100000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 20000f;
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
            g.MaxJumpPower = 0f; // No jumping for navigation/evasion
            g.MaxJumpBoost = 0f;
            g.BeastSinkMaxTiles = 2;
            g.KiteRangeMin = 12f;
            g.KiteRangeMax = 30f;
            g.PatrolMode = NPCs.PatrolMode.Wander;

            attackCooldown = 90;
            middleAttackCooldown = 120;
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
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
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
            if (middleAttackCooldown > 0)
            {
                middleAttackCooldown--;
            }

            float distToPlayer = Vector2.Distance(player.Center, NPC.Center);
            bool playerInFront = (player.Center.X - NPC.Center.X) * NPC.direction > -40f;

            // Trigger middle head attack when off cooldown (HydraScream at close range, LungeBite up to 550px long range)
            if (middleAttackTimer == 0 && middleAttackCooldown == 0 && playerInFront && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (distToPlayer < 550f)
                {
                    middleAttackTimer = 1;
                    middleAttackCooldown = 260; // ~4.3s cooldown

                    if (distToPlayer < 250f)
                    {
                        // In close range (< 250px): 50% HydraScream, 50% LungeBite
                        currentMiddleAttack = Main.rand.NextBool(2) ? AttackID.HydraScream : AttackID.LungeBite;
                    }
                    else
                    {
                        // In long range (250px..550px): 100% LungeBite (3x range physical bite!)
                        currentMiddleAttack = AttackID.LungeBite;
                    }
                    NPC.netUpdate = true;
                }
            }

            if (middleAttackTimer > 0)
            {
                middleAttackTimer++;

                Vector2 headPos = MiddleHeadWorldPosition != Vector2.Zero ? MiddleHeadWorldPosition : NPC.Center;

                if (currentMiddleAttack == AttackID.HydraScream)
                {
                    TickHydraScream(player, headPos);
                }
                else if (currentMiddleAttack == AttackID.LungeBite)
                {
                    TickLungeBite(player, headPos);
                }
            }
        }

        void TickHydraScream(Player player, Vector2 headPos)
        {
            // screamState: 0 = Telegraph (60t), 1 = Vacuum Pull (max 300t or dist <= 150px), 2 = Dodge Window (30t), 3 = Scream Blast (1t), 4 = Hold & Retract (90t)
            if (screamState == 0)
            {
                // 1. 60-Tick Windup Telegraph: rear back + mouth purple dust burst
                screamSubTimer++;
                float progress = screamSubTimer / 60f;
                middleAttackProgress = MathHelper.Lerp(0f, -0.25f, MathF.Sin(progress * MathHelper.PiOver2));

                if (screamSubTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Zombie7 with { Volume = 0.75f, Pitch = -0.2f }, headPos);
                }

                // Burst of purple dusts at middle head mouth
                if (Main.rand.NextBool(2))
                {
                    int d1 = Dust.NewDust(headPos - new Vector2(16f, 16f), 32, 32, DustID.ShadowbeamStaff, 0f, 0f, 100, default, 1.4f);
                    Main.dust[d1].noGravity = true;
                    int d2 = Dust.NewDust(headPos - new Vector2(16f, 16f), 32, 32, DustID.Shadowflame, 0f, 0f, 100, default, 1.2f);
                    Main.dust[d2].noGravity = true;
                }

                if (screamSubTimer >= 60)
                {
                    screamState = 1;
                    screamSubTimer = 0;
                }
            }
            else if (screamState == 1)
            {
                // 2. Vacuum Pull Phase (Part 1 of Attack): pulls player toward middle head until dist <= 150px or 300t (5s)
                screamSubTimer++;
                middleAttackProgress = -0.25f; // Hold reared back position

                // Sustained horizontal pull toward Hydra's body (exact Witchking pull physics!)
                float xDiff = player.Center.X - NPC.Center.X;
                float yDiff = player.Center.Y - NPC.Center.Y;

                float xSign = xDiff > 0 ? 1 : -1;
                float ySign = yDiff > 0 ? 1 : -1;

                float strength = 3.0f;

                // Pull gets stronger as player is farther away
                player.velocity.X -= (xDiff / 3000f + strength * 0.050f * xSign) * (1f + strength * 0.115f);

                // Only apply Y pull if player is currently midair - prevents ground collision flick bug!
                if (player.velocity.Y != 0f)
                {
                    player.velocity.Y -= (yDiff / 800f + strength * 0.03f * ySign);
                }

                // Visual pull effects: WitchkingsGrasp debuff + purple dust streams from player to head
                player.AddBuff(ModContent.BuffType<Buffs.Debuffs.WitchkingsGrasp>(), 5, false);
                for (int k = 0; k < 2; k++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(16f, 16f);
                    Vector2 dustPos = player.position + offset;
                    Vector2 vel = (headPos - dustPos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(8f, 15f);
                    Dust.NewDustPerfect(dustPos + vel, DustID.ShadowbeamStaff, vel, Scale: Main.rand.NextFloat(0.9f, 1.4f)).noGravity = true;
                    Dust.NewDustPerfect(dustPos + vel, DustID.Shadowflame, vel * 0.8f, Scale: Main.rand.NextFloat(0.8f, 1.2f)).noGravity = true;
                }

                // Check pull completion: reached 150px area OR 5 seconds elapsed!
                float distToHead = Vector2.Distance(player.Center, headPos);
                if (distToHead <= 150f || screamSubTimer >= 300)
                {
                    screamState = 2; // Transition to 30-tick reaction window!
                    screamSubTimer = 0;
                    tsorcRevampAIs.SpawnTelegraphFlash(NPC, Color.White, headPos); // White telegraph flash at pull end!
                    middleLockedDir = (player.Center - headPos).SafeNormalize(NPC.direction == 1 ? Vector2.UnitX : -Vector2.UnitX);
                }
            }
            else if (screamState == 2)
            {
                // 3. 30-Tick Reaction Window: pull stops, white flash shown, gives player window to dodge roll out of 250px radius!
                screamSubTimer++;
                middleAttackProgress = MathHelper.Lerp(-0.25f, 0.0f, screamSubTimer / 30f);

                if (screamSubTimer >= 30)
                {
                    screamState = 3;
                    screamSubTimer = 0;
                }
            }
            else if (screamState == 3)
            {
                // 4. Scream Blast Release (Part 2 of Attack): Wyvern/wraith scream + 250px ExplosionFlash & ShockwaveEffect VFX + 250px damage hitbox!
                screamSubTimer++;
                middleAttackProgress = 1.0f;

                if (screamSubTimer == 1)
                {
                    // Existing Wyvern/wraith scream sound
                    SoundEngine.PlaySound(SoundID.DD2_WyvernScream with { Volume = 0.85f, Pitch = -0.1f }, headPos);
                    UsefulFunctions.ScreenShake(headPos, 6.0f, 15, 12f, 600f);

                    // Original 250px ExplosionFlash & ShockwaveEffect VFX rings!
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), headPos, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 550, 20);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), headPos, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 520, 60);
                    }

                    // Burst of purple and red dust
                    for (int i = 0; i < 30; i++)
                    {
                        Vector2 dustSpeed = Main.rand.NextVector2Circular(20f, 20f);
                        int d1 = Dust.NewDust(headPos, 0, 0, DustID.ShadowbeamStaff, dustSpeed.X, dustSpeed.Y, 0, default, 1.8f);
                        Main.dust[d1].noGravity = true;
                        int d2 = Dust.NewDust(headPos, 0, 0, DustID.Firework_Red, dustSpeed.X, dustSpeed.Y, 0, default, 1.8f);
                        Main.dust[d2].noGravity = true;
                    }

                    // 250px damage hitbox matching 250px VFX rings!
                    if (Vector2.Distance(player.Center, headPos) < 250f && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int dir = player.Center.X >= NPC.Center.X ? 1 : -1;
                        player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(NPC.whoAmI), ScreamDamage, dir);
                    }
                }

                screamState = 4;
                screamSubTimer = 0;
            }
            else if (screamState == 4)
            {
                // 5. Peak Hold (45t) & Retraction (45t)
                screamSubTimer++;

                if (screamSubTimer <= 45)
                {
                    middleAttackProgress = 1.0f;
                }
                else if (screamSubTimer <= 90)
                {
                    float retractProg = (screamSubTimer - 45) / 45f;
                    float ease = 0.5f * (1f + MathF.Cos(retractProg * MathHelper.Pi));
                    middleAttackProgress = MathHelper.Lerp(0.0f, 1.0f, ease);
                }
                else
                {
                    middleAttackTimer = 0;
                    middleAttackProgress = 0f;
                    screamState = 0;
                    screamSubTimer = 0;
                }
            }
        }

        void TickLungeBite(Player player, Vector2 headPos)
        {
            if (middleAttackTimer <= LungeBiteTelegraphTicks)
            {
                // 1. 40-Tick Windup Telegraph Phase
                float progress = middleAttackTimer / (float)LungeBiteTelegraphTicks;
                float smoothEase = MathF.Sin(progress * MathHelper.PiOver2);
                middleAttackProgress = MathHelper.Lerp(0f, -0.25f, smoothEase);

                if (middleAttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Zombie7 with { Volume = 0.8f, Pitch = -0.1f }, headPos);
                }
                else if (middleAttackTimer == 10)
                {
                    // Standard white telegraph flash 30 ticks before lunge!
                    tsorcRevampAIs.SpawnTelegraphFlash(NPC, Color.White, headPos);
                }

                if (middleAttackTimer == LungeBiteTelegraphTicks)
                {
                    // Lock lunge direction at end of 40t telegraph
                    middleLockedDir = (player.Center - headPos).SafeNormalize(NPC.direction == 1 ? Vector2.UnitX : -Vector2.UnitX);
                }
            }
            else if (middleAttackTimer <= LungeBiteTelegraphTicks + LungeBiteAttackTicks)
            {
                // 2. 3x Range Physical Snapping Bite (Ticks 41-60): thrusts 340px out along locked direction
                float lungeProg = (middleAttackTimer - LungeBiteTelegraphTicks) / (float)LungeBiteAttackTicks;
                middleAttackProgress = MathHelper.Lerp(0.0f, 1.0f, MathF.Sin(lungeProg * MathHelper.PiOver2));

                if (middleAttackTimer == LungeBiteTelegraphTicks + 5)
                {
                    // Physical heavy bite sound (no scream)
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = 0.2f }, headPos);
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.3f }, headPos);
                    UsefulFunctions.ScreenShake(headPos, 3.5f, 10, 8f, 500f);

                    // Physical bite damage (75 damage) on player if within range of extended head
                    if (Vector2.Distance(player.Center, headPos) < 180f && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int dir = player.Center.X >= NPC.Center.X ? 1 : -1;
                        player.Hurt(Terraria.DataStructures.PlayerDeathReason.ByNPC(NPC.whoAmI), LungeBiteDamage, dir);
                    }
                }
            }
            else if (middleAttackTimer <= LungeBiteTelegraphTicks + LungeBiteAttackTicks + LungeBiteHoldTicks)
            {
                // 3. Extended Peak Hold (Ticks 61-105): holds extended 340px out in front
                middleAttackProgress = 1.0f;
            }
            else if (middleAttackTimer <= LungeBiteDuration)
            {
                // 4. Slow Cosine Retraction (Ticks 106-155)
                float retractProg = (middleAttackTimer - (LungeBiteTelegraphTicks + LungeBiteAttackTicks + LungeBiteHoldTicks)) / (float)LungeBiteRetractTicks;
                float ease = 0.5f * (1f + MathF.Cos(retractProg * MathHelper.Pi));
                middleAttackProgress = MathHelper.Lerp(0.0f, 1.0f, ease);
            }
            else
            {
                middleAttackTimer = 0;
                middleAttackProgress = 0f;
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
                    holyBeamActiveTimer = 135;
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
            if (Main.netMode == NetmodeID.Server || Main.rand.NextFloat() >= 0.54f + progress * 0.46f)
            {
                return;
            }

            int spawnCount = Main.rand.NextBool() ? 2 : 1;
            for (int i = 0; i < spawnCount; i++)
            {
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

            if (holyBeamActiveTimer > 0)
            {
                holyBeamActiveTimer--;
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

            // Head 1 (Middle Head / HydraScream & LungeBite):
            float middleMouth = (middleAttackTimer > 0) ? 1.0f : 0.0f;
            mouthOpenProgress[1] = MathHelper.Lerp(mouthOpenProgress[1], middleMouth, 0.25f);

            // Head 2 (Front Head / Holy Beam & Lightning):
            float frontMouth = (phase == Phase.AimLock || phase == Phase.SmiteMark || phase == Phase.SmiteInterval || holyBeamActiveTimer > 0) ? 1.0f : 0.0f;
            mouthOpenProgress[2] = MathHelper.Lerp(mouthOpenProgress[2], frontMouth, 0.25f);

            // Always face target player while engaged, keep body upright with zero rotation
            NPC.rotation = 0f;
            if (player?.active == true && !player.dead)
            {
                NPC.direction = player.Center.X >= NPC.Center.X ? 1 : -1;
                NPC.spriteDirection = NPC.direction;
            }

            // Update MagicShield ability when player is > 500px away
            UpdateMagicShield(player);
        }

        void UpdateMagicShield(Player player)
        {
            if (player?.active != true || player.dead) return;

            float distToPlayer = Vector2.Distance(player.Center, NPC.Center);
            bool shouldShield = distToPlayer > 500f;

            if (shouldShield)
            {
                if (!magicShieldActive)
                {
                    magicShieldActive = true;
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = 0.2f }, NPC.Center);
                }

                NPC.defense = 200; // 200 defense while MagicShield is active!

                // Update oldBodyPos trail history
                for (int k = oldBodyPos.Length - 1; k > 0; k--)
                {
                    oldBodyPos[k] = oldBodyPos[k - 1];
                }
                oldBodyPos[0] = NPC.position;

                // Blue aura particle effects around body and heads
                if (Main.rand.NextBool(2))
                {
                    Vector2 auraPos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                    int d = Dust.NewDust(auraPos, 0, 0, DustID.MagicMirror, 0f, -1f, 100, default, 1.2f);
                    Main.dust[d].noGravity = true;
                }

                // Reflect all player projectiles off Hydra body & heads!
                ReflectProjectiles(player);
            }
            else
            {
                if (magicShieldActive)
                {
                    magicShieldActive = false;
                    NPC.defense = 30; // Reset to default 30 defense
                }
            }
        }

        void ReflectProjectiles(Player player)
        {
            Rectangle bodyHitbox = NPC.Hitbox;
            Rectangle backHeadHitbox = new Rectangle((int)BackHeadWorldPosition.X - 35, (int)BackHeadWorldPosition.Y - 35, 70, 70);
            Rectangle middleHeadHitbox = new Rectangle((int)MiddleHeadWorldPosition.X - 40, (int)MiddleHeadWorldPosition.Y - 40, 80, 80);
            Rectangle frontHeadHitbox = new Rectangle((int)FrontHeadWorldPosition.X - 35, (int)FrontHeadWorldPosition.Y - 35, 70, 70);

            // Calculate 20% of player max HP for reflected projectile damage!
            int reflectedDamage = (int)(player.statLifeMax2 * 0.20f);
            if (reflectedDamage < 40) reflectedDamage = 40;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.friendly && !proj.hostile && proj.damage > 0)
                {
                    Rectangle projHitbox = proj.Hitbox;
                    if (projHitbox.Intersects(bodyHitbox) || projHitbox.Intersects(backHeadHitbox) || projHitbox.Intersects(middleHeadHitbox) || projHitbox.Intersects(frontHeadHitbox))
                    {
                        // Reflect projectile: turn hostile, set damage to 20% of player max HP, point straight back at player!
                        proj.friendly = false;
                        proj.hostile = true;
                        proj.damage = reflectedDamage;

                        float currentSpeed = Math.Max(proj.velocity.Length(), 14f);
                        proj.velocity = (player.Center - proj.Center).SafeNormalize(Vector2.Zero) * currentSpeed;

                        // Play reflection sound & spawn blue magic mirror dust ring
                        SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = 0.5f }, proj.Center);
                        UsefulFunctions.DustRing(proj.Center, 20, DustID.MagicMirror, 8, 1.5f);
                    }
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoul>(), 1, 20000, 20000));
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

            // Massive burst of 500 blood dusts on kill across entire Hydra body and heads
            for (int i = 0; i < 500; i++)
            {
                Vector2 spawnPos = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                Vector2 vel = Main.rand.NextVector2Circular(18f, 18f);
                int d = Dust.NewDust(spawnPos, 0, 0, DustID.Blood, vel.X, vel.Y, 0, default, Main.rand.NextFloat(1.5f, 2.8f));
                Main.dust[d].noGravity = Main.rand.NextBool(2);
            }
        }

        // ── Drawing Engine ─────────────────────────────────────────────────────────────
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            neckTexture ??= ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/Hydra_Neck");
            headTexture ??= ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/Hydra_Head");

            // Base neck anchor offsets on body (relative to NPC.Bottom facing left)
            // Relative coordinates: -X is Front/Chest side (left when facing left), +X is Back/Tail side.
            // Neck 0 (Purple / Back): offset (-15, -77) — raised 5px
            // Neck 1 (Red / Middle): offset (-9, -78) — raised 5px
            // Neck 2 (Orange / Front): offset (-2, -87)
            Vector2[] neckBaseOffsetsLeft = new Vector2[]
            {
                new Vector2(-15f, -77f),
                new Vector2(-9f, -78f),
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

            Color finalDrawColor = magicShieldActive ? Color.Lerp(drawColor, new Color(100, 200, 255), 0.75f) : drawColor;

            // Draw motion blur trail copies and enlarged Massacre-style radial halo during MagicShield!
            if (magicShieldActive)
            {
                // 1. Enlarged 20-pixel radial blue halo on all 12 sides around body (Juggernaut / Massacre style)
                Color haloColor = new Color(40, 140, 255, 0) * 0.45f;
                for (int i = 0; i < 12; i++)
                {
                    Vector2 haloOffset = new Vector2(20f, 0f).RotatedBy(MathHelper.TwoPi * i / 12f);
                    spriteBatch.Draw(bodyTex, drawPos + haloOffset, sourceRect, haloColor, NPC.rotation, origin, NPC.scale * 1.05f, effects, 0f);
                }

                // 2. 3x motion blur trail copies across 18 cached history positions
                for (int k = oldBodyPos.Length - 1; k >= 1; k--)
                {
                    if (oldBodyPos[k] != Vector2.Zero)
                    {
                        Vector2 trailBottom = oldBodyPos[k] + new Vector2(NPC.width / 2f, NPC.height);
                        Vector2 trailDrawPos = trailBottom - screenPos + new Vector2(0f, NPC.gfxOffY + 4f);
                        float alpha = (float)(oldBodyPos.Length - k) / oldBodyPos.Length * 0.45f;
                        Color trailColor = new Color(40, 140, 255, 0) * alpha;
                        spriteBatch.Draw(bodyTex, trailDrawPos, sourceRect, trailColor, NPC.rotation, origin, NPC.scale, effects, 0f);
                    }
                }
            }

            spriteBatch.Draw(bodyTex, drawPos, sourceRect, finalDrawColor, NPC.rotation, origin, NPC.scale, effects, 0f);
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

            Color finalDrawColor = magicShieldActive ? Color.Lerp(drawColor, new Color(100, 200, 255), 0.75f) : drawColor;

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

            if (neckIndex == 1 && middleAttackProgress != 0f)
            {
                // Smooth, continuous curve modulation during middle head attack
                if (middleAttackProgress < 0f)
                {
                    // Windup: rears back smoothly to 150deg
                    float ease = MathHelper.Clamp(-middleAttackProgress / 0.25f, 0f, 1f);
                    baseBendDegrees = MathHelper.Lerp(190f, 150f, ease);
                }
                else
                {
                    // Attack / Peak Hold / Retraction: sweeps forward up to 240deg
                    baseBendDegrees = MathHelper.Lerp(190f, 240f, middleAttackProgress);
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

            // Apply forward lunge displacement during middle head attacks (3x reach for LungeBite!)
            if (neckIndex == 1 && middleAttackProgress > 0f)
            {
                float maxReach = (currentMiddleAttack == AttackID.LungeBite) ? 320f : 110f;
                float lungeDisplacement = MathHelper.Lerp(0f, maxReach, MathHelper.Clamp(middleAttackProgress, 0f, 1f));
                Vector2 lungeVec = (middleLockedDir != Vector2.Zero ? middleLockedDir : (facingRight ? Vector2.UnitX : -Vector2.UnitX));
                for (int i = 1; i <= segmentCount; i++)
                {
                    float stepProgress = (float)i / segmentCount;
                    segmentPoints[i] += lungeVec * (lungeDisplacement * MathF.Sin(stepProgress * MathHelper.PiOver2));
                }
            }

            // Draw neck segments along segment points with dynamic length stretching to eliminate ALL gaps!
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 drawPos = segmentPoints[i] - screenPos;
                Vector2 nextPos = segmentPoints[i + 1];
                Vector2 diff = nextPos - segmentPoints[i];
                float neckRotation = diff.ToRotation();
                float segDist = diff.Length();

                // Dynamically stretch segment sprite length (+2.5px overlap) so there are NEVER any gaps between segments during extension/breathing!
                Vector2 segScale = new Vector2((segDist + 2.5f) / neckTex.Width * NPC.scale, NPC.scale);
                Vector2 neckOrigin = new Vector2(0f, neckTex.Height / 2f);

                // Draw motion blur trails and enlarged radial halo for neck segment if MagicShield is active
                if (magicShieldActive)
                {
                    // 1. Enlarged 16-pixel radial halo for neck segment
                    Color haloColor = new Color(40, 140, 255, 0) * 0.35f;
                    for (int h = 0; h < 4; h++)
                    {
                        Vector2 haloOffset = new Vector2(16f, 0f).RotatedBy(MathHelper.TwoPi * h / 4f);
                        spriteBatch.Draw(neckTex, drawPos + haloOffset, null, haloColor, neckRotation, neckOrigin, segScale * 1.06f, SpriteEffects.None, 0f);
                    }

                    // 2. 3x motion blur trails along old positions history
                    for (int k = 1; k <= 4; k++)
                    {
                        int posIndex = k * 4;
                        if (posIndex < oldBodyPos.Length && oldBodyPos[posIndex] != Vector2.Zero)
                        {
                            Vector2 trailOffset = oldBodyPos[posIndex] - NPC.position;
                            float alpha = (5f - k) / 5f * 0.35f;
                            Color trailColor = new Color(40, 140, 255, 0) * alpha;
                            spriteBatch.Draw(neckTex, drawPos + trailOffset, null, trailColor, neckRotation, neckOrigin, segScale, SpriteEffects.None, 0f);
                        }
                    }
                }

                spriteBatch.Draw(neckTex, drawPos, null, finalDrawColor, neckRotation, neckOrigin, segScale, SpriteEffects.None, 0f);
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

            // Calculate head targeting angle: during attack execution, head locks to neck vector without tracking player movement!
            float targetHeadAngle;
            int threshold = (currentMiddleAttack == AttackID.HydraScream) ? ScreamTelegraphTicks : LungeBiteTelegraphTicks;
            if (neckIndex == 1 && middleAttackTimer > threshold)
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

            // Draw motion blur trails and enlarged radial halo for head if MagicShield is active
            if (magicShieldActive)
            {
                // 1. Enlarged 20-pixel radial halo on all 8 sides for head (Juggernaut / Massacre style)
                Color headHaloColor = new Color(40, 140, 255, 0) * 0.5f;
                for (int h = 0; h < 8; h++)
                {
                    Vector2 haloOffset = new Vector2(20f, 0f).RotatedBy(MathHelper.TwoPi * h / 8f);
                    spriteBatch.Draw(headTex, headDrawPos + haloOffset, headSourceRect, headHaloColor, headRotation, headOrigin, NPC.scale * 1.08f, headEffects, 0f);
                }

                // 2. 3x motion blur trails along old positions history
                for (int k = 1; k <= 4; k++)
                {
                    int posIndex = k * 4;
                    if (posIndex < oldBodyPos.Length && oldBodyPos[posIndex] != Vector2.Zero)
                    {
                        Vector2 trailOffset = oldBodyPos[posIndex] - NPC.position;
                        float alpha = (5f - k) / 5f * 0.45f;
                        Color trailColor = new Color(40, 140, 255, 0) * alpha;
                        spriteBatch.Draw(headTex, headDrawPos + trailOffset, headSourceRect, trailColor, headRotation, headOrigin, NPC.scale, headEffects, 0f);
                    }
                }
            }

            spriteBatch.Draw(headTex, headDrawPos, headSourceRect, finalDrawColor, headRotation, headOrigin, NPC.scale, headEffects, 0f);
        }
    }
}
