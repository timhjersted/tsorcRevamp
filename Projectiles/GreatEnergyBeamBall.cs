using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class GreatEnergyBeamBall : ModProjectile {
		public override string Texture => "tsorcRevamp/Projectiles/Bolt1Ball";
		public override void SetDefaults() {
			Projectile.friendly = true;
			Projectile.height = 16;
			Projectile.light = 1;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 8;
			Projectile.tileCollide = true;
			Projectile.width = 16;
			Projectile.timeLeft = 1;
        }

        public override void Kill(int timeLeft) {
			if (!Projectile.active) {
				return;
			}
			Projectile.timeLeft = 0;
			{
				Terraria.Audio.SoundEngine.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 10);
				if (Projectile.position.X + (float)(Projectile.width / 2) > Main.player[Projectile.owner].position.X + (float)(Main.player[Projectile.owner].width / 2)) {
					if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.position.X + (float)(Projectile.width * 14f), Projectile.position.Y + (float)(Projectile.height - 0.5f), 0, 0, ModContent.ProjectileType<GreatEnergyBeam>(), (int)(55 * (Main.player[Projectile.owner].magicDamage)), 8f, Projectile.owner);
				}
				else {
					if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.position.X + (float)(Projectile.width * -13), Projectile.position.Y + (float)(Projectile.height - 0.5f), 0, 0, ModContent.ProjectileType<GreatEnergyBeam>(), (int)(55 * (Main.player[Projectile.owner].magicDamage)), 8f, Projectile.owner);
				}
				Vector2 arg_1394_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
				int arg_1394_1 = Projectile.width;
				int arg_1394_2 = Projectile.height;
				int arg_1394_3 = 15;
				float arg_1394_4 = 0f;
				float arg_1394_5 = 0f;
				int arg_1394_6 = 100;
				int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, default, 2f);
				Main.dust[num41].noGravity = true;
				Dust expr_13B1 = Main.dust[num41];
				expr_13B1.velocity *= 2f;
				Vector2 arg_1422_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
				int arg_1422_1 = Projectile.width;
				int arg_1422_2 = Projectile.height;
				int arg_1422_3 = 15;
				float arg_1422_4 = 0f;
				float arg_1422_5 = 0f;
				int arg_1422_6 = 100;
				num41 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, default, 1f);
			}
			Projectile.active = false;
		}

	}
}
