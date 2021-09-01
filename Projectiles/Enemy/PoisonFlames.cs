using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class PoisonFlames : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";
        public override void SetDefaults() {
            projectile.width = 16;
            projectile.height = 16;
            projectile.alpha = 150;
            projectile.aiStyle = 8;
            projectile.timeLeft = 200;
            projectile.damage = 46;
            projectile.light = 0.8f;
            projectile.penetrate = 1;
            projectile.tileCollide = false;
            projectile.hostile = true;
            aiType = 96; //pretendtype
        }
        public override bool PreAI() {
            projectile.rotation += 1f;

            int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 75, 0, 0, 50, Color.Chartreuse, 3.0f);
            Main.dust[dust].noGravity = true;

            if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4) {
                float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
                projectile.velocity.X *= accel;
                projectile.velocity.Y *= accel;
            }
            return true;
        }
        public override void OnHitPlayer(Player target, int damage, bool crit) {
            target.AddBuff(BuffID.Poisoned, 2400);
            target.AddBuff(BuffID.Bleeding, 2400);
        }
    }
}
