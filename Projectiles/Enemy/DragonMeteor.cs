using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class DragonMeteor : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = 1;
            aiType = 20;
            projectile.width = 59;
            projectile.height = 62;
            projectile.hide = false;
            projectile.magic = true;
            projectile.timeLeft = 600;
            projectile.light = 1;
            projectile.tileCollide = true;
            projectile.friendly = false;
            projectile.hostile = true;
        }

        public override bool PreKill(int timeLeft) {
            projectile.type = 30;
            return true;
        }

        public override bool PreAI() {
            int D = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, projectile.velocity.X, projectile.velocity.Y, 200, new Color(), 3f);
            Main.dust[D].noGravity = true;
            projectile.rotation += 0.1f;
            Lighting.AddLight((int)((projectile.position.X + (float)(projectile.width / 2)) / 16f), (int)((projectile.position.Y + (float)(projectile.height / 2)) / 16f), 1f, 1f, 1f);
            return true;
        }
    }
}
