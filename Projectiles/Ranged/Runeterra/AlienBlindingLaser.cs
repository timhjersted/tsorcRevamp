using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.DataStructures;
using tsorcRevamp.Buffs.Runeterra.Ranged;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
	public class AlienBlindingLaser : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Alien Blinding Laser");
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
			ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
		}

		public override void SetDefaults()
		{
			Projectile.width = 2;
			Projectile.height = 2;
			Projectile.aiStyle = ProjAIStyleID.SmallFlying;
			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 300;
			Projectile.light = 0.75f;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 3;

			AIType = ProjectileID.Bat;
        }
        public override void OnSpawn(IEntitySource source)
        {
            var player = Main.player[Projectile.owner];
            player.AddBuff(ModContent.BuffType<AlienBlindingLaserCooldown>(), 5 * 60);
            Projectile.damage *= 4;
			Projectile.CritChance += 100;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            //Dust.NewDust(Projectile.Center + new Vector2(0, -5), 10, 10, DustID.Demonite, 0, 0, 0, Color.HotPink, 0.75f);
		}
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(ModContent.BuffType<ElectrifiedDebuff>(), 2 * 60);
            target.AddBuff(BuffID.Confused, 2 * 60);
        }
    }
}