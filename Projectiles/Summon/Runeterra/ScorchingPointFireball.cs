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
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.Projectiles.Summon.Runeterra
{
	public class ScorchingPointFireball : DynamicTrail
	{
		public float angularSpeed = 0.03f;
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
			Projectile.minionSlots = 0.5f;
			Projectile.penetrate = -1;
			Projectile.extraUpdates = 1;

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;

			trailWidth = 45;
			trailPointLimit = 900;
			trailMaxLength = 111;
			collisionPadding = 50;
			NPCSource = false;
			trailCollision = true;
			collisionFrequency = 5;
			noFadeOut = true;
			customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedFlamelash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
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
			base.AI();

			Player owner = Main.player[Projectile.owner];
			tsorcRevampPlayer modPlayer = owner.GetModPlayer<tsorcRevampPlayer>();


			if (!CheckActive(owner))
			{
				return;
			}

			currentAngle += (angularSpeed / (modPlayer.MinionCircleRadius * 0.001f + 1f));

			Vector2 offset = new Vector2(MathF.Sin(currentAngle), MathF.Cos(currentAngle)) * modPlayer.MinionCircleRadius;

			Projectile.Center = owner.Center + offset;
			Projectile.velocity = Projectile.rotation.ToRotationVector2();

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
		public override float CollisionWidthFunction(float progress)
		{
			return WidthFunction(progress) - 35;
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

		Vector2 samplePointOffset1;
		Vector2 samplePointOffset2;
		public override void SetEffectParameters(Effect effect)
		{
			trailWidth = 45;
			trailMaxLength = 400;

			effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture3);
			effect.Parameters["length"].SetValue(trailCurrentLength);
			float hostVel = 0;
			hostVel = Projectile.velocity.Length();
			float modifiedTime = 0.001f * hostVel;

			if (Main.gamePaused)
			{
				modifiedTime = 0;
			}
			samplePointOffset1.X += (modifiedTime);
			samplePointOffset1.Y -= (0.001f);
			samplePointOffset2.X += (modifiedTime * 3.01f);
			samplePointOffset2.Y += (0.001f);

			samplePointOffset1.X += modifiedTime;
			samplePointOffset1.X %= 1;
			samplePointOffset1.Y %= 1;
			samplePointOffset2.X %= 1;
			samplePointOffset2.Y %= 1;
			collisionEndPadding = trailPositions.Count / 2;

			effect.Parameters["samplePointOffset1"].SetValue(samplePointOffset1);
			effect.Parameters["samplePointOffset2"].SetValue(samplePointOffset2);
			effect.Parameters["fadeOut"].SetValue(fadeOut);
			effect.Parameters["speed"].SetValue(hostVel);
			effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
			effect.Parameters["shaderColor"].SetValue(Color.Orange.ToVector4());
			effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
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
    }
}