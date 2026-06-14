using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.Enemy
{
    class AbyssFlames : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.timeLeft = 600;
            Projectile.damage = 46;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.9f;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.hostile = true;
            Projectile.alpha = 125;
        }
        public override bool PreAI()
        {
            if (Projectile.ai[0] == 1)
            {
                Projectile.timeLeft = 120;
            }

            Projectile.velocity = Projectile.velocity.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-0.5f, 0.5f)));

            Projectile.rotation += 1f;

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.PinkTorch, 0, 0, 50, default, 2.6f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 223, 0, 0, 50, default, 2.5f);
                Main.dust[dust].noGravity = true;
            }

            if (Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4)
            {
                float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
                Projectile.velocity.X *= accel;
                Projectile.velocity.Y *= accel;
            }
            return true;
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<AbyssInferno>(), 4 * 60, false);
        }
    }
}
