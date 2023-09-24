using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Meteor : ModProjectile
    {

        public override void SetDefaults()
        {
            //projectile.aiStyle = 9;
            Projectile.friendly = true;
            Projectile.height = 48;
            Projectile.width = 48;
            Projectile.light = 0.8f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false; //strictly worse than unupgraded variant otherwise
            Projectile.timeLeft = 200;
        }

        public override void AI()
        {
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 127, Projectile.velocity.X / 2, Projectile.velocity.Y / 2, 160, default, 3f);
            Main.dust[dust].noGravity = true;
            dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 130, Projectile.velocity.X / 2, Projectile.velocity.Y / 2, 220, default, 1f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(58, Main.LocalPlayer);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, 127, Main.rand.Next(-10, 10) + Projectile.velocity.X, Main.rand.Next(-10, 10) + Projectile.velocity.Y, 160, default, 3f);
                dust = Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, 127, Main.rand.Next(-10, 10) + Projectile.velocity.X, Main.rand.Next(-10, 10) + Projectile.velocity.Y, 160, default, 3f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, 130, Main.rand.Next(-10, 10) + Projectile.velocity.X, Main.rand.Next(-10, 10) + Projectile.velocity.Y, 160, default, 1.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(58, Main.LocalPlayer);
            }

            Projectile.penetrate = 20;
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.damage /= 2;
            Projectile.Damage();
        }
    }
}


