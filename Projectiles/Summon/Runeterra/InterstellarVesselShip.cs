using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Buffs.Runeterra;

namespace tsorcRevamp.Projectiles.Summon.Runeterra
{
	public class InterstellarVesselShip : ModProjectile
	{
		public static float angularSpeed2 = 0.03f;
		public static float circleRad2 = 50f;
		public float currentAngle2 = 0;
        public static int timer2 = 0;

        public override void SetStaticDefaults()
		{
			//Main.projFrames[Projectile.type] = 5;
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
			Projectile.localNPCHitCooldown = 20;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (crit)
            {
                InterstellarCommander.hascrit2 = true;
            }
        }
        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (timer2 > 0)
            {
                damage += (Projectile.damage / 4);
            }
        }
        public override void OnSpawn(IEntitySource source) 
		{
			InterstellarVesselControls.projectiles.Add(this);
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
			InterstellarVesselControls.projectiles.Remove(this);
		}
		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];

			Vector2 visualplayercenter = owner.Center + new Vector2(-27, -12);

			if (!CheckActive(owner))
			{
				return;
            }

            if (timer2 > 0)
            {
                angularSpeed2 = 0.075f;
            }
            else
            {
                angularSpeed2 = 0.03f;
            }

            currentAngle2 += (angularSpeed2 / (circleRad2 * 0.001f + 1f)); 

			Vector2 offset = new Vector2(MathF.Sin(currentAngle2), MathF.Cos(currentAngle2)) * circleRad2;

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
				owner.ClearBuff(ModContent.BuffType<InterstellarCommander>());

				return false;
			}

			if (!owner.HasBuff(ModContent.BuffType<InterstellarCommander>()))
      {
				circleRad2 = 50f;
				currentAngle2 = 0;
				InterstellarVesselControls.projectiles.Clear();
      }

			if (owner.HasBuff(ModContent.BuffType<InterstellarCommander>()))
			{
				Projectile.timeLeft = 2;
			}

			return true;
		}
		private void Visuals()
		{

      Projectile.rotation = currentAngle2 * -1f;

      //float frameSpeed = 3f;

			//Projectile.frameCounter++;

			/*if (Projectile.frameCounter >= frameSpeed)
			{
				Projectile.frameCounter = 0;
				Projectile.frame++;

				if (Projectile.frame >= Main.projFrames[Projectile.type])
				{
					Projectile.frame = 0;
				}
			}*/

			// Some visuals here
			Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.48f);
		}
	}
}