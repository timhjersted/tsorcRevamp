using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemySpellGreatFireballBall  : ModProjectile
	{

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Enemy Spell Great Fireball Ball");

		}
		public override void SetDefaults()
		{
			Projectile.hostile = true;
			Projectile.height = 16;
			Projectile.width = 16;
			Projectile.light = 0.8f;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.penetrate = 1;
			Projectile.tileCollide = true;
		}

		public override void AI()
		{
			if ( Math.Abs(Projectile.velocity.X) + Math.Abs(Projectile.velocity.Y) > 2f)//projectile.soundDelay == 0 &&
			{
				//projectile.soundDelay = 10;
				//Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
				Main.PlaySound(2, (int)Projectile.position.X, (int)Projectile.position.Y, 34, 0.01f, 0f); // flamethrower

				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 42, 0.6f, 0f); //flaming wood, high pitched air going out
				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 43, 0.6f, 0f); //staff magic cast, low sound
				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 45, 0.4f, 0.7f); //inferno fork, almost same as fire (works)
				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 48, 0.6f, 0.7f); // mine snow, tick sound
				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 60, 0.6f, 0.0f); //terra beam
				// Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish

				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 81, 0.6f, 0f); //spawn slime mount, more like thunder flame burn
				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 88, 0.6f, 0f); //meteor staff more bass and fire
				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 100, 0.1f, 0f); // cursed flame wall, lasts a bit longer than flame
				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 101, 0.6f, 0f); // crystal vilethorn - breaking crystal
				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 103, 0.6f, 0f); //shadowflame hex (little beasty)
				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 104, 0.6f, 0f); //shadowflame 
				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 106, 0.6f, 0f); //flask throw tink sound

				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 109, 0.6f, 0.0f); //crystal serpent fire
				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 110, 0.6f, -0.01f); //crystal serpent split, paper, thud, faint high squeel

				//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 125, 0.3f, .2f); //phantasmal bolt fire 2

			}
			int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, 0, 0, 100, default, 2f);
			Main.dust[thisDust].noGravity = true;

			Projectile.rotation += 0.25f;
		}

        public override bool PreKill(int timeLeft)
        {
			Main.PlaySound(2, (int)Projectile.position.X, (int)Projectile.position.Y, 100, 0.5f, 0f); // cursed flame wall, lasts a bit longer than flame
																									  //Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 125, 0.3f, .2f); //phantasmal bolt fire 2
																									  //Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 69, 0.6f, 0.0f); //earth staff rough fireish
			if (Projectile.owner == Main.myPlayer)
			{
				Projectile.NewProjectile(Projectile.position.X, Projectile.position.Y, 0, 0, ModContent.ProjectileType<EnemySpellGreatFireball>(), Projectile.damage, 6f, Projectile.owner);
			}

			for (int i = 0; i < 5; i++)
			{
				Vector2 vel = Main.rand.NextVector2Circular(12, 12);
				int thisDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 15, vel.X, vel.Y, 100, default, 2f);
				Main.dust[thisDust].noGravity = true;
			}
			return true;
		}
	}
}