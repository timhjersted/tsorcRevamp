using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class MeteorShower : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 26;
            projectile.height = 26;
            projectile.aiStyle = 0;
            projectile.friendly = true;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.timeLeft = 120;
        }

        public override void AI() {
            Color color = new Color();
            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, projectile.velocity.X, projectile.velocity.Y, 200, color, 3f);
            Main.dust[dust].noGravity = true;
            projectile.rotation += 0.1f;
            Lighting.AddLight((int)((projectile.position.X + (float)(projectile.width / 2)) / 16f), (int)((projectile.position.Y + (float)(projectile.height / 2)) / 16f), 1f, 1f, 1f);
        }
    }
}
