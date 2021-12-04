using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellLightning3Ball : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
        public override void SetDefaults()
        {
            //projectile.aiStyle = 4;
            projectile.hostile = true;
            projectile.height = 16;
            projectile.penetrate = 1;
            projectile.tileCollide = true;
            projectile.width = 16;
            projectile.timeLeft = 600;
        }

        public override void AI()
        {
            if (projectile.ai[0] != 0)
            {
                projectile.timeLeft = (int)projectile.ai[0];
                projectile.ai[0] = 0;
            }
            if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f)
            {
                projectile.soundDelay = 10;
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
            }
            int num47 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, DustID.MagicMirror, 0f, 0f, 100, default, 2f);
            Main.dust[num47].velocity *= 0.3f;
            Main.dust[num47].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].noGravity = true;

            if (projectile.velocity.X != 0f || projectile.velocity.Y != 0f)
            {
                projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) - 2.355f;
            }
        }
        public override void Kill(int timeLeft)
        {

            Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);

            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width / 2), projectile.position.Y + (float)(projectile.height / 2), 0, 0, ModContent.ProjectileType<EnemySpellLightning3Bolt>(), projectile.damage, 8f, projectile.owner);
            Vector2 projectilePos = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
            int num41 = Dust.NewDust(projectilePos, projectile.width, projectile.height, DustID.MagicMirror, 0f, 0f, 100, default, 1f);
            Main.dust[num41].noGravity = true;
            Main.dust[num41].velocity *= 2f;
            Dust.NewDust(projectilePos, projectile.width, projectile.height, DustID.MagicMirror, 0f, 0f, 100, default, 1f);
        }
    }
}
