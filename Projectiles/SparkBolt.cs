using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles {

	public class SparkBolt : ModProjectile {

		public override void SetDefaults() {
			projectile.width = 10;
			projectile.height = 10;
			projectile.aiStyle = 1; //i have no clue but it works
			projectile.friendly = true; //can hit enemies
			projectile.hostile = false; //can hit player / friendly NPCs
			projectile.timeLeft = 300;
			projectile.ignoreWater = false;
			projectile.tileCollide = true;
			aiType = ProjectileID.WoodenArrowFriendly; //slow, gravity arc
		}

		public override void AI() {
			Lighting.AddLight(projectile.position, 0.8f, 0.4f, 0.8f);
		}
	}
}