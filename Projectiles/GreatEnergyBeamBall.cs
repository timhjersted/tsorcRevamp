using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class GreatEnergyBeamBall : ModProjectile {

        public override void SetDefaults() {
			projectile.friendly = true;
			projectile.height = 16;
			projectile.light = 1;
			projectile.magic = true;
			projectile.penetrate = 8;
			projectile.tileCollide = true;
			projectile.width = 16;
			projectile.timeLeft = 1;
        }

        public override void Kill(int timeLeft) {
			if (!projectile.active) {
				return;
			}
			projectile.timeLeft = 0;
			{
				Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
				if (projectile.position.X + (float)(projectile.width / 2) > Main.player[projectile.owner].position.X + (float)(Main.player[projectile.owner].width / 2)) {
					if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * 14f), projectile.position.Y + (float)(projectile.height - 0.5f), 0, 0, ModContent.ProjectileType<GreatEnergyBeam>(), (int)(55 * (Main.player[projectile.owner].magicDamage)), 8f, projectile.owner);
				}
				else {
					if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(projectile.position.X + (float)(projectile.width * -13), projectile.position.Y + (float)(projectile.height - 0.5f), 0, 0, ModContent.ProjectileType<GreatEnergyBeam>(), (int)(55 * (Main.player[projectile.owner].magicDamage)), 8f, projectile.owner);
				}
				Vector2 arg_1394_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
				int arg_1394_1 = projectile.width;
				int arg_1394_2 = projectile.height;
				int arg_1394_3 = 15;
				float arg_1394_4 = 0f;
				float arg_1394_5 = 0f;
				int arg_1394_6 = 100;
				int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, default, 2f);
				Main.dust[num41].noGravity = true;
				Dust expr_13B1 = Main.dust[num41];
				expr_13B1.velocity *= 2f;
				Vector2 arg_1422_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
				int arg_1422_1 = projectile.width;
				int arg_1422_2 = projectile.height;
				int arg_1422_3 = 15;
				float arg_1422_4 = 0f;
				float arg_1422_5 = 0f;
				int arg_1422_6 = 100;
				num41 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, default, 1f);
			}
			projectile.active = false;
		}

	}
}
