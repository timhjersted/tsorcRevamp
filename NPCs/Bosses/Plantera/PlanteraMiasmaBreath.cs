using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;

namespace tsorcRevamp.NPCs.Bosses.Plantera
{
    /// <summary>
    /// Miasma Breath: a phase-2 (below 50% life) attack layered on top of vanilla Plantera's AI.
    /// Plantera slows, gathers purple/pink miasma in its mouth for a second, then exhales a sweeping
    /// stream of MiasmaBreathPuff at the player for two seconds. The sweep turns slower than the
    /// player can circle, so the counterplay is to keep moving around her.
    ///
    /// State lives in npc.ai[3] (the "breath clock"), which vanilla Plantera never uses and which
    /// vanilla NPC sync already sends. Every machine advances the clock deterministically; only the
    /// server starts a breath (gates + aim) and restarts the cooldown, with netUpdate at each.
    /// </summary>
    public class PlanteraMiasmaBreath : GlobalNPC
    {
        // Cooldown 7s + 0-4s jitter between breaths, so it isn't metronomic.
        public const int CooldownTicks = 420;
        public const int CooldownJitterTicks = 240;
        public const int TelegraphTicks = 60;
        public const int BreathTicks = 120;

        // Clock values marking each phase boundary. Clock == ReadyClock means "waiting for the server to start".
        private const int ReadyClock = CooldownTicks;
        private const int BreathStartClock = ReadyClock + TelegraphTicks;
        private const int BreathEndClock = BreathStartClock + BreathTicks;

        // One puff every 2 ticks at 11 px/tick; puffs decay to ~390px reach (see MiasmaBreathPuff).
        public const int PuffIntervalTicks = 2;
        public const float PuffSpeed = 11f;
        public const float PuffSpreadRadians = 0.14f;

        // Sweep turn rate 0.025 rad/tick = ~86°/s: tracks a standing player, loses one who circles.
        public const float BreathTurnRate = 0.025f;

        // Only start when the player is reachable: within ~35 tiles and in line of sight.
        public const float MaxStartDistance = 560f;

        // Per-tick velocity damping on top of vanilla's acceleration. 0.9 ≈ a near stop for the
        // wind-up; 0.96 lets her keep drifting slowly while exhaling.
        public const float TelegraphDamping = 0.9f;
        public const float BreathDamping = 0.96f;

        // Per-puff damage before Expert/Master scaling, close to vanilla's poison seed (27).
        public const float PuffDamageNormal = 26f;
        public const float PuffDamageExpert = 24f;

        public override bool InstancePerEntity => true;

        // Current sweep angle. Authoritative on the server (it spawns the puffs); cosmetic on clients.
        private float breathAim;
        private float previousClock;

        public override bool AppliesToEntity(NPC entity, bool lateInstantiation)
        {
            return entity.type == NPCID.Plantera;
        }

        public override void PostAI(NPC npc)
        {
            bool inSecondPhase = npc.life < npc.lifeMax / 2;
            if (!inSecondPhase)
            {
                npc.ai[3] = 0f;
                previousClock = 0f;
                return;
            }

            bool isAuthority = Main.netMode != NetmodeID.MultiplayerClient;
            Player target = Main.player[npc.target];
            float clock = npc.ai[3];

            // Idle: count up to Ready. Everyone advances; the server's netUpdates correct drift.
            if (clock < ReadyClock)
            {
                npc.ai[3] = clock + 1f;
                previousClock = clock;
                return;
            }

            // Ready: only the server may start a breath, and only at a reachable, visible target.
            if (clock == ReadyClock)
            {
                if (isAuthority && CanStartBreath(npc, target))
                {
                    npc.ai[3] = ReadyClock + 1f;
                    npc.netUpdate = true;
                }

                previousClock = clock;
                return;
            }

            // Telegraph start: one-shot cue, detected by crossing so a client that only learned of it
            // from a late packet still plays it.
            if (previousClock <= ReadyClock && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 1f, Pitch = -0.5f }, npc.Center);
            }

            Vector2 toTarget = target.Center - npc.Center;
            float angleToTarget = toTarget.ToRotation();

            if (clock < BreathStartClock)
            {
                // Wind-up: she nearly stops and keeps her mouth on the player (vanilla rotation).
                npc.velocity *= TelegraphDamping;
                breathAim = angleToTarget;

                if (!Main.dedServ)
                {
                    float progress = (clock - ReadyClock) / TelegraphTicks;
                    SpawnGatheringDust(npc, progress);
                }
            }
            else if (clock < BreathEndClock)
            {
                // Exhale: the sweep lags behind the player, and her sprite follows the breath, not the player.
                npc.velocity *= BreathDamping;
                breathAim = breathAim.AngleTowards(angleToTarget, BreathTurnRate);
                npc.rotation = breathAim + MathHelper.PiOver2;

                int breathTick = (int)(clock - BreathStartClock);
                bool isPuffTick = breathTick % PuffIntervalTicks == 0;

                if (isAuthority && isPuffTick)
                {
                    float puffAngle = breathAim + Main.rand.NextFloat(-PuffSpreadRadians, PuffSpreadRadians);
                    Vector2 puffVelocity = puffAngle.ToRotationVector2() * PuffSpeed;
                    int puffDamage = npc.GetAttackDamage_ForProjectiles(PuffDamageNormal, PuffDamageExpert);
                    Projectile.NewProjectile(npc.GetSource_FromAI(), MouthPosition(npc, breathAim), puffVelocity,
                        ModContent.ProjectileType<MiasmaBreathPuff>(), puffDamage, 0f, Main.myPlayer);
                }

                if (!Main.dedServ)
                {
                    Lighting.AddLight(MouthPosition(npc, breathAim), MiasmaPlayer.MiasmaColor.ToVector3() * 0.8f);

                    if (Main.rand.NextBool(4))
                    {
                        SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.8f, Pitch = -0.5f, PitchVariance = 0.3f }, npc.Center);
                    }
                }
            }
            else
            {
                // Finished. Clients hold here until the server's reset arrives.
                if (isAuthority)
                {
                    npc.ai[3] = -Main.rand.Next(0, CooldownJitterTicks + 1);
                    npc.netUpdate = true;
                }

                previousClock = clock;
                return;
            }

            npc.ai[3] = clock + 1f;
            previousClock = clock;
        }

        private static bool CanStartBreath(NPC npc, Player target)
        {
            if (!target.active || target.dead)
            {
                return false;
            }

            float distance = Vector2.Distance(npc.Center, target.Center);
            if (distance > MaxStartDistance)
            {
                return false;
            }

            return Collision.CanHit(npc.position, npc.width, npc.height, target.position, target.width, target.height);
        }

        // The mouth is the front edge of the bulb along the facing angle, a few px inside the sprite.
        private static Vector2 MouthPosition(NPC npc, float facingAngle)
        {
            float mouthDistance = npc.height * 0.5f + 6f;
            return npc.Center + facingAngle.ToRotationVector2() * mouthDistance;
        }

        // Telegraph: dust spirals inward to the mouth from 80px out, getting denser and larger as the breath nears.
        private static void SpawnGatheringDust(NPC npc, float progress)
        {
            float facingAngle = npc.rotation - MathHelper.PiOver2;
            Vector2 mouth = MouthPosition(npc, facingAngle);
            int dustCount = 1 + (int)(progress * 3f);

            for (int i = 0; i < dustCount; i++)
            {
                Color dustColor = MiasmaPlayer.MiasmaColor;

                if (Main.rand.NextBool())
                {
                    dustColor = MiasmaPlayer.MiasmaPinkColor;
                }

                Vector2 offset = Main.rand.NextVector2CircularEdge(80f, 80f);
                Vector2 inwardVelocity = -offset * 0.08f;
                float scale = MathHelper.Lerp(0.8f, 1.5f, progress);

                Dust dust = Dust.NewDustPerfect(mouth + offset, DustID.RainbowMk2, inwardVelocity, 60, dustColor, scale);
                dust.noGravity = true;
            }

            Lighting.AddLight(mouth, MiasmaPlayer.MiasmaColor.ToVector3() * progress);
        }
    }

    /// <summary>
    /// One puff of Plantera's Miasma Breath. Short-lived, decelerating and billowing, so the stream
    /// is many small hits gated by player i-frames. Miasma buildup is applied in
    /// tsorcGlobalProjectile.OnHitPlayer (PlanteraBreathMiasmaBuildup).
    /// </summary>
    public class MiasmaBreathPuff : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        // 11 px/tick decaying 1.5%/tick for 50 ticks travels 11·(1-0.985^50)/0.015 ≈ 390px.
        private const int Lifetime = 50;
        private const float Decay = 0.985f;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = Lifetime;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity *= Decay;

            if (Main.dedServ)
            {
                return;
            }

            // Cloud grows and thins with age; the alpha ramp is the fade-out, so it never pops off at full opacity.
            float age = 1f - Projectile.timeLeft / (float)Lifetime;
            int alpha = (int)(60f + age * 160f);
            float scale = 1.1f + age * 0.8f;

            for (int i = 0; i < 2; i++)
            {
                Color dustColor = MiasmaPlayer.MiasmaColor;

                if (i == 1)
                {
                    dustColor = MiasmaPlayer.MiasmaPinkColor;
                }

                Vector2 spawnPosition = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.5f);
                Vector2 dustVelocity = Projectile.velocity * 0.5f + Main.rand.NextVector2Circular(0.8f, 0.8f);

                Dust dust = Dust.NewDustPerfect(spawnPosition, DustID.RainbowMk2, dustVelocity, alpha, dustColor, scale);
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, MiasmaPlayer.MiasmaColor.ToVector3() * 0.3f * (1f - age));
        }

        // Breath doesn't pass through terrain; a wall soaks it up. Deliberately piercing on players (penetrate -1).
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }

        // Small puff, not the usual ~70-dust burst: dozens of these die per breath. Deliberate.
        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 5; i++)
            {
                Color dustColor = MiasmaPlayer.MiasmaColor;

                if (i % 2 == 0)
                {
                    dustColor = MiasmaPlayer.MiasmaPinkColor;
                }

                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.RainbowMk2, Main.rand.NextVector2Circular(1.5f, 1.5f), 150, dustColor, 1.3f);
                dust.noGravity = true;
            }
        }
    }
}
