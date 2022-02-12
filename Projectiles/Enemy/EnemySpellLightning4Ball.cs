using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellLightning4Ball : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
        public override void SetDefaults() {
            projectile.width = 16;
            projectile.height = 16;
            projectile.penetrate = 1;
            projectile.hostile = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 250;
            projectile.aiStyle = 0;
        }
        public override void AI() {
            if (projectile.ai[0] != 0)
            {
                projectile.timeLeft = (int)projectile.ai[0];
                projectile.ai[0] = 0;
            }
            if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f) {
                projectile.soundDelay = 10;
                //Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
                Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 9, 0.1f, 0.3f);
            }
            int num47 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 15, 0f, 0f, 100, default, 2f);
            Dust expr_2684 = Main.dust[num47];
            expr_2684.velocity *= 0.3f;
            Main.dust[num47].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].noGravity = true;


            if (projectile.velocity.X != 0f || projectile.velocity.Y != 0f) {
                projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) - 2.355f;
            }

            if (projectile.velocity.Y > 16f) {
                projectile.velocity.Y = 16f;
                return;
            }
        }
        public override void Kill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(projectile.position.X + (float)(projectile.width / 2), projectile.position.Y + (float)(projectile.height / 2), 0, 0, ModContent.ProjectileType<EnemySpellLightning4Bolt>(), (int)(this.projectile.damage), 8f, projectile.owner);
            }
        }
    }
}
