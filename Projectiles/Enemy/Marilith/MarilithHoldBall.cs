using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{
    class MarilithHoldBall : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.height = 16;
            Projectile.penetrate = 4;
            Projectile.tileCollide = false;
            Projectile.width = 16;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 57, 0, 0, 50, Color.Yellow, 2.0f);
            Main.dust[dust].noGravity = true;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(ModContent.BuffType<Buffs.MarilithHold>(), 120, false);
        }

        public override void Kill(int timeLeft)
        {
            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.Center);
                if (Projectile.owner == Main.myPlayer)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height - 3)), new Vector2(3, 0), ModContent.ProjectileType<EnemySpellEffectBuff>(), 8, 3f, Projectile.owner);
                }
                int num41 = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y), Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 3f);
                Main.dust[num41].noGravity = true;
                Main.dust[num41].velocity *= 2f;
                num41 = Dust.NewDust(new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y), Projectile.width, Projectile.height, 15, 0f, 0f, 100, default, 2f);
            }
            Projectile.active = false;
        }
    }
}
