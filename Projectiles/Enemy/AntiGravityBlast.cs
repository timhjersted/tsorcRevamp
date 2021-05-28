using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy {
    class AntiGravityBlast : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 55;
            projectile.height = 55;
            projectile.scale = 2.3f;
            projectile.aiStyle = 9;
            aiType = 79;
            projectile.hostile = true;
            projectile.damage = 80;
            projectile.penetrate = 2;
            projectile.tileCollide = false;
        }
        public override void Kill(int timeLeft) {
            projectile.type = 79;
        }

		public override void AI() {
			projectile.rotation += 0.5f;

			if (Main.player[(int)projectile.ai[0]].position.X < projectile.position.X) {
				if (projectile.velocity.X > -10) projectile.velocity.X -= 0.1f;
			}

			if (Main.player[(int)projectile.ai[0]].position.X > projectile.position.X) {
				if (projectile.velocity.X < 10) projectile.velocity.X += 0.1f;
			}

			if (Main.player[(int)projectile.ai[0]].position.Y < projectile.position.Y) {
				if (projectile.velocity.Y > -10) projectile.velocity.Y -= 0.1f;
			}

			if (Main.player[(int)projectile.ai[0]].position.Y > projectile.position.Y) {
				if (projectile.velocity.Y < 10) projectile.velocity.Y += 0.1f;
			}

			if (Main.rand.Next(4) == 0) {
				int dust = Dust.NewDust(new Vector2((float)projectile.position.X + 10, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 200, Color.Red, 1f);
				Main.dust[dust].noGravity = true;
			}
			Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);
		}

        public override void OnHitPlayer(Player target, int damage, bool crit) {
			target.AddBuff(BuffID.Gravitation, 180);
			target.AddBuff(BuffID.Confused, 180);
        }
    }
}
