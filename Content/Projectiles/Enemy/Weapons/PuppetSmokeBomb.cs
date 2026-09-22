using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class PuppetSmokeBomb : ModProjectile
    {
        private const float Gravity = 0.18f;
        // Smoke-bomb spec: 500px diameter, non-damaging obscuring cloud for 120 ticks. Its particles
        // fill that same footprint rather than remaining concentrated at the impact point.
        private const int CloudRadius = 250;
        private const int CloudDuration = 120;
        // Four times the old sustained and burst counts (2 -> 8, 85 -> 340). Raising count rather
        // than scale makes the large cloud denser without turning individual smoke motes blocky.
        private const int CloudParticlesPerTick = 8;
        private const int CloudBurstParticleCount = 340;
        private const int BlackoutDuration = 3 * 60;

        private bool Exploded => Projectile.ai[0] == 1f;

        /// <summary>Slot of the looping fuse sound started when the bomb appears in the thrower's hand
        /// (see BlackNinja.OnRangedBurstStarted).  Stopped here when the bomb detonates / despawns so
        /// the 3 s clip doesn't outlive the bomb.  Static = assumes one live fuse at a time.</summary>
        internal static SlotId ActiveFuseSlot = SlotId.Invalid;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemySmokebomb));

        /// <summary>Stops the one active Black Ninja fuse when the encounter or its bomb ends.</summary>
        internal static void StopFuse()
        {
            if (SoundEngine.TryGetActiveSound(ActiveFuseSlot, out ActiveSound fuse))
                fuse.Stop();
            ActiveFuseSlot = SlotId.Invalid;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = -1;
            Projectile.damage = 1;
            Projectile.knockBack = 0f;
            Projectile.DamageType = DamageClass.Ranged;
        }


        public override void AI()
        {
            if (Exploded)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.alpha = 255;

                for (int i = 0; i < CloudParticlesPerTick; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(CloudRadius, CloudRadius);
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Smoke, Main.rand.NextVector2Circular(2.4f, 2.4f), 140, Color.Gray, Main.rand.NextFloat(1.6f, 2.7f));
                    dust.noGravity = true;
                }
                return;
            }

            Projectile.velocity.Y += Gravity;
            if (Projectile.velocity.Y > 14f)
            {
                Projectile.velocity.Y = 14f;
            }

            Projectile.rotation += Projectile.velocity.X * 0.08f;
            if (Main.rand.NextBool(4))
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, 0f, 160, default, 0.75f);
            }

            // Lit-fuse: red sparks off the top of the bomb as it travels.
            if (Main.rand.NextBool(2))
            {
                Vector2 top = Projectile.Center - new Vector2(0f, Projectile.height * 0.5f);
                Dust spark = Dust.NewDustPerfect(top, DustID.RedTorch,
                    new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-1.3f, -0.3f)),
                    0, default, Main.rand.NextFloat(0.7f, 1.2f));
                spark.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft) => StopFuse();

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Explode();
            return false;
        }

        public override void ModifyHitPlayer(Player target, ref Player.HurtModifiers modifiers)
        {
            modifiers.FinalDamage *= 0f;
            modifiers.Knockback *= 0f;
            modifiers.DisableSound();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            ApplyDebuffs(target);
            Explode();
        }

        private void Explode()
        {
            if (Exploded)
            {
                return;
            }

            Projectile.ai[0] = 1f;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = CloudDuration;
            Projectile.position = Projectile.Center - new Vector2(CloudRadius);
            Projectile.width = CloudRadius * 2;
            Projectile.height = CloudRadius * 2;
            Projectile.netUpdate = true;

            StopFuse(); // cut the 3 s fuse clip the instant it detonates
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.75f, PitchVariance = 0.25f }, Projectile.Center);

            for (int i = 0; i < CloudBurstParticleCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(5.5f, 5.5f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(CloudRadius, CloudRadius), DustID.Smoke, velocity, 120, Color.Gray, Main.rand.NextFloat(1.5f, 3.1f));
                dust.noGravity = true;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.Distance(Projectile.Center) <= CloudRadius)
                {
                    ApplyDebuffs(player);
                }
            }
        }

        private static void ApplyDebuffs(Player target)
        {
            target.AddBuff(ModContent.BuffType<Crippled>(), 6 * 60);
            target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 30 * 60);
            target.AddBuff(BuffID.Slow, 3 * 60);
            target.AddBuff(BuffID.Blackout, BlackoutDuration);
        }
    }
}
