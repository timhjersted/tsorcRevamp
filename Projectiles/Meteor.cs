using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
	class Meteor : ModProjectile {

		public override void SetDefaults() {
			//projectile.aiStyle = 9;
			projectile.friendly = true;
			projectile.height = 48;
			projectile.width = 48;
			projectile.light = 0.8f;
			projectile.magic = true;
			projectile.penetrate = 1;
			projectile.tileCollide = true;
			projectile.timeLeft = 200;
		}

		public override void AI()
        {
			int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 127, projectile.velocity.X / 2, projectile.velocity.Y / 2, 160, default, 3f);
			Main.dust[dust].noGravity = true;
			dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 130, projectile.velocity.X / 2, projectile.velocity.Y / 2, 220, default, 1f);
			Main.dust[dust].noGravity = true;
			Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(58, Main.LocalPlayer);
		}

        public override void Kill(int timeLeft)
        {
			for (int i = 0; i < 30; i++)
			{
				int dust = Dust.NewDust(new Vector2(projectile.position.X - (float)(projectile.width / 2), projectile.position.Y - (float)(projectile.height / 2)), projectile.width, projectile.height, 127, Main.rand.Next(-10, 10) + projectile.velocity.X, Main.rand.Next(-10, 10) + projectile.velocity.Y, 160, default, 3f);
				dust = Dust.NewDust(new Vector2(projectile.position.X - (float)(projectile.width / 2), projectile.position.Y - (float)(projectile.height / 2)), projectile.width, projectile.height, 127, Main.rand.Next(-10, 10) + projectile.velocity.X, Main.rand.Next(-10, 10) + projectile.velocity.Y, 160, default, 3f);
				Main.dust[dust].noGravity = true;
				dust = Dust.NewDust(new Vector2(projectile.position.X - (float)(projectile.width / 2), projectile.position.Y - (float)(projectile.height / 2)), projectile.width, projectile.height, 130, Main.rand.Next(-10, 10) + projectile.velocity.X, Main.rand.Next(-10, 10) + projectile.velocity.Y, 160, default, 1.5f);
				Main.dust[dust].noGravity = true;
				Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(58, Main.LocalPlayer);
			}

			projectile.penetrate = 20;
			projectile.width = 200;
			projectile.height = 200;
			projectile.damage /= 2;
			projectile.Damage();
		}
    }
}


