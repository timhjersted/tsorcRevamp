using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {
    class EnemySpellLightning3Ball : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = 4;
            projectile.hostile = true;
            projectile.height = 16;
            projectile.penetrate = 1;
            projectile.tileCollide = true;
            projectile.width = 16;
        }

		public override void Kill(int timeLeft) {
			if (!projectile.active) {
				return;
			}
			projectile.timeLeft = 0;
			{
				Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
				for (int num40 = 0; num40 < 20; num40++) {
					Projectile.NewProjectile(projectile.position.X + (float)(projectile.width / 2), projectile.position.Y + (float)(projectile.height / 2), 0, 0, ModContent.ProjectileType<EnemySpellLightning3Bolt>(), 70, 8f, projectile.owner);
					Vector2 projectilePos = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
					int num41 = Dust.NewDust(projectilePos, projectile.width, projectile.height, 15, 0f, 0f, 100, default, 1f);
					Main.dust[num41].noGravity = true;
					Main.dust[num41].velocity *= 2f;
					Dust.NewDust(projectilePos, projectile.width, projectile.height, 15, 0f, 0f, 100, default, 1f);
				}
			}
			if (projectile.owner == Main.myPlayer) {
				if (Main.netMode != NetmodeID.SinglePlayer) {
					NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, projectile.identity, (float)projectile.owner, 0f, 0f, 0);
				}
			}
			projectile.active = false;
		}

	}
}
