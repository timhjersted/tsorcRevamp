using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    public class PoisonSmog : ModProjectile {
        public override void SetStaticDefaults() {
            Main.projFrames[projectile.type] = 4;
			DisplayName.SetDefault("Cursed Flame");
        }
        public override void SetDefaults() {
			projectile.width = 16;
			projectile.height = 16;
			projectile.hostile = true;
			projectile.tileCollide = false;
			projectile.timeLeft = 600;
        }
        public override void AI() {
			projectile.rotation += 0.1f;
			if (Main.rand.Next(4) == 0) {
				int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 50, Color.Green, 3.0f);
				Main.dust[dust].noGravity = false;
			}
			Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

			if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4) {
				float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
				projectile.velocity.X *= accel;
				projectile.velocity.Y *= accel;
			}
		}
		public override bool PreKill(int timeLeft) {
			projectile.type = 44; //killpretendtype
			return true;
		}
		public override void OnHitPlayer(Player target, int damage, bool crit) {
			target.AddBuff(BuffID.Poisoned, 600, false);
			target.AddBuff(BuffID.Tipsy, 1800, false);
		}
    }
}
