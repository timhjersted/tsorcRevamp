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
    }
}