using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    class ShadowOrb : ModProjectile {

        public override void SetDefaults() {
	    projectile.timeLeft = 480;
            projectile.hostile = true;
            projectile.height = 15;
            projectile.width = 15;
            projectile.scale = 0.9f;
			projectile.tileCollide = false;
        }

        public override void Kill(int timeLeft) {
            projectile.type = 44;
        }

		public override void AI() {
			projectile.rotation++;
			int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 100, Color.Red, 2.0f);
			Main.dust[dust].noGravity = true;

			if (projectile.velocity.X <= 10 && projectile.velocity.Y <= 10 && projectile.velocity.X >= -10 && projectile.velocity.Y >= -10) {
				projectile.velocity.X *= 1.01f;
				projectile.velocity.Y *= 1.01f;
			}
		}

        public override void OnHitPlayer(Player target, int damage, bool crit) {
			if (Main.rand.Next(2) == 0) {
				target.AddBuff(BuffID.BrokenArmor, 120, false); //broken armor
				target.AddBuff(BuffID.Weak, 600, false); //weak
				target.AddBuff(BuffID.OnFire, 180, false); //on fire!
			}

			if (Main.rand.Next(8) == 0) {
				target.AddBuff(ModContent.BuffType<Buffs.FracturingArmor>(), 1800, false);
			}
		}
    }
}
