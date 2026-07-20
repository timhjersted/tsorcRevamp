using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemyMeteorStormMeteor : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Meteor";

        public override void SetDefaults()
        {
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.height = 48;
            Projectile.width = 48;
            Projectile.light = 0.85f;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 200;
            Projectile.extraUpdates = 1;
            Projectile.scale = 1.2f;
        }

        public override void AI()
        {
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 293, Projectile.velocity.X / 2, Projectile.velocity.Y / 2, 160, default, 3.2f);
            Main.dust[dust].noGravity = true;
            dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 130, Projectile.velocity.X / 2, Projectile.velocity.Y / 2, 220, default, 1.05f);
            Main.dust[dust].noGravity = true;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 30; i++)
            {
                int dust = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.width / 2f, Projectile.position.Y - Projectile.height / 2f), Projectile.width, Projectile.height, 293, Main.rand.Next(-10, 10) + Projectile.velocity.X, Main.rand.Next(-10, 10) + Projectile.velocity.Y, 160, default, 3f);
                Main.dust[dust].noGravity = true;
                dust = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.width / 2f, Projectile.position.Y - Projectile.height / 2f), Projectile.width, Projectile.height, 130, Main.rand.Next(-10, 10) + Projectile.velocity.X, Main.rand.Next(-10, 10) + Projectile.velocity.Y, 160, default, 1.5f);
                Main.dust[dust].noGravity = true;
            }

            Projectile.penetrate = 20;
            Vector2 oldCenter = Projectile.Center;
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.position = oldCenter - new Vector2(Projectile.width / 2f, Projectile.height / 2f);
            Projectile.damage /= 2;
            Projectile.Damage();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }
    }
}
