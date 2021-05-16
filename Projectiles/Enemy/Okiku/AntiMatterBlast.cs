using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class AntiMatterBlast : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 55;
            projectile.height = 55;
            projectile.scale = 2.3f;
            projectile.aiStyle = 9;
            projectile.hostile = true;
            projectile.damage = 80;
            projectile.penetrate = 2;
            projectile.tileCollide = false;
            projectile.ranged = true;
        }
        public override bool PreKill(int timeLeft) {
            projectile.type = 79; //killpretendtype
            return true;
        }
        public override bool PreAI() {
            projectile.rotation++;
            int dust = Dust.NewDust(new Vector2((float)projectile.position.X + 10, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 200, Color.Red, 1f);
            Main.dust[dust].noGravity = true;

            if (projectile.velocity.X <= 10 && projectile.velocity.Y <= 10 && projectile.velocity.X >= -10 && projectile.velocity.Y >= -10) {
                projectile.velocity.X *= 1.01f;
                projectile.velocity.Y *= 1.01f;
            }
            return true;
        }

        public override void OnHitPlayer(Player target, int damage, bool crit) {
            target.AddBuff(BuffID.Confused, 300, false);
            target.AddBuff(BuffID.Gravitation, 300, false);
            target.AddBuff(BuffID.Slow, 300, false);
        }

    }
}
