using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy {
    class ShadowShot : ModProjectile {

        public override void SetDefaults() {
            projectile.hostile = true;
            projectile.height = projectile.width = 15;
            projectile.tileCollide = false;
            projectile.aiStyle = 0;
        }

        public override bool PreKill(int timeLeft) {
            projectile.type = 44;
            return true;
        }

        public override void AI() {
            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 52, 0, 0, 100, default, 2.0f);
            Main.dust[dust].noGravity = true;

            if (projectile.velocity.X <= 10 && projectile.velocity.Y <= 10 && projectile.velocity.X >= -10 && projectile.velocity.Y >= -10) {
                projectile.velocity.X *= 1.01f;
                projectile.velocity.Y *= 1.01f;
            }

        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            target.AddBuff(BuffID.Bleeding, 600);
            target.AddBuff(BuffID.Poisoned, 300);
            target.AddBuff(BuffID.PotionSickness, 1200); // 20s of potion sick? that is *vile* why would you do that
            target.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 600); //no kb resist
        }
    }
}
