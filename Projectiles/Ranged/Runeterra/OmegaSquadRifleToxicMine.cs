using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
	public class OmegaSquadRifleToxicMine: ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Toxic Mine");
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
			ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
		}

		public override void SetDefaults()
		{
			Projectile.width = 22;
			Projectile.height = 24;

			Projectile.friendly = true;
			Projectile.DamageType = DamageClass.Ranged;
			Projectile.penetrate = 1;
			Projectile.timeLeft = 3600;
			Projectile.light = 0.5f;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = true;
			Projectile.extraUpdates = 3;

		}

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            Projectile.damage = (int)((2 * owner.GetWeaponDamage(owner.HeldItem)) + (owner.GetDamage(DamageClass.Magic).ApplyTo(owner.GetWeaponDamage(owner.HeldItem))));
            Projectile.CritChance = owner.GetWeaponCrit(owner.HeldItem);
            Dust.NewDust(Projectile.TopLeft, 20, 20, DustID.GlowingMushroom, 0, 0, 250, Color.Brown, 0.25f);
            float maxDetectRadius = 100f;
            float projSpeed = 5f;


            NPC closestNPC = FindClosestNPC(maxDetectRadius);
            if (closestNPC == null)
            {
                return;
            }

            Projectile.velocity = (closestNPC.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * projSpeed;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;

            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

            for (int k = 0; k < Main.maxNPCs; k++)
            {
                NPC target = Main.npc[k];
                if (target.CanBeChasedBy())
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestNPC = target;
                    }
                }
            }
            return closestNPC;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
			target.AddBuff(BuffID.Venom, 120);
        }
    }
}