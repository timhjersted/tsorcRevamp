using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	public class EnemyCrystalKnightBoltII : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Freezing Crystal Bolt");
		}
		public override void SetDefaults()
		{
			projectile.aiStyle = 16;
			projectile.hostile = true;
			projectile.height = 16;
			projectile.light = 1;
			projectile.ranged = true;
			projectile.penetrate = 8;
			projectile.scale = 1.3f;
			projectile.tileCollide = true;
			projectile.width = 16;
			projectile.timeLeft = 300;
			projectile.ignoreWater = true;
		}

		public override void AI()
		{
			int num40 = Dust.NewDust(new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y), projectile.width, projectile.height, 15, projectile.velocity.X, projectile.velocity.Y, 250, default(Color), 1f);
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.DarkInferno>(), 600, false);
			//Main.player[Main.myPlayer].AddBuff(4, 1200, false); //gills. Which used to make you suffocate out of water. Guess this evil was too deep for modern Terraria...

			if (Main.expertMode)
			{
				Main.player[Main.myPlayer].AddBuff(BuffID.Frozen, 15, false); //slowed
				Main.player[Main.myPlayer].AddBuff(BuffID.Slow, 300, false); //normal slow
			}
			else
			{
				Main.player[Main.myPlayer].AddBuff(BuffID.Frozen, 30, false); //slowed
				Main.player[Main.myPlayer].AddBuff(BuffID.Slow, 600, false); //normal slow
			}
		}
	}
}