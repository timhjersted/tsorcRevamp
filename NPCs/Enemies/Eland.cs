using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    // Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
    public class Eland : ModNPC
    {
        float npcAcSPD = 0.7f; //How fast they accelerate.
        float npcSPD = 2.3f; //Max speed

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 14; // 924px sheet / 14 = 66px frames (15 didn't divide evenly, causing frame drift that looked like the sprite continuously rising)
        }

        public override void SetDefaults()
        {
            NPC.lifeMax = 500;
            NPC.damage = 60;
            NPC.defense = 6;
            NPC.knockBackResist = 0.4f;
            NPC.width = 30;
            NPC.height = 40;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.HitSound = SoundID.NPCHit1;
            // value/25 = expert-mode soul drop (see GlobalNPC.OnKill's Dark Souls formula) — 5000 -> 200 souls.
            NPC.value = 5000;
            // No AnimationType: the sheet's layout (0=idle, 1=jump, 2..=walk) doesn't match any
            // vanilla NPC's, so framing is driven explicitly in FindFrame below.

            if (Main.hardMode)
            {
                NPC.lifeMax = 1000;
                NPC.value = 7500; // -> 300 souls in expert mode
            }

            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().NavSearchRadius = 40; // SmartFighter4AI movement
        }

        // Rare pre-hardmode, slightly more common in hardmode; jungle biome, underground (dirt or rock layer) only.
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player p = spawnInfo.Player;
            bool undergroundJungle = p.ZoneJungle && (p.ZoneDirtLayerHeight || p.ZoneRockLayerHeight);
            if (!undergroundJungle)
                return 0f;

            return Main.hardMode ? 0.05f : 0.02f;
        }

        // Spritesheet layout: frame 0 = idle, frame 1 = jump, frames 2..(count-1) = walk cycle.
        public override void FindFrame(int frameHeight)
        {
            int frameCount = Main.npcFrameCount[NPC.type];
            const int walkStart = 2;
            int walkEnd = frameCount - 1;

            // Airborne → jump frame.
            if (NPC.velocity.Y != 0f)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = frameHeight; // frame 1
                return;
            }

            // Grounded and essentially still → idle frame.
            if (Math.Abs(NPC.velocity.X) < 0.1f)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0; // frame 0
                return;
            }

            // Walking → advance the cycle within [walkStart, walkEnd]; pace tied to walk speed.
            int currentFrame = NPC.frame.Y / frameHeight;
            if (currentFrame < walkStart || currentFrame > walkEnd)
                currentFrame = walkStart;

            NPC.frameCounter += Math.Abs(NPC.velocity.X);
            if (NPC.frameCounter >= 9.0)
            {
                NPC.frameCounter = 0;
                currentFrame++;
                if (currentFrame > walkEnd)
                    currentFrame = walkStart;
            }
            NPC.frame.Y = currentFrame * frameHeight;
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, topSpeed: npcSPD, acceleration: npcAcSPD);

            UpdatePoisonTrail();
            UpdateSpitAttacks();
        }

        // Pre-hardmode / hardmode / super-hardmode tiered value, used for every attack's damage below.
        static int Tiered(int preHardmode, int hardmode, int superHardmode)
        {
            if (tsorcRevampWorld.SuperHardMode) return superHardmode;
            if (Main.hardMode) return hardmode;
            return preHardmode;
        }

        // ================================================================================
        //  Toxic touch — passive contact poison, independent of everything below.
        // ================================================================================
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Poisoned, 4 * 60, false);
            }
        }

        // ================================================================================
        //  Poison trail — floating gas puffs dropped behind Eland while it walks. Runs on its own
        //  timers and is never interrupted by / doesn't interrupt the spit attacks below.
        // ================================================================================
        const int TrailDurationTicks = 12 * 60;   // keeps emitting for 12s
        const int TrailCooldownTicks = 20 * 60;   // 20s before it can start emitting again
        const int TrailPuffIntervalTicks = 20;    // a new puff roughly every 1/3s while emitting
        // Each puff's own 6s lifetime lives on PoisonTrailCloud.SetDefaults (Projectile.timeLeft).

        bool trailActive;
        int trailEmitTimer;
        int trailCooldownTimer;

        void UpdatePoisonTrail()
        {
            if (trailCooldownTimer > 0)
                trailCooldownTimer--;

            if (trailActive)
            {
                trailEmitTimer++;
                if (trailEmitTimer % TrailPuffIntervalTicks == 0)
                {
                    SpawnTrailPuff();
                }
                if (trailEmitTimer >= TrailDurationTicks)
                {
                    trailActive = false;
                    trailCooldownTimer = TrailCooldownTicks;
                }
            }
            else if (trailCooldownTimer <= 0 && NPC.HasValidTarget)
            {
                trailActive = true;
                trailEmitTimer = 0;
                SpawnTrailPuff();
            }
        }

        void SpawnTrailPuff()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            const int size = Projectiles.Enemy.PoisonTrailCloud.MinSize;
            float behindX = -NPC.direction * (NPC.width * 0.5f + 24f);
            Vector2 spawn = NPC.Center + new Vector2(behindX, -8f) - new Vector2(size, size) * 0.5f;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.PoisonTrailCloud>(), Tiered(20, 40, 60), 0f, Main.myPlayer);
        }

        // ================================================================================
        //  Attacks — a shared telegraph→execute→recovery FSM reused by 6 attacks. "Recovery" is a
        //  brief shared lockout right after any attack finishes (nothing else can start); "cooldown"
        //  is that SPECIFIC attack's own longer re-use gate, ticking down independently every frame
        //  regardless of FSM state, so a long-cooldown attack doesn't block others from firing sooner.
        // ================================================================================
        enum SpitAttack { ComboA, ComboB, ComboC, StickySpit, ToxicGasNova, ChargeJump }
        enum SpitState { Idle, Telegraph, Firing }

        const float SpitRange = 650f;
        const float NovaTriggerRange = 160f; // "close range"
        const float NovaRadius = 400f;
        const float ChargeMinRange = 250f;   // "medium"
        const float ChargeMaxRange = 650f;   // "long"

        static readonly Vector2[] ThreeSpread = { new Vector2(-24, 0), new Vector2(0, 0), new Vector2(24, 0) };
        static readonly Vector2[] TwoSpread = { new Vector2(-16, 0), new Vector2(16, 0) };
        // Sticky Spit's 8 single-shot offsets: floor center/sides, then upper/wall angles, for max
        // ground+wall+ceiling coverage around the frozen target.
        static readonly Vector2[] StickyCoverage =
        {
            new Vector2(0, 0),
            new Vector2(-48, 0),
            new Vector2(48, 0),
            new Vector2(-24, -64),
            new Vector2(24, -64),
            new Vector2(0, -96),
            new Vector2(-64, -32),
            new Vector2(64, -32),
        };
        // Charge Jump's suppression volley: 5 poison balls fired past the player (behind, relative to
        // Eland) before the leap, so backing away from the charge walks the player into them.
        static readonly float[] ChargeSuppressionDistances = { 60f, 90f, 120f, 150f, 180f };

        SpitState spitState = SpitState.Idle;
        SpitAttack activeSpit;
        int spitTimer;
        int recoveryTimer; // shared FSM lockout after any attack finishes
        int volleyIndex;
        Vector2 frozenTarget;
        List<(int tick, Vector2[] offsets)> schedule;
        int currentProjType;
        int currentDamage;
        float currentSpeed;
        float currentGravity;
        bool chargeLaunched;
        bool chargeBurstDone;

        int comboACooldown, comboBCooldown, comboCCooldown, stickySpitCooldown, novaCooldown, chargeCooldown;

        void UpdateSpitAttacks()
        {
            if (comboACooldown > 0) comboACooldown--;
            if (comboBCooldown > 0) comboBCooldown--;
            if (comboCCooldown > 0) comboCCooldown--;
            if (stickySpitCooldown > 0) stickySpitCooldown--;
            if (novaCooldown > 0) novaCooldown--;
            if (chargeCooldown > 0) chargeCooldown--;

            if (!NPC.HasValidTarget)
                return;
            Player target = Main.player[NPC.target];
            if (target.dead)
                return;

            switch (spitState)
            {
                case SpitState.Idle:
                    if (recoveryTimer > 0)
                    {
                        recoveryTimer--;
                        return;
                    }
                    if (NPC.velocity.Y != 0f)
                        return; // don't start a windup mid-air

                    float distance = NPC.Distance(target.Center);
                    bool lowHp = NPC.life < NPC.lifeMax * 0.5f;

                    if (lowHp && comboCCooldown == 0)
                    {
                        if (Main.rand.NextBool(90))
                            BeginAttack(SpitAttack.ComboC, target);
                        break;
                    }

                    // Toxic Gas Nova only procs at close range.
                    if (distance <= NovaTriggerRange && novaCooldown == 0 && Main.rand.NextBool(60))
                    {
                        BeginAttack(SpitAttack.ToxicGasNova, target);
                        break;
                    }

                    // Charge Jump only at medium-to-long range.
                    if (distance >= ChargeMinRange && distance <= ChargeMaxRange && chargeCooldown == 0 && Main.rand.NextBool(90))
                    {
                        BeginAttack(SpitAttack.ChargeJump, target);
                        break;
                    }

                    if (distance <= SpitRange && Main.rand.NextBool(90))
                    {
                        List<SpitAttack> options = new List<SpitAttack>();
                        if (comboACooldown == 0) options.Add(SpitAttack.ComboA);
                        if (comboBCooldown == 0) options.Add(SpitAttack.ComboB);
                        if (stickySpitCooldown == 0) options.Add(SpitAttack.StickySpit);
                        if (options.Count > 0)
                            BeginAttack(options[Main.rand.Next(options.Count)], target);
                    }
                    break;

                case SpitState.Telegraph:
                    NPC.velocity.X *= 0.7f; // plant to spit
                    spitTimer++;
                    if (activeSpit == SpitAttack.ToxicGasNova)
                    {
                        // Ring pulses AT the true detonation radius the whole telegraph (doesn't grow into it) —
                        // shows exactly where the blast will land. Matches ToxicGasNovaGlow's own pulse.
                        float pulse = 0.85f + 0.15f * (float)Math.Sin(spitTimer * 0.3f);
                        UsefulFunctions.DustRingPrecise(NPC.Center, NovaRadius, DustID.Poisoned, 48, 0f, 120, pulse * 1.3f);
                    }
                    if (spitTimer >= TelegraphTicks(activeSpit))
                    {
                        spitState = SpitState.Firing;
                        spitTimer = 0;
                        volleyIndex = 0;
                    }
                    break;

                case SpitState.Firing:
                    switch (activeSpit)
                    {
                        case SpitAttack.ToxicGasNova:
                            UpdateToxicGasNovaFiring(target);
                            break;
                        case SpitAttack.ChargeJump:
                            UpdateChargeJumpFiring(target);
                            break;
                        default:
                            UpdateVolleyFiring();
                            break;
                    }
                    break;
            }
        }

        void BeginAttack(SpitAttack attack, Player target)
        {
            activeSpit = attack;
            frozenTarget = target.Center; // frozen now — attacks aim here even if the player moves
            spitState = SpitState.Telegraph;
            spitTimer = 0;
            volleyIndex = 0;
            schedule = null;
            chargeLaunched = false;
            chargeBurstDone = false;

            // Damage bypasses the AddAttack/SimpleProjectile framework entirely (direct Projectile.NewProjectile
            // calls below), so these numbers land exactly as written — tiered by difficulty ourselves.
            switch (attack)
            {
                case SpitAttack.StickySpit:
                    schedule = BuildSchedule(attack);
                    currentProjType = ModContent.ProjectileType<Projectiles.Enemy.StickySpitBall>();
                    currentDamage = Tiered(40, 60, 80); // "poison strike"
                    currentSpeed = 8f;
                    currentGravity = 0.3f;
                    break;
                case SpitAttack.ToxicGasNova:
                case SpitAttack.ChargeJump:
                    break; // handled by their own Firing routines
                default:
                    schedule = BuildSchedule(attack);
                    currentProjType = ModContent.ProjectileType<Projectiles.Enemy.PoisonSpitBall>();
                    currentDamage = Tiered(50, 70, 90); // "spit"
                    currentSpeed = 9f;
                    currentGravity = 0.25f;
                    break;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, NPC.velocity,
                    ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer,
                    UsefulFunctions.ColorToFloat(Color.LimeGreen));

                if (attack == SpitAttack.ToxicGasNova)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.VFX.ToxicGasNovaGlow>(), 0, 0f, Main.myPlayer, NPC.whoAmI, NovaRadius);
                }
            }
        }

        static List<(int tick, Vector2[] offsets)> BuildSchedule(SpitAttack attack)
        {
            switch (attack)
            {
                // 3 lobs at once, twice, 30 ticks apart.
                case SpitAttack.ComboA:
                    return new List<(int, Vector2[])>
                    {
                        (0, ThreeSpread),
                        (30, ThreeSpread),
                    };

                // 4 volleys of 3, 25 ticks apart.
                case SpitAttack.ComboB:
                    return new List<(int, Vector2[])>
                    {
                        (0, ThreeSpread),
                        (25, ThreeSpread),
                        (50, ThreeSpread),
                        (75, ThreeSpread),
                    };

                // Enrage: 8 volleys of 2 — first 2 volleys 40 ticks apart, remaining 6 volleys 15 ticks apart.
                case SpitAttack.ComboC:
                    return new List<(int, Vector2[])>
                    {
                        (0, TwoSpread),
                        (40, TwoSpread),
                        (55, TwoSpread),
                        (70, TwoSpread),
                        (85, TwoSpread),
                        (100, TwoSpread),
                        (115, TwoSpread),
                        (130, TwoSpread),
                    };

                // Sticky Spit: 8 single shots, 5 ticks apart, fanned for ground/wall/ceiling coverage.
                case SpitAttack.StickySpit:
                    var list = new List<(int, Vector2[])>();
                    for (int i = 0; i < StickyCoverage.Length; i++)
                    {
                        list.Add((i * 5, new[] { StickyCoverage[i] }));
                    }
                    return list;

                default:
                    return new List<(int, Vector2[])>();
            }
        }

        // Short shared FSM lockout right after this attack finishes — nothing else can start during it.
        static int RecoveryTicks(SpitAttack attack) => attack switch
        {
            SpitAttack.ComboA => 90,
            SpitAttack.ComboB => 120,
            SpitAttack.ComboC => 160,
            SpitAttack.StickySpit => 120,
            SpitAttack.ToxicGasNova => 120,
            SpitAttack.ChargeJump => 90,
            _ => 60,
        };

        // This specific attack's own re-use gate — for the original 4 combos this equals RecoveryTicks
        // (one number was ever given for them); Nova/Charge have a much longer cooldown than their recovery.
        static int CooldownTicks(SpitAttack attack) => attack switch
        {
            SpitAttack.ToxicGasNova => 12 * 60, // 12s
            SpitAttack.ChargeJump => 10 * 60,   // 10s
            _ => RecoveryTicks(attack),
        };

        static int TelegraphTicks(SpitAttack attack) => attack switch
        {
            SpitAttack.ToxicGasNova => 60,
            _ => 25, // matches the shared framework's default ProjectileTelegraphTime
        };

        void SetCooldown(SpitAttack attack, int value)
        {
            switch (attack)
            {
                case SpitAttack.ComboA: comboACooldown = value; break;
                case SpitAttack.ComboB: comboBCooldown = value; break;
                case SpitAttack.ComboC: comboCCooldown = value; break;
                case SpitAttack.StickySpit: stickySpitCooldown = value; break;
                case SpitAttack.ToxicGasNova: novaCooldown = value; break;
                case SpitAttack.ChargeJump: chargeCooldown = value; break;
            }
        }

        void FinishAttack()
        {
            SetCooldown(activeSpit, CooldownTicks(activeSpit));
            recoveryTimer = RecoveryTicks(activeSpit);
            spitState = SpitState.Idle;
            spitTimer = 0;
        }

        void UpdateVolleyFiring()
        {
            NPC.velocity.X *= 0.85f;
            spitTimer++;
            while (volleyIndex < schedule.Count && schedule[volleyIndex].tick <= spitTimer)
            {
                FireVolley(schedule[volleyIndex].offsets);
                volleyIndex++;
            }
            if (volleyIndex >= schedule.Count)
            {
                FinishAttack();
            }
        }

        void FireVolley(Vector2[] offsets)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            foreach (Vector2 offset in offsets)
            {
                Vector2 aimPoint = frozenTarget + offset;
                Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, aimPoint, currentSpeed, currentGravity);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, currentProjType, currentDamage, 0f, Main.myPlayer, currentGravity);
            }
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, NPC.Center);
        }

        // ================================================================================
        //  Toxic Gas Nova — close range only. Telegraph (60 ticks, a full-radius pulsing dust ring +
        //  matching green shader glow via ToxicGasNovaGlow, both showing the true blast size the whole
        //  time) ends in a poison fog explosion filling the entire 400px radius: an instant scattered
        //  burst + damage/12s Poisoned to whoever's still inside, followed by a few seconds of lingering
        //  fog (ToxicGasNovaFog) that keeps poisoning anyone who walks into it afterward.
        // ================================================================================
        void UpdateToxicGasNovaFiring(Player target)
        {
            if (spitTimer == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (NPC.Distance(target.Center) <= NovaRadius + 20f)
                    {
                        int hitDir = target.Center.X < NPC.Center.X ? -1 : 1;
                        target.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), Tiered(30, 50, 70), hitDir, dodgeable: true);
                        target.AddBuff(BuffID.Poisoned, 12 * 60, false);
                    }

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Enemy.ToxicGasNovaFog>(), Tiered(20, 40, 60), 0f, Main.myPlayer);
                }

                // Instant "boom": dust scattered across the WHOLE disc, not just the ring edge.
                for (int i = 0; i < 120; i++)
                {
                    Vector2 spot = NPC.Center + Main.rand.NextVector2Circular(NovaRadius, NovaRadius);
                    int dust = Dust.NewDust(spot, 1, 1, DustID.Poisoned, 0f, 0f, 100, default, 1.5f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = Main.rand.NextVector2Circular(1.2f, 1.2f);
                }
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f }, NPC.Center);
            }

            spitTimer++;
            if (spitTimer >= 20) // brief linger so the pop is visible before returning to idle
            {
                FinishAttack();
            }
        }

        // ================================================================================
        //  Charge Jump — medium/long range only. Fires 5 poison balls in a spread PAST the player
        //  (cutting off backward retreat), then leaps at the frozen target; bursts poison gas on
        //  contact with the player mid-air, or on landing if it never touched them.
        // ================================================================================
        void UpdateChargeJumpFiring(Player target)
        {
            spitTimer++;

            if (!chargeLaunched)
            {
                FireChargeSuppressionVolley();
                LaunchCharge();
                chargeLaunched = true;
                return; // skip the landed-check this tick — velocity was just set
            }

            if (!chargeBurstDone && NPC.velocity.Y != 0f && NPC.Hitbox.Intersects(target.Hitbox))
            {
                SpawnPoisonBurst(NPC.Center);
                chargeBurstDone = true;
            }

            if (NPC.velocity.Y == 0f)
            {
                if (!chargeBurstDone)
                {
                    SpawnPoisonBurst(NPC.Center);
                    chargeBurstDone = true;
                }
                FinishAttack();
                return;
            }

            // Safety timeout so a charge that somehow never "lands" (falls into water/a pit) can't get stuck.
            if (spitTimer > 300)
            {
                FinishAttack();
            }
        }

        void FireChargeSuppressionVolley()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            float dirSign = frozenTarget.X >= NPC.Center.X ? 1f : -1f;
            int damage = Tiered(50, 70, 90); // "spit" tier
            for (int i = 0; i < ChargeSuppressionDistances.Length; i++)
            {
                Vector2 aimPoint = frozenTarget + new Vector2(dirSign * ChargeSuppressionDistances[i], (i - 2) * 10f);
                Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, aimPoint, 9f, 0.25f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.PoisonSpitBall>(), damage, 0f, Main.myPlayer, 0.25f);
            }
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f }, NPC.Center);
        }

        void LaunchCharge()
        {
            float rotation = (float)Math.Atan2(frozenTarget.Y - NPC.Center.Y, frozenTarget.X - NPC.Center.X);
            const float chargeSpeed = 9f;
            NPC.velocity.X = (float)Math.Cos(rotation) * chargeSpeed;
            NPC.velocity.Y = (float)Math.Sin(rotation) * chargeSpeed - 3f; // extra pop so it arcs like a leap
            NPC.netUpdate = true;
        }

        // Shared by Charge Jump's landing/contact burst and the on-death burst below.
        void SpawnPoisonBurst(Vector2 center)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int damage = Tiered(20, 40, 60); // "gas cloud" tier
            const int burstCount = 8;
            for (int i = 0; i < burstCount; i++)
            {
                float rotation = MathHelper.TwoPi * i / burstCount;
                Vector2 velocity = new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) * (0.5f + Main.rand.NextFloat(-0.15f, 0.15f));
                Projectile.NewProjectile(NPC.GetSource_Death(), center, velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.PoisonBurstCloud>(), damage, 0f, Main.myPlayer);
            }
        }

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("ElandGore1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("ElandGore2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("ElandGore3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("ElandGore2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("ElandGore3").Type, 1f);
            }
            // Drops commented out

            // Death burst: a ring of poison gas puffs (double-size PoisonSmog, see PoisonBurstCloud).
            SpawnPoisonBurst(NPC.Center);
        }
    }
}
