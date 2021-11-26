using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    class ExplosionBall : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/GreatFireballBall";

        public override void SetDefaults() {
            projectile.aiStyle = 9; 
            projectile.friendly = true;
            projectile.height = 16;
            projectile.width = 16;
            projectile.light = 0.8f;
            projectile.magic = true;
            projectile.penetrate = 1;
            projectile.tileCollide = true;
        }
        public override void AI()
        {
            if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f)
            {
                projectile.soundDelay = 10;
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
            }

            int thisDust = Dust.NewDust(new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y), projectile.width, projectile.height, 15, 0, 0, 100, default, 2f);
            Main.dust[thisDust].noGravity = true;

            projectile.rotation += 0.3f;
        }
        public override void Kill(int timeLeft)
        {

            Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);

            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width), projectile.position.Y + (float)(projectile.height), 0, 0, ModContent.ProjectileType<Explosion>(), projectile.damage, 8f, projectile.owner);
            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * 4), projectile.position.Y + (float)(projectile.height), 0, 0, ModContent.ProjectileType<Explosion>(), projectile.damage, 8f, projectile.owner);
            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * -2), projectile.position.Y + (float)(projectile.height), 0, 0, ModContent.ProjectileType<Explosion>(), projectile.damage, 8f, projectile.owner);
            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width), projectile.position.Y + (float)(projectile.height * -2), 0, 0, ModContent.ProjectileType<Explosion>(), projectile.damage, 8f, projectile.owner);
            Projectile.NewProjectile(projectile.position.X + (float)(projectile.width), projectile.position.Y + (float)(projectile.height * 4), 0, 0, ModContent.ProjectileType<Explosion>(), projectile.damage, 8f, projectile.owner);

            for (int i = 0; i < 20; i++)
            {
                int thisDust = Dust.NewDust(new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y), projectile.width, projectile.height, 15, 0, 0, 100, default, 2f);
                Main.dust[thisDust].noGravity = true;
                thisDust = Dust.NewDust(new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y), projectile.width, projectile.height, 15, 0, 0, 100, default, 2f);
                Main.dust[thisDust].noGravity = true;
            }
        }
    }
}
