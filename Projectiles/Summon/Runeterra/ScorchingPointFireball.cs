using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Buffs.Runeterra;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Buffs.Summon.WhipDebuffs;

namespace tsorcRevamp.Projectiles.Summon.Runeterra
{
	public class ScorchingPointFireball : ModProjectile
	{
		public static float angularSpeed = 0.03f;
		public static float circleRad = 50f;
		public float currentAngle = 0;
        bool spawnedTrail = false;

        public override void SetStaticDefaults()
		{
			Main.projFrames[Projectile.type] = 8;
			Main.projPet[Projectile.type] = true;
			ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
		}
		public sealed override void SetDefaults()
		{
			Projectile.width = 66;
			Projectile.height = 28;
			Projectile.tileCollide = false;

			Projectile.friendly = true; 
			Projectile.minion = true;
			Projectile.DamageType = DamageClass.Summon;
			Projectile.minionSlots = 1f;
			Projectile.penetrate = -1;
			Projectile.extraUpdates = 1;

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
		}
		public override void OnSpawn(IEntitySource source)
		{
			ScorchingPoint.projectiles.Add(this);
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
			ScorchingPoint.projectiles.Remove(this);
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
			Projectile.velocity = Projectile.rotation.ToRotationVector2();

            if (!spawnedTrail)
			{
				Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<ScorchingPointTrail>(), 0, 0, Projectile.owner, 0, Projectile.whoAmI);
				spawnedTrail = true;
			}

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
				owner.ClearBuff(ModContent.BuffType<CenterOfTheHeat>());

				return false;
			}

			if (!owner.HasBuff(ModContent.BuffType<CenterOfTheHeat>()))
			{
				circleRad = 50f;
				currentAngle = 0;
				ScorchingPoint.projectiles.Clear();
			}

			if (owner.HasBuff(ModContent.BuffType<CenterOfTheHeat>()))
			{
				Projectile.timeLeft = 2;
			}

			return true;
		}
		private void Visuals()
		{

			Projectile.rotation = currentAngle * -1f;

			float frameSpeed = 4f;

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

			Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.48f);
		}
		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			if (crit)
			{
                target.AddBuff(ModContent.BuffType<ScorchingDebuff>(), 80);
            }
			else
			{
                target.AddBuff(ModContent.BuffType<ScorchingDebuff>(), 40);
            }
		}

        public override bool PreDraw(ref Color lightColor)
        {
			return false;
        }
    }
}