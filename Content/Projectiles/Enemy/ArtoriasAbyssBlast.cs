using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // Circular abyss blast (450px across) at Flip Slash's landing point. It used to burst into ArtoriasFlameOrb seekers;
    // the Flip Slash landing now fires Homing Volley orbs itself, so this is the AOE only.
    class ArtoriasAbyssBlast : ModProjectile
    {
        internal const float Radius = 225f;
        const int Lifetime = 20;
        // The damage circle is 90% of the drawn radius: the outer 10% is the smoke body's soft falloff.
        const float HitRadiusFraction = 0.9f;
        // PreDraw fades over the last FadeTicks; damage ends when the fade starts.
        const int FadeTicks = 5;

        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.timeLeft = Lifetime;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Vector3(0.54f, 0.14f, 0.72f));

            if (!Main.dedServ && Projectile.timeLeft % 2 == 0)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Unit();
                    Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(24f, Radius * 0.70f);
                    Vector2 velocity = direction * Main.rand.NextFloat(3f, 8f)
                        + Main.rand.NextVector2Circular(0.5f, 0.5f);
                    int type = Main.rand.NextBool(5) ? DustID.SilverFlame : DustID.ShadowbeamStaff;
                    Dust dust = Dust.NewDustPerfect(position, type, velocity, 90,
                        type == DustID.SilverFlame ? new Color(224, 214, 255) : new Color(132, 46, 210),
                        Main.rand.NextFloat(0.9f, 1.3f));
                    dust.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            ArtoriasVFX.DrawImpactBlast(Projectile.Center, Radius, progress, 0.94f * fade);
            return false;
        }

        public override bool? CanDamage() => Projectile.timeLeft > FadeTicks;

        // Nearest point of the player's hitbox to the centre, so a player whose edge is inside the visible blast is hit.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float nearestX = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
            float nearestY = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
            float hitRadius = Radius * HitRadiusFraction;
            return Vector2.DistanceSquared(Projectile.Center, new Vector2(nearestX, nearestY)) <= hitRadius * hitRadius;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 42; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Vector2 velocity = direction * Main.rand.NextFloat(4f, 11f);
                int type = i % 5 == 0 ? DustID.SilverFlame : DustID.ShadowbeamStaff;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + direction * Main.rand.NextFloat(12f, 96f),
                    type, velocity, 70,
                    type == DustID.SilverFlame ? new Color(230, 220, 255) : new Color(139, 48, 220),
                    Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }
    }
}
