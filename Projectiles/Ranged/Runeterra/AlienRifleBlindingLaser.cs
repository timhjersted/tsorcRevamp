using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
	public class AlienRifleBlindingLaser : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			// DisplayName.SetDefault("Blinding Laser");
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
			Projectile.extraUpdates = 3;

			AIType = ProjectileID.Bat;
		}

        public override void AI()
        {
			var owner = Main.player[Projectile.owner];
			Projectile.damage = (int)((owner.GetWeaponDamage(owner.HeldItem) + (0.8f * (owner.GetDamage(DamageClass.Magic).ApplyTo(owner.GetWeaponDamage(owner.HeldItem))))));
			Dust.NewDust(Projectile.Center, 10, 10, DustID.Demonite, 0, 0, 0, Color.HotPink, 0.75f);
		}
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
			target.AddBuff(BuffID.Confused, 120);
        }
    }
}