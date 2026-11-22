using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    internal sealed class DestroyerProbeExplosion : ModProjectile
    {
        private const int FireTeleportFlameCount = 12;
        private const int DecorativeFlameCount = 8;
        private const float FireTeleportFlameSpeed = 5.6f;
        private const int FlameLifetime = 30;
        private const int MistDuration = 20;
        private const float MistRadius = 28f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyLingeringLaser));

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = MistDuration;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor) => false;

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                TriggerInitialBurst();
            }

            if (Main.dedServ)
            {
                return;
            }

            // Lingering fire-teleport mist cloud over the 20-tick lifetime.
            for (int i = 0; i < 7; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(MistRadius, MistRadius);
                Vector2 velocity = Main.rand.NextVector2Circular(0.8f, 0.8f);
                if (Main.rand.NextBool())
                {
                    Dust fire = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Torch, velocity, 180,
                        new Color(255, 100, 20), Main.rand.NextFloat(1.05f, 1.7f));
                    fire.noGravity = true;
                }
                else
                {
                    Dust smoke = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Smoke, velocity * 0.5f, 200,
                        new Color(60, 40, 30), Main.rand.NextFloat(0.6f, 0.9f));
                    smoke.noGravity = true;
                    smoke.fadeIn = Main.rand.NextFloat(0.95f, 1.4f);
                }

                if (Main.rand.NextBool())
                {
                    Vector2 outward = offset.SafeNormalize(Main.rand.NextVector2Unit());
                    int emberDustType = DustID.Torch;
                    if (Main.rand.NextBool(4))
                    {
                        emberDustType = DustID.OrangeTorch;
                    }

                    Vector2 emberVelocity = outward * Main.rand.NextFloat(1.6f, 4.2f) - Vector2.UnitY * Main.rand.NextFloat(0.2f, 1.1f);
                    Dust ember = Dust.NewDustPerfect(Projectile.Center + offset * 0.7f, emberDustType, emberVelocity,
                        120, default, Main.rand.NextFloat(0.4f, 0.8f));
                    ember.noGravity = true;
                }
            }
        }

        private void TriggerInitialBurst()
        {
            float rotationOffset = Main.rand.NextFloat(MathHelper.TwoPi);
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.4f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.65f, Pitch = -0.1f }, Projectile.Center);

                // Smoke ring: 30 evenly-spaced particles with a slight spiral twist (matches FireTeleportBlast).
                for (int i = 0; i < 30; i++)
                {
                    float angle = MathHelper.TwoPi * i / 30f;
                    Vector2 direction = angle.ToRotationVector2();
                    Vector2 tangent = new Vector2(-direction.Y, direction.X) * Main.rand.NextFloat(-0.15f, 0.15f);
                    float speed = Main.rand.NextFloat(9f, 13f);
                    Dust smoke = Dust.NewDustPerfect(
                        Projectile.Center + direction * Main.rand.NextFloat(4f, 18f),
                        DustID.Smoke, (direction + tangent) * speed,
                        65, new Color(145, 115, 92), Main.rand.NextFloat(2.0f, 3.2f));
                    smoke.noGravity = true;
                    smoke.fadeIn = 0.8f;
                }

                // Fire ring: 20 particles staggered between the smoke spokes (matches FireTeleportBlast).
                for (int i = 0; i < 20; i++)
                {
                    float angle = MathHelper.TwoPi * i / 20f + MathHelper.TwoPi / 40f;
                    Vector2 direction = angle.ToRotationVector2();
                    float speed = Main.rand.NextFloat(11f, 15f);
                    Dust fire = Dust.NewDustPerfect(
                        Projectile.Center + direction * Main.rand.NextFloat(3f, 14f),
                        DustID.Torch, direction * speed,
                        50, new Color(255, 155, 45), Main.rand.NextFloat(1.8f, 2.8f));
                    fire.noGravity = true;
                    fire.fadeIn = 0.9f;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int flameDamage = Projectile.damage;
            if (flameDamage < 1)
            {
                flameDamage = 1;
            }

            // Ring 1: damaging expanding ring of FireBreath flames (matches SpawnFireTeleportBurst).
            for (int i = 0; i < FireTeleportFlameCount; i++)
            {
                float flameAngle = rotationOffset + MathHelper.TwoPi * i / FireTeleportFlameCount;
                Vector2 velocity = flameAngle.ToRotationVector2() * FireTeleportFlameSpeed;
                Projectile flame = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, velocity,
                    ModContent.ProjectileType<FireBreath>(), flameDamage, 5f, Main.myPlayer);
                flame.timeLeft = FlameLifetime;
            }

            // Ring 2: slower, anti-phase decorative inner ring of FireBreath flames for volume.
            float decorativeOffset = rotationOffset + MathHelper.Pi / DecorativeFlameCount + 0.37f;
            for (int i = 0; i < DecorativeFlameCount; i++)
            {
                float flameAngle = decorativeOffset + MathHelper.TwoPi * i / DecorativeFlameCount;
                Vector2 outward = flameAngle.ToRotationVector2();
                Projectile flame = Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(),
                    Projectile.Center + outward * 14f, outward * (FireTeleportFlameSpeed * 0.55f),
                    ModContent.ProjectileType<FireBreath>(), 1, 0f, Main.myPlayer, 0f, 1f);
                flame.timeLeft = FlameLifetime + 16;
                flame.hostile = false;
                flame.friendly = false;
                flame.damage = 0;
            }
        }
    }
}
