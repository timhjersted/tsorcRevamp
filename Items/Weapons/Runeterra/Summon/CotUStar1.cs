using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Runeterra.Summon
{
	public class CotUStar1 : ModProjectile
	{
		public static float angularSpeed = 0.1f;
		public static float circleRad = 50f;
		public float currentAngle = 0;
    
		public override void SetStaticDefaults()
		{
			Main.projFrames[Projectile.type] = 8;
			Main.projPet[Projectile.type] = true; // Denotes that this projectile is a pet or minion
			ProjectileID.Sets.MinionSacrificable[Projectile.type] = true; // This is needed so your minion can properly spawn when summoned and replaced when other minions are summoned
		}
		public sealed override void SetDefaults()
		{
			Projectile.width = 66;
			Projectile.height = 28;
			Projectile.tileCollide = false; // Makes the minion go through tiles freely

			// These below are needed for a minion weapon
			Projectile.friendly = true; // Only controls if it deals damage to enemies on contact (more on that later)
			Projectile.minion = true; // Declares this as a minion (has many effects)
			Projectile.DamageType = DamageClass.Summon; // Declares the damage type (needed for it to deal damage)
			Projectile.minionSlots = 1f; // Amount of slots this minion occupies from the total minion slots available to the player (more on that later)
			Projectile.penetrate = -1; // Needed so the minion doesn't despawn on collision with enemies or tiles
			Projectile.extraUpdates = 1;

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 15;
		}
		public override void OnSpawn(IEntitySource source) 
		{
			CotUItem1.projectiles.Add(this);
		}
		public override bool? CanCutTiles()
		{
			return false;
		}
		public override bool MinionContactDamage()
		{
			return true;
		}
		public override void Kill(int timeLeft) 
		{
			CotUItem1.projectiles.Remove(this);
		}
		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];

			Vector2 visualplayercenter = owner.Center + new Vector2(-27, -12);

			if (!CheckActive(owner))
			{
				return;
			}

      currentAngle += (angularSpeed / (circleRad * 0.001f + 1f)); 

			Vector2 offset = new Vector2(MathF.Sin(currentAngle), MathF.Cos(currentAngle)) * circleRad;

			Projectile.position = visualplayercenter + offset;

      Visuals();
		}
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
      float distance = Vector2.Distance(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2());
			if (distance < Projectile.height * 1.2f && distance > Projectile.height * 1.2f - 32)
			{
        return true;
      }
      else
      {
        return false;
      }
    }
    private bool CheckActive(Player owner)
		{
			if (owner.dead || !owner.active)
			{
				owner.ClearBuff(ModContent.BuffType<CotUBuff1>());

				return false;
			}

			if (!owner.HasBuff(ModContent.BuffType<CotUBuff1>()))
      {
				circleRad = 50f;
        currentAngle = 0;
				CotUItem1.projectiles.Clear();
      }

			if (owner.HasBuff(ModContent.BuffType<CotUBuff1>()))
			{
				Projectile.timeLeft = 2;
			}

			return true;
		}
		private void Visuals()
		{

      Projectile.rotation = currentAngle * -1f;

      float frameSpeed = 3f;

			Projectile.frameCounter++;

			if (Projectile.frameCounter >= frameSpeed)
			{
				Projectile.frameCounter = 0;
				Projectile.frame++;

				if (Projectile.frame >= Main.projFrames[Projectile.type])
				{
					Projectile.frame = 0;
				}
			}

			// Some visuals here
			Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.78f);
		}
		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			target.AddBuff(BuffID.OnFire, 120);
		}
	}
}