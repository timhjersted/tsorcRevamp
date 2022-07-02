using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.SummonProjectiles
{
    public class NullSpriteBeam : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Null Beam");
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.extraUpdates = 100;
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.localAI[0] += 1f;
            if (Projectile.localAI[0] > 5f)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 projectilepos = Projectile.position;
                    projectilepos -= Projectile.velocity * (i * 0.5f);
                    Projectile.alpha = 255;
                    int num448 = Dust.NewDust(projectilepos, 1, 1, 70);
                    Main.dust[num448].noGravity = true;
                    Main.dust[num448].position = projectilepos;
                    Main.dust[num448].scale = (float)Main.rand.Next(70, 110) * 0.013f;
                    Main.dust[num448].velocity *= 0.2f;
                }
            }
        }
    }
}
