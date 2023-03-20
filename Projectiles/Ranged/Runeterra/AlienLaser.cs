using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using tsorcRevamp.Buffs.Runeterra;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
	public class AlienLaser : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Alien Laser");
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
			ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
		}

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.aiStyle = ProjAIStyleID.SmallFlying;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 240;
			Projectile.light = 0.5f;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;

			AIType = ProjectileID.Bat;
		}

        public override void AI()
        {
			var owner = Main.player[Projectile.owner];
			Dust.NewDust(Projectile.Center, 10, 10, DustID.VenomStaff, 0, 0, 0, Color.DarkRed, 0.75f);
            Projectile.extraUpdates = 2;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
			target.AddBuff(ModContent.BuffType<ElectrifiedDebuff>(), 40);
        }
		public override bool PreDraw(ref Color lightColor)
		{
			return false;
		}
	}
}