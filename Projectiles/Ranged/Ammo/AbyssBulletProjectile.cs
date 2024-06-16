using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Ammo;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class AbyssBulletProjectile : ModProjectile
    {

        public override void SetDefaults()
        {
			Projectile.width = 8; 
			Projectile.height = 8; 
			Projectile.aiStyle = 1; 
			Projectile.friendly = true; 
			Projectile.hostile = false; 
			Projectile.DamageType = DamageClass.Ranged; 
			Projectile.timeLeft = 600; 
			Projectile.light = 0.5f; 
			Projectile.ignoreWater = true; 
			Projectile.tileCollide = true; 
			Projectile.extraUpdates = 2; 

			AIType = ProjectileID.Bullet; 
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 3 * 60);
        }

		public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.9f, 0.5f, 0f);
        }

		public override void OnKill(int timeLeft) 
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
            for (int i = 0; i < 5; i++)
			{
				Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.SolarFlare, 1f);
				dust.noGravity = true;
            }
        }
    }
}