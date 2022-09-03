using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon
{
    public class FriendlyTetsujinLaser : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.height = 14;
            Projectile.penetrate = 1;
            Projectile.scale = 1.5f;
            Projectile.tileCollide = false;
            Projectile.width = 2;
            Projectile.usesLocalNPCImmunity = true;
        }
        public override void AI()
        {
            Projectile.rotation = (float)Math.Atan2((double)Projectile.velocity.Y, (double)Projectile.velocity.X) + 1.57f;
            //Color color = new Color();
            //int dust = Dust.NewDust(new Vector2((float) projectile.position.X, (float) projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 20, color, 1.0f);
            //Main.dust[dust].noGravity = true;
            float red = 1.0f;
            float green = 0.0f;
            float blue = 1.0f;

            Lighting.AddLight((int)((Projectile.position.X + (float)(Projectile.width / 2)) / 16f), (int)((Projectile.position.Y + (float)(Projectile.height / 2)) / 16f), red, green, blue);
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 15;
            return base.PreKill(timeLeft);
        }
    }
}