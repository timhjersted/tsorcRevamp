using Terraria.Audio;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles{
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