using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>Delayed fire brand placed around a player by Soul of Cinder's staff rituals.
    /// ai[1]: 0 constellation, 1 cross, 2 orbit.</summary>
    class CinderRemoteFlame : ModProjectile
    {
        const int HoldTicks = 30;
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.CultistBossFireBall;

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.light = 0.8f;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > HoldTicks ? null : false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] == 1)
                Projectile.localAI[1] = Projectile.rotation;

            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxPlayers || !Main.player[targetIndex].active)
            {
                Projectile.Kill();
                return;
            }

            int mode = (int)Projectile.ai[1];
            float baseAngle = Projectile.localAI[1];
            if (Projectile.localAI[0] <= HoldTicks)
            {
                Projectile.velocity *= 0f;
                Projectile.scale = 0.55f + Projectile.localAI[0] / HoldTicks * 0.45f;
                ChargeVFX(mode, baseAngle, targetIndex);
            }
            else if (Projectile.localAI[0] == HoldTicks + 1)
            {
                Projectile.velocity = (Main.player[targetIndex].Center - Projectile.Center).SafeNormalize(Vector2.UnitY)
                    * (mode == 1 ? 12f : mode == 2 ? 11f : 10f);
                Projectile.tileCollide = true;
                ReleaseVFX(mode, baseAngle);
                SoundEngine.PlaySound(SoundID.Item74 with
                {
                    Volume = 0.35f,
                    Pitch = mode == 0 ? 0.3f : mode == 1 ? -0.15f : 0.1f
                }, Projectile.Center);
            }

            if (Projectile.localAI[0] > HoldTicks && Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.OrangeTorch, Main.rand.NextVector2Circular(0.5f, 0.5f),
                    90, Color.OrangeRed, 0.8f);
                dust.noGravity = true;
            }

            Color lightColor = mode == 1 ? new Color(1f, 0.16f, 0.04f) : mode == 2 ? new Color(1f, 0.42f, 0.08f) : new Color(1f, 0.68f, 0.22f);
            Lighting.AddLight(Projectile.Center, lightColor.ToVector3() * Projectile.scale * 0.4f);
        }

        void ChargeVFX(int mode, float baseAngle, int targetIndex)
        {
            float charge = Projectile.localAI[0] / HoldTicks;
            if (mode == 0) // Four diagonal brands: inward-curling gold sparks and late white glints.
            {
                Projectile.rotation = baseAngle + Projectile.localAI[0] * 0.15f;
                if (((int)Projectile.localAI[0]) % 2 == 0)
                {
                    float spiral = baseAngle + Projectile.localAI[0] * 0.24f;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center + spiral.ToRotationVector2() * (4f + charge * 5f), DustID.OrangeTorch,
                        -spiral.ToRotationVector2() * (0.8f + charge), 80, Color.Orange, 0.55f + charge * 0.3f);
                    dust.noGravity = true;
                }
                if (charge > 0.72f && ((int)Projectile.localAI[0]) % 4 == 0)
                {
                    Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, Main.rand.NextVector2Circular(0.6f, 0.6f),
                        120, Color.LightGoldenrodYellow, 0.5f);
                    glint.noGravity = true;
                }
            }
            else if (mode == 1) // Cardinal brands: red-orange sparks draw a compact cross through each mark.
            {
                Projectile.rotation = baseAngle;
                if (((int)Projectile.localAI[0]) % 2 == 0)
                {
                    Vector2 axis = baseAngle.ToRotationVector2();
                    Vector2 crossAxis = axis.RotatedBy(MathHelper.PiOver2);
                    bool perpendicular = ((int)Projectile.localAI[0] / 2) % 2 == 0;
                    Vector2 direction = perpendicular ? crossAxis : axis;
                    Dust dust = Dust.NewDustPerfect(Projectile.Center - direction * 7f, perpendicular ? DustID.RedTorch : DustID.OrangeTorch,
                        direction * (1.35f + charge * 1.3f), 80, Color.OrangeRed, 0.55f + charge * 0.3f);
                    dust.noGravity = true;
                }
            }
            else // Ashen Orbit: an increasingly tight, clockwise trail around the player.
            {
                float angle = baseAngle + Projectile.localAI[0] * 0.055f;
                Projectile.Center = Main.player[targetIndex].Center + angle.ToRotationVector2()
                    * MathHelper.Lerp(220f, 88f, charge);
                Projectile.rotation = angle + MathHelper.PiOver2;
                if (Main.rand.NextBool(2))
                {
                    Vector2 tangent = (angle + MathHelper.PiOver2).ToRotationVector2();
                    Dust dust = Dust.NewDustPerfect(Projectile.Center - tangent * 3f, DustID.OrangeTorch, tangent * (1.1f + charge),
                        80, Color.Orange, 0.52f + charge * 0.28f);
                    dust.noGravity = true;
                }
                if (charge > 0.7f && ((int)Projectile.localAI[0]) % 3 == 0)
                {
                    Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, Main.rand.NextVector2Circular(0.55f, 0.55f),
                        110, Color.LightGoldenrodYellow, 0.46f);
                    glint.noGravity = true;
                }
            }
        }

        void ReleaseVFX(int mode, float baseAngle)
        {
            int sparks = mode == 2 ? 6 : 4;
            for (int i = 0; i < sparks; i++)
            {
                float angle = mode == 1
                    ? baseAngle + (i - (sparks - 1) * 0.5f) * 0.22f
                    : MathHelper.TwoPi * i / sparks + baseAngle;
                Vector2 velocity = angle.ToRotationVector2() * (1.6f + Main.rand.NextFloat(1.2f));
                int dustType = mode == 1 && i % 2 == 0 ? DustID.RedTorch : DustID.OrangeTorch;
                Dust dust = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 70, Color.OrangeRed, 0.75f);
                dust.noGravity = true;
            }

            if (mode == 0 || mode == 2)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, Main.rand.NextVector2Circular(2.2f, 2.2f),
                        90, Color.LightGoldenrodYellow, 0.65f);
                    glint.noGravity = true;
                }
            }
        }
    }
}
