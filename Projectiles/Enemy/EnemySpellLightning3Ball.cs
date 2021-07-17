using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellLightning3Ball : ModProjectile {
		public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
		public override void SetDefaults() {
            //projectile.aiStyle = 4;
            projectile.hostile = true;
            projectile.height = 16;
            projectile.penetrate = 1;
            projectile.tileCollide = true;
            projectile.width = 16;
        }

        public override void AI()
        {
            if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f)
            {
                projectile.soundDelay = 10;
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
            }
            int num47 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, DustID.MagicMirror, 0f, 0f, 100, default, 2f);
            Dust expr_2684 = Main.dust[num47];
            expr_2684.velocity *= 0.3f;
            Main.dust[num47].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].noGravity = true;
            if (Main.myPlayer == projectile.owner && projectile.ai[0] == 0f)
            {
                if (Main.player[projectile.owner].channel)
                {
                    float num48 = 12f;
                    Vector2 vector6 = new Vector2(projectile.position.X + (float)projectile.width * 0.5f, projectile.position.Y + (float)projectile.height * 0.5f);
                    float num49 = (float)Main.mouseX + Main.screenPosition.X - vector6.X;
                    float num50 = (float)Main.mouseY + Main.screenPosition.Y - vector6.Y;
                    float num51;
                    num51 = (float)Math.Sqrt((double)(num49 * num49 + num50 * num50));
                    if (num51 > num48)
                    {
                        num51 = num48 / num51;
                        num49 *= num51;
                        num50 *= num51;
                        int num52 = (int)(num49 * 1000f);
                        int num53 = (int)(projectile.velocity.X * 1000f);
                        int num54 = (int)(num50 * 1000f);
                        int num55 = (int)(projectile.velocity.Y * 1000f);
                        if (num52 != num53 || num54 != num55)
                        {
                            projectile.netUpdate = true;
                        }
                        projectile.velocity.X = num49;
                        projectile.velocity.Y = num50;
                    }
                    else
                    {
                        int num56 = (int)(num49 * 1000f);
                        int num57 = (int)(projectile.velocity.X * 1000f);
                        int num58 = (int)(num50 * 1000f);
                        int num59 = (int)(projectile.velocity.Y * 1000f);
                        if (num56 != num57 || num58 != num59)
                        {
                            projectile.netUpdate = true;
                        }
                        projectile.velocity.X = num49;
                        projectile.velocity.Y = num50;
                    }
                }
            }


            if (projectile.velocity.X != 0f || projectile.velocity.Y != 0f)
            {
                projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) - 2.355f;
            }

            if (projectile.velocity.Y > 16f)
            {
                projectile.velocity.Y = 16f;
                return;
            }
        }
        public override void Kill(int timeLeft) {
			if (!projectile.active) {
				return;
			}
			projectile.timeLeft = 0;
			
				Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
				for (int num40 = 0; num40 < 20; num40++) {
					Projectile.NewProjectile(projectile.position.X + (float)(projectile.width / 2), projectile.position.Y + (float)(projectile.height / 2), 0, 0, ModContent.ProjectileType<EnemySpellLightning3Bolt>(), 70, 8f, projectile.owner);
					Vector2 projectilePos = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
					int num41 = Dust.NewDust(projectilePos, projectile.width, projectile.height, DustID.MagicMirror, 0f, 0f, 100, default, 1f);
					Main.dust[num41].noGravity = true;
					Main.dust[num41].velocity *= 2f;
					Dust.NewDust(projectilePos, projectile.width, projectile.height, DustID.MagicMirror, 0f, 0f, 100, default, 1f);
				}
			
			if (projectile.owner == Main.myPlayer) {
				if (Main.netMode != NetmodeID.SinglePlayer) {
					NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, projectile.identity, (float)projectile.owner, 0f, 0f, 0);
				}
			}
			projectile.active = false;
		}

	}
}
