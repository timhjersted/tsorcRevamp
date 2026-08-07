using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Circular purple AOE blast (~150px across) that lingers briefly, then bursts into 6
    // ArtoriasFlameOrb seekers fanning out evenly in every direction.
    class ArtoriasAbyssBlast : ModProjectile
    {
        const float Radius = 75f;
        const int Lifetime = 20;
        const int OrbCount = 6;
        const float OrbSpeed = 4f;
        const int OrbDamage = 40;

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
                for (int i = 0; i < 2; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Unit();
                    Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(8f, Radius * 0.70f);
                    Vector2 velocity = direction * Main.rand.NextFloat(1.6f, 4.2f)
                        + Main.rand.NextVector2Circular(0.5f, 0.5f);
                    int type = Main.rand.NextBool(5) ? DustID.SilverFlame : DustID.ShadowbeamStaff;
                    Dust dust = Dust.NewDustPerfect(position, type, velocity, 90,
                        type == DustID.SilverFlame ? new Color(224, 214, 255) : new Color(132, 46, 210),
                        Main.rand.NextFloat(0.72f, 1.08f));
                    dust.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float fade = MathHelper.Clamp(Projectile.timeLeft / 5f, 0f, 1f);
            ArtoriasVFX.DrawImpactBlast(Projectile.Center, Radius, progress, 0.94f * fade);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return Vector2.Distance(Projectile.Center, targetHitbox.Center.ToVector2()) <= Radius;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 14; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Vector2 velocity = direction * Main.rand.NextFloat(2.5f, 6.5f);
                int type = i % 5 == 0 ? DustID.SilverFlame : DustID.ShadowbeamStaff;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + direction * Main.rand.NextFloat(4f, 32f),
                    type, velocity, 70,
                    type == DustID.SilverFlame ? new Color(230, 220, 255) : new Color(139, 48, 220),
                    Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < OrbCount; i++)
            {
                Vector2 vel = (MathHelper.TwoPi * i / OrbCount).ToRotationVector2() * OrbSpeed;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, vel,
                    ModContent.ProjectileType<ArtoriasFlameOrb>(), OrbDamage, 0f, Main.myPlayer);
            }
        }
    }
}
