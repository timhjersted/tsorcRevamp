using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    public class ArtoriasAbyssShockwave : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 16;
            Projectile.alpha = 255;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                if (Main.myPlayer == Projectile.owner)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Projectile.owner, 160, 18);
                }
            }

            float progress = 1f - Projectile.timeLeft / 16f;
            int size = (int)MathHelper.Lerp(42f, 170f, progress);
            Vector2 center = Projectile.Center;
            Projectile.Resize(size, size);
            Projectile.Center = center;
            Projectile.damage = (int)(Projectile.damage * 0.94f);
            Lighting.AddLight(Projectile.Center, 0.38f, 0.08f, 0.55f);

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 6f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + velocity * Main.rand.NextFloat(5f, size / 2f), DustID.PurpleTorch, velocity, 120, Color.MediumPurple, Main.rand.NextFloat(1f, 1.8f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}