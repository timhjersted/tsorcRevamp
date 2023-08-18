using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Projectiles.VFX;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using tsorcRevamp.NPCs;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee;

namespace tsorcRevamp.Projectiles.VFX
{
	public class Slash : DynamicTrail
	{
		public sealed override void SetDefaults()
		{
			Projectile.width = 1;
			Projectile.height = 1;
			Projectile.tileCollide = false;
			Projectile.friendly = false; 
			Projectile.penetrate = -1;

            trailWidth = 45;
			trailPointLimit = 900;
			trailMaxLength = 500;
			Projectile.hide = true;
			collisionPadding = 5;
			NPCSource = false;
			trailCollision = false;
			noFadeOut = true;
			ScreenSpace = true;
			newPointDistance = 0.000f;
			customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/InterstellarVessel", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
		}
		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		{
			behindNPCs.Add(index);
		}

		bool rotatingRight;
		bool initializedSlash;
		float speed;
		float progress;
		public override void AI()
		{
            Player owner = Main.player[Projectile.owner];
            tsorcRevampPlayer modPlayer = owner.GetModPlayer<tsorcRevampPlayer>();
			bool flippedSwing = false;
			if (owner.HeldItem.TryGetGlobalItem(out ItemMeleeAttackAiming aiming))
			{
				flippedSwing = aiming.AttackId % 2 != 0;
			}

			if (!initializedSlash)
			{
				trailWidth = (int)(owner.HeldItem.height * 1.5f);
				Projectile.rotation = Projectile.velocity.ToRotation();
				Projectile.timeLeft = owner.HeldItem.useTime + 5;
				speed = -MathHelper.Pi / owner.HeldItem.useTime; //Swing clockwise if we're aiming right
				rotatingRight = Projectile.velocity.X > 0;
				if (!rotatingRight)
                {
					speed *= -1; //And counter-clockwise if aiming left
                }
                if (!flippedSwing)
                {
					speed *= -1;
                }
				Projectile.velocity = (Projectile.rotation + MathHelper.PiOver2).ToRotationVector2();
				initializedSlash = true;
			}

			float rotationOffset = -MathHelper.PiOver2;
            if (!rotatingRight)
            {
				rotationOffset *= -1;
			}
			if (flippedSwing)
			{
				rotationOffset *= -1;
			}

			Projectile.rotation += speed;
			progress += speed;

			Projectile.Center = owner.Center + new Vector2(0 + trailWidth / 1.5f, 0).RotatedBy(Projectile.rotation + rotationOffset);
			Projectile.velocity = Projectile.rotation.ToRotationVector2();

			if (!initialized)
			{
				Initialize();
			}

			trailPositions.Add(HostEntity.Center - Main.screenPosition);
			trailRotations.Add(HostEntity.velocity.ToRotation());

			float subdivisionCount = 5;// (int)(50 * Math.Abs(speed));
			for(float i = 0; i < subdivisionCount; i++)
            {
				trailPositions.Add(owner.Center + new Vector2(trailWidth / 1.5f, 0).RotatedBy(Projectile.rotation + (speed * i / subdivisionCount) + rotationOffset) - Main.screenPosition);
				trailRotations.Add(Projectile.rotation + (speed * i / subdivisionCount));
			}


			speed *= 1.08f;
			if (Math.Abs(progress) > 2.8)
			{
				speed /= 2;
			}
		}

		//Projectile.rotation += speed * 3 * ((float)Math.Pow(Math.Sin(MathHelper.Pi * ((float)Projectile.timeLeft) / ((float)owner.HeldItem.useTime)), 5) + 0.08f);

		Vector2 samplePointOffset1;
		Vector2 samplePointOffset2;
		float trailIntensity = 1;
		public override void SetEffectParameters(Effect effect)
		{
			visualizeTrail = false;
			effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseWavy);
			effect.Parameters["length"].SetValue(2 * trailWidth * progress);
			float hostVel = Projectile.velocity.Length();
			float modifiedTime = 0.0002f * hostVel;
			if(Projectile.timeLeft < 10)
            {
				trailIntensity = Projectile.timeLeft / 10f;
            }
			if (Main.gamePaused)
			{
				modifiedTime = 0;
			}
			samplePointOffset1.X += (modifiedTime * 2);
			samplePointOffset1.Y -= (0.001f);
			samplePointOffset2.X += (modifiedTime * 3.01f);
			samplePointOffset2.Y += (0.001f);

			samplePointOffset1.X += modifiedTime;
			samplePointOffset1.X %= 1;
			samplePointOffset1.Y %= 1;
			samplePointOffset2.X %= 1;
			samplePointOffset2.Y %= 1;

			effect.Parameters["samplePointOffset1"].SetValue(samplePointOffset1);
			effect.Parameters["samplePointOffset2"].SetValue(samplePointOffset2);
			effect.Parameters["fadeOut"].SetValue(trailIntensity);
			effect.Parameters["speed"].SetValue(hostVel);
			effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
			effect.Parameters["shaderColor"].SetValue(new Color(0.8f, 0.6f, 0.2f).ToVector4());
			effect.Parameters["secondaryColor"].SetValue(new Color(0.005f, 0.05f, 1f).ToVector4());
			effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
		}

		public static Texture2D texture;
		public static Texture2D glowTexture;
		public override bool PreDraw(ref Color lightColor)
        {
			visualizeTrail = false;
			base.PreDraw(ref lightColor);
			return false;
		}
    }	
}