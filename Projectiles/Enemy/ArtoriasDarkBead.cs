using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class ArtoriasDarkBead : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 8;
            Projectile.timeLeft = 300;//was 600
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.tileCollide = true;
            Projectile.penetrate = 3;
            Projectile.width = 16;
            AIType = 27;
            Main.projFrames[Projectile.type] = 1;
            Projectile.ignoreWater = true;
        }


        public override void AI()
        {
            Projectile.rotation += 4f;
            if (Main.rand.NextBool(2)) // 
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 27, 0, 0, 50, Color.Purple, 2.0f);
                Main.dust[dust].noGravity = false;

                int pink2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.CrystalSerpent, Projectile.velocity.X, Projectile.velocity.Y, Scale: 0.6f);

                Main.dust[pink2].noGravity = true;
            }
            Lighting.AddLight((int)(Projectile.position.X / 16f), (int)(Projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

            if (Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4)
            {
                float accel = 0.1f + (Main.rand.Next(1, 2) * 0.5f); //was 10, 30
                Projectile.velocity.X *= accel;
                Projectile.velocity.Y *= accel;
            }
        }
    }
}