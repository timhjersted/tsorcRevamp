using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.UnusedAssets
{
	[Autoload(false)] //doesn't seem to have a texture?
	public class OmnirsEnemySpellToxin : ModProjectile
	{
		public override void SetDefaults()
		{
			
			Projectile.width = 60;
			Projectile.height = 80;
			Projectile.penetrate = 20;
			Projectile.knockBack = 9;
			Projectile.timeLeft = 360;
			Projectile.alpha = 150;
			Projectile.light = 0.2f;
			Projectile.friendly = false;
			Projectile.hostile = true;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;
		}
	}
}