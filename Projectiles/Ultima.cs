using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles {
    class Ultima : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[Projectile.type] = 13;
        }

        public override void SetDefaults() {
            Projectile.width = 150;
            Projectile.height = 80;
            Projectile.friendly = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

		public override void AI() {
			Projectile.frameCounter++;
			if (Projectile.frameCounter > 3) {
				Projectile.frame++;
				Projectile.frameCounter = 0;
			}
			if (Projectile.frame >= 13) {
				Projectile.Kill();
				if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2), 0, 0, ModContent.ProjectileType<UltimaExplosion>(), 500, 8f, Projectile.owner);
				return;
			}
			{
				if (Projectile.ai[1] == 0f) {
					Projectile.ai[1] = 1f;
					Projectile.netUpdate = true;
				}
			}
			if (Projectile.soundDelay == 0) {
				Projectile.soundDelay = 20 + Main.rand.Next(40);
				Main.PlaySound(SoundID.Item, (int)Projectile.position.X, (int)Projectile.position.Y, 9);
			}
			if (Projectile.localAI[0] == 0f) {
				Projectile.localAI[0] = 1f;
			}
			Projectile.alpha += (int)(25f * Projectile.localAI[0]);
			if (Projectile.alpha > 200) {
				Projectile.alpha = 200;
				Projectile.localAI[0] = -1f;
			}
			if (Projectile.alpha < 0) {
				Projectile.alpha = 0;
				Projectile.localAI[0] = 1f;
			}
			Projectile.rotation += (Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y)) * 0.01f * (float)Projectile.direction;
			if (Projectile.ai[1] == 1f || Projectile.type == 92) {
				Projectile.light = 0.9f;
				if (Main.rand.Next(10) == 0) {
					Vector2 arg_1328_0 = Projectile.position;
					int arg_1328_1 = Projectile.width;
					int arg_1328_2 = Projectile.height;
					int arg_1328_3 = 58;
					float arg_1328_4 = Projectile.velocity.X * 0.5f;
					float arg_1328_5 = Projectile.velocity.Y * 0.5f;
					int arg_1328_6 = 150;
					Dust.NewDust(arg_1328_0, arg_1328_1, arg_1328_2, arg_1328_3, arg_1328_4, arg_1328_5, arg_1328_6, default, 1.2f);
				}
				if (Main.rand.Next(20) == 0) {
					Gore.NewGore(Projectile.position, new Vector2(Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f), Main.rand.Next(16, 18), 1f);
					return;
				}
			}
		}
	}
}
