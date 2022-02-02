using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellIce3Ball : ModProjectile {
		public override string Texture => "tsorcRevamp/Projectiles/Ice1Ball";
        public override void SetDefaults() {
            projectile.aiStyle = 4;
            projectile.hostile = true;
            projectile.height = 16;
            projectile.magic = true;
            projectile.penetrate = 1;
            projectile.tileCollide = true;
            projectile.width = 16;
        }
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Enemy Spell Ice 3");

		}
		public override void AI()
        {
			if (projectile.ai[0] != 0) //Dark Elf Mage version
			{
				projectile.timeLeft = 80;
				projectile.aiStyle = 1;
			}

			if(Math.Abs(Main.player[(int)projectile.ai[1]].Center.X - projectile.Center.X) < 8 )
            {
				projectile.Kill();
				projectile.active = false;
			}
		}

        public override void Kill(int timeLeft) {
			Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 30, 0.2f, .3f); //ice materialize - good
			//Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
			int Icicle = ModContent.ProjectileType<EnemySpellIce3Icicle>();
			Projectile.NewProjectile(projectile.position.X + (float)(projectile.width), projectile.position.Y + (float)(projectile.height), 0, 5, Icicle, projectile.damage, 3f, projectile.owner);
			Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * 4), projectile.position.Y + (float)(projectile.height * 2), 0, 5, Icicle, projectile.damage, 3f, projectile.owner);
			Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * -2), projectile.position.Y + (float)(projectile.height * 2), 0, 5, Icicle, projectile.damage, 3f, projectile.owner);
			Vector2 projectilePos = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
			int num41 = Dust.NewDust(projectilePos, projectile.width, projectile.height, 15, 0f, 0f, 100, default, 2f);
			Main.dust[num41].noGravity = true;
			Main.dust[num41].velocity *= 2f;
			Dust.NewDust(projectilePos, projectile.width, projectile.height, 15, 0f, 0f, 100, default, 1f);
		}
	}
}
