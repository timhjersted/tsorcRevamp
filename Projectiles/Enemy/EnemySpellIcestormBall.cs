using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellIcestormBall : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = 23;
            projectile.hostile = true;
            projectile.height = 16;
            projectile.penetrate = 8;
            projectile.tileCollide = true;
            projectile.width = 16;
            projectile.timeLeft = 0;
        }

        public override void Kill(int timeLeft) {
            if (!projectile.active) {
                return;
            }
            projectile.timeLeft = 0;
            {
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);

                if (projectile.position.X + (float)(projectile.width / 2) > Main.player[projectile.owner].position.X + (float)(Main.player[projectile.owner].width / 2)) {
                    if (Main.rand.Next(5) == 1) {
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2((Main.rand.Next(30) * -1), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle1>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2((Main.rand.Next(30) * -1), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle2>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2((Main.rand.Next(30) * -1), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle3>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2((Main.rand.Next(30) * -1), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle4>(), 50, 3f, projectile.owner);

                    }
                    if (Main.rand.Next(7) == 1) {
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2((Main.rand.Next(30) * -1), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle1>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2((Main.rand.Next(30) * -1), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle2>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2((Main.rand.Next(30) * -1), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle3>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2((Main.rand.Next(30) * -1), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle4>(), 50, 3f, projectile.owner);
                    }

                }
                else {
                    if (Main.rand.Next(5) == 1) {
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2(Main.rand.Next(30), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle1>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2(Main.rand.Next(30), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle2>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2(Main.rand.Next(30), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle3>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2(Main.rand.Next(30), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle4>(), 50, 3f, projectile.owner);

                    }
                    if (Main.rand.Next(7) == 1) {
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2(Main.rand.Next(30), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle1>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2(Main.rand.Next(30), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle2>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2(Main.rand.Next(30), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle3>(), 50, 3f, projectile.owner);
                        if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(new Vector2(projectile.position.X + (float)(projectile.width * (Main.rand.Next(50))), projectile.position.Y + (float)(projectile.height * (Main.rand.Next(60) - 2))), new Vector2(Main.rand.Next(30), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle4>(), 50, 3f, projectile.owner);

                    }

                    Vector2 projectilePos = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
                    int num41 = Dust.NewDust(projectilePos, projectile.width, projectile.height, 15, 0f, 0f, 100, default, 2f);
                    Main.dust[num41].noGravity = true;
                    Main.dust[num41].velocity *= 2f;
                    Dust.NewDust(projectilePos, projectile.width, projectile.height, 15, 0f, 0f, 100, default, 1f);
                }
            }
            projectile.active = false;
        }

    }
}
