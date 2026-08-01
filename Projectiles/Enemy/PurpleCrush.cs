using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
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
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 100;
            Projectile.light = .8f;
            DrawOriginOffsetX = 13;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Purple Crush");
        }

        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 44; //killpretendtype
            return true;
        }
        public override void AI()
        {
            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PinkTorch, Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 120, default, 1f);
                Main.dust[dust].noGravity = true;
            }

            Projectile.rotation += 0.9f;
            if (Projectile.velocity.X <= 10 && Projectile.velocity.Y <= 10 && Projectile.velocity.X >= -10 && Projectile.velocity.Y >= -10)
            {
                Projectile.velocity.X *= 1.01f;
                Projectile.velocity.Y *= 1.01f;
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            EnemyVFX.DrawDemonSpiritCrush(Projectile.Center, Projectile.velocity);
            return false;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(22, 18000, false);
            target.AddBuff(30, 600, false);
            //target.AddBuff(23, 180, false); //curse
            target.AddBuff(32, 600, false);
        }
    }
}
