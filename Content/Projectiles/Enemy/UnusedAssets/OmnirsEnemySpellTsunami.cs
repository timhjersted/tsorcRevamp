using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.UnusedAssets
{
	public class OmnirsEnemySpellTsunami : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			Main.projFrames[Projectile.type] = 4;
		}
		public override void SetDefaults()
		{
			Projectile.Name = "Tsunami";
			Projectile.width = 150;
			Projectile.height = 120;
			Projectile.penetrate = 50;
			Projectile.knockBack = 9;
			Projectile.timeLeft = 360;
			Projectile.alpha = 100;
			Projectile.light = 1f;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
		}
		public override void AI() 
		{
			Projectile.frameCounter++;
			if (Projectile.frameCounter > 3)
			{
				Projectile.frame++;
				Projectile.frameCounter = 0;
			}
			if (Projectile.frame >= 4)
			{
				Projectile.Kill();
				return;
			}
		}
	}
}