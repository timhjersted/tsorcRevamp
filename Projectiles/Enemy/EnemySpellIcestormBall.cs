using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellIcestormBall : ModProjectile {
        public override string Texture => "tsorcRevamp/Projectiles/Ice1Ball";
        public override void SetDefaults() {
            projectile.hostile = true;
            projectile.height = 16;
            projectile.width = 16;
            projectile.tileCollide = true;
            projectile.aiStyle = 1;
        }

        public override void AI()
        {
            if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f)
            {
                projectile.soundDelay = 10;
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
            }
            int thisDust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 15, 0, 0, 100, default, 2f);
            Main.dust[thisDust].noGravity = true;

            projectile.rotation += 0.25f;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Enemy Spell Ice Storm");

        }

        public override void Kill(int timeLeft) {

            for(int i = 0; i < 20; i++)
            {
                int thisDust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 15, 0, 0, 100, default, 2f);
                Main.dust[thisDust].noGravity = true;
                Main.dust[thisDust].velocity.X += Main.rand.Next(-15, 15);
            }
            Vector2 positionOffset = new Vector2(1000, 0);
            Projectile.NewProjectile(projectile.position + positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(-9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle1>(), projectile.damage, 3f, projectile.owner);
            Projectile.NewProjectile(projectile.position + positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(-9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle2>(), projectile.damage, 3f, projectile.owner);
            Projectile.NewProjectile(projectile.position + positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(-9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle3>(), projectile.damage, 3f, projectile.owner);
            Projectile.NewProjectile(projectile.position + positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(-9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle4>(), projectile.damage, 3f, projectile.owner);

            Projectile.NewProjectile(projectile.position - positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle1>(), projectile.damage, 3f, projectile.owner);
            Projectile.NewProjectile(projectile.position - positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle2>(), projectile.damage, 3f, projectile.owner);
            Projectile.NewProjectile(projectile.position - positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle3>(), projectile.damage, 3f, projectile.owner);
            Projectile.NewProjectile(projectile.position - positionOffset + Main.rand.NextVector2Circular(32, 32), new Vector2(9 + (Main.rand.NextFloat(-1, 1)), 0), ModContent.ProjectileType<EnemySpellIcestormIcicle4>(), projectile.damage, 3f, projectile.owner);


            Vector2 projectilePos = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
            int num41 = Dust.NewDust(projectilePos, projectile.width, projectile.height, 15, 0f, 0f, 100, default, 2f);
            Main.dust[num41].noGravity = true;
            Main.dust[num41].velocity *= 2f;
            Dust.NewDust(projectilePos, projectile.width, projectile.height, 15, 0f, 0f, 100, default, 1f);
        }

    }
}
