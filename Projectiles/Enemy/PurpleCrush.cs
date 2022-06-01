using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class PurpleCrush : ModProjectile
    {
        public override void SetDefaults()
        {
            //projectile.aiStyle = 24;
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.damage = 25;
            Projectile.width = 16;
            //projectile.aiPretendType = 94;
            Projectile.timeLeft = 100;
            Projectile.light = .8f;
            drawOriginOffsetX = 13;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Purple Crush");
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 44; //killpretendtype
            return true;
        }
        public override void AI()
        {
            Projectile.rotation += 0.9f;
            if (Projectile.velocity.X <= 10 && Projectile.velocity.Y <= 10 && Projectile.velocity.X >= -10 && Projectile.velocity.Y >= -10)
            {
                Projectile.velocity.X *= 1.01f;
                Projectile.velocity.Y *= 1.01f;
            }
        }
        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(22, 18000, false);
            target.AddBuff(30, 600, false);
            //target.AddBuff(23, 180, false); //curse
            target.AddBuff(32, 600, false);
            base.OnHitPlayer(target, damage, crit);
        }
    }
}