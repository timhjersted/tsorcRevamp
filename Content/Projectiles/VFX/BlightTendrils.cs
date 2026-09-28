using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.VFX
{
    // Dust-only proc visual. The vanilla projectile texture is hidden; no custom art is needed.
    public class BlightTendrils : ModProjectile
    {
        private const int LifetimeTicks = Blight.DurationTicks;
        private const int FadeTicks = 54;
        private static readonly int[] LengthsInTiles = { 3, 9, 5, 8, 4, 7, 6, 9 };

        public override string Texture => "Terraria/Images/Projectile_0";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            if (Main.dedServ) return;

            int age = LifetimeTicks - Projectile.timeLeft;
            Vector2 center = Projectile.Center;
            if (age == 0)
            {
                for (int i = 0; i < 40; i++)
                {
                    Dust burst = Dust.NewDustPerfect(center, DustID.Wraith,
                        Main.rand.NextVector2Circular(4f, 4f), 95,
                        new Color(75, 65, 90), Main.rand.NextFloat(0.65f, 1.35f));
                    burst.noGravity = true;
                }
            }

            float expansion = MathHelper.Clamp((age + 1) / 18f, 0f, 1f);
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            for (int arm = 0; arm < LengthsInTiles.Length; arm++)
            {
                float angle = MathHelper.TwoPi * arm / LengthsInTiles.Length + 0.12f * (float)Math.Sin(age * 0.08f + arm);
                Vector2 direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
                Vector2 normal = new Vector2(-direction.Y, direction.X);
                float length = LengthsInTiles[arm] * 16f * expansion;
                int segments = Math.Max(1, (int)Math.Ceiling(length / 7f));
                for (int segment = 0; segment <= segments; segment++)
                {
                    if ((segment + age + arm) % 2 != 0) continue;
                    float along = segment / (float)segments;
                    float sway = (float)Math.Sin(age * 0.12f - along * 5f + arm * 1.7f) * (3f + 11f * along) * along;
                    Vector2 position = center + direction * (length * along) + normal * sway;
                    Vector2 velocity = direction * 0.12f + normal * (float)Math.Cos(age * 0.12f - along * 5f + arm * 1.7f) * 0.18f;
                    Dust tendril = Dust.NewDustPerfect(position, DustID.Wraith, velocity, 125,
                        new Color(65, 55, 80), MathHelper.Lerp(1.2f, 0.7f, along) * fade);
                    tendril.noGravity = true;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                Dust body = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(12f, 20f),
                    DustID.Wraith, Main.rand.NextVector2Circular(0.3f, 0.3f), 125,
                    new Color(65, 55, 80), Main.rand.NextFloat(0.65f, 1.05f) * fade);
                body.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < 24; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(24f, 24f),
                    DustID.Wraith, Main.rand.NextVector2Circular(0.8f, 0.8f), 175,
                    new Color(65, 55, 80), Main.rand.NextFloat(0.45f, 0.9f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
