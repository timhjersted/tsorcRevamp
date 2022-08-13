using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Weapons.Runeterra.Ranged
{
	public class TSBlindDart : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Blinding Dart");
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
			Projectile.damage = owner.GetWeaponDamage(owner.HeldItem) + 20 + (int)owner.GetDamage(DamageClass.Magic).ApplyTo(owner.GetWeaponDamage(owner.HeldItem));
			Dust.NewDust(Projectile.Center, 10, 10, DustID.Demonite, 0, 0, 0, Color.HotPink, 0.75f);
		}
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
			target.AddBuff(BuffID.Confused, 120);
        }
    }
}