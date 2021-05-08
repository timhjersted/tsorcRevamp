using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class FireBall : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 12;
            projectile.height = 12;
            projectile.timeLeft = 3600;
            projectile.friendly = true;
            projectile.tileCollide = true;
            projectile.magic = true;
            projectile.aiStyle = 0;
        }
        public override void AI() {

            Color color = new Color();
            for (int d = 0; d < 2; d++) {
                int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 100, color, 1.25f);
                Main.dust[dust].noGravity = true;
            }
            if (projectile.wet) {
                projectile.Kill();
            }
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit) {
            if (Main.rand.Next(4) == 0) {
                target.AddBuff(BuffID.OnFire, 300);
            }
        }
        public override void Kill(int timeLeft)
        {
            for (int d = 0; d < 15; d++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 174, projectile.velocity.X, projectile.velocity.Y, 0, default(Color), 1f);
                Main.dust[dust].noGravity = true;
            }
            Main.PlaySound(SoundID.NPCHit3.WithVolume(.45f), projectile.position);
        }
    }
}
