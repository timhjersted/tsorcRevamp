using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellHoldBall : ModProjectile {

        public override void SetDefaults() {
            projectile.hostile = true;
            projectile.height = 16;
            projectile.penetrate = 4;
            projectile.tileCollide = true;
            projectile.width = 16;
        }

        public override void AI() {
            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 57, 0, 0, 50, Color.Yellow, 2.0f);
            Main.dust[dust].noGravity = true;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            target.AddBuff(ModContent.BuffType<Buffs.Hold>(), 120, false);
        }

        public override void Kill(int timeLeft) {
            if (!projectile.active) {
                return;
            }
            projectile.timeLeft = 0;
            {
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
                if (projectile.owner == Main.myPlayer) {
                    Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width / 2), projectile.position.Y + (float)(projectile.height - 3)), new Vector2(3, 0), ModContent.ProjectileType<EnemySpellEffectBuff>(), 8, 3f, projectile.owner);
                }
                int num41 = Dust.NewDust(new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y), projectile.width, projectile.height, 15, 0f, 0f, 100, default, 3f);
                Main.dust[num41].noGravity = true;
                Main.dust[num41].velocity *= 2f;
                num41 = Dust.NewDust(new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y), projectile.width, projectile.height, 15, 0f, 0f, 100, default, 2f);
            }
            projectile.active = false;
        }
    }
}
