using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class PoisonCrystalFire : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Poison Crystal Fire");

        }
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.scale = 1;
            Projectile.aiStyle = 8;
            Projectile.timeLeft = 610;
            Projectile.damage = 81;
            Projectile.light = 0.5f;
            Projectile.penetrate = 2;
            //projectile.AIType = 8;
            Projectile.tileCollide = true;
            //projectile.pretendType = 15;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hostile = true;
        }
        public override void AI()
        {
            Projectile.rotation++;


            if (Projectile.velocity.X <= 5 && Projectile.velocity.Y <= 5 && Projectile.velocity.X >= -5 && Projectile.velocity.Y >= -5)
            {
                Projectile.velocity.X *= 1.00f;
                Projectile.velocity.Y *= 1.00f;
            }


            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.Torch, 0, 0, 100, Color.Red, 2.0f);
            Main.dust[dust].noGravity = false;
        }
        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(22, 18000, false); //darkness
            target.AddBuff(30, 1800, false); //bleeding
            target.AddBuff(24, 1600, false); //on fire
            target.AddBuff(21, 600, false); //potion sickness
        }
    }
}