using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Projectiles.VFX;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using System.Collections.Generic;

namespace tsorcRevamp.Projectiles.Summon.Runeterra
{
	public class InterstellarVesselShip : DynamicTrail
	{
		public float angularSpeed2 = 0.03f;
		public float currentAngle2 = 0;

        public override void SetStaticDefaults()
		{
            //Main.projFrames[Projectile.type] = 2;
            Main.projPet[Projectile.type] = true; 
			ProjectileID.Sets.MinionSacrificable[Projectile.type] = true; 
		}
		public sealed override void SetDefaults()
		{
			Projectile.width = 98;
			Projectile.height = 50;
			Projectile.tileCollide = false;

			Projectile.friendly = true; 
			Projectile.minion = true;
			Projectile.DamageType = DamageClass.Summon; 
			Projectile.minionSlots = 0.5f; 
			Projectile.penetrate = -1;
			Projectile.extraUpdates = 1;

			Projectile.usesIDStaticNPCImmunity = true;
			Projectile.idStaticNPCHitCooldown = 20;


			trailWidth = 45;
			trailPointLimit = 900;
			trailMaxLength = 333;
			Projectile.hide = true;
			collisionPadding = 50;
			NPCSource = false;
			trailCollision = true;
			collisionFrequency = 5;
			noFadeOut = true;
			customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/InterstellarVessel", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
		}
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
            {
                modifiers.SourceDamage *= 1.25f;
            }
        }
        public override void OnSpawn(IEntitySource source) 
		{
			InterstellarVesselGauntlet.projectiles.Add(this);
		}
		public override bool? CanCutTiles()
		{
			return false;
		}
		public override bool MinionContactDamage()
		{
			return true;
		}
		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		{
			behindNPCs.Add(index);
		}
		public override void Kill(int timeLeft) 
		{
			InterstellarVesselGauntlet.projectiles.Remove(this);
		}

		public override void AI()
		{
			base.AI();
			if (angularSpeed2 > 0.03f)
			{
				trailIntensity = 2;
			}
		

			if (trailIntensity > 1)
			{
				trailIntensity -= 0.05f;
			}

			Player owner = Main.player[Projectile.owner];
			tsorcRevampPlayer modPlayer = owner.GetModPlayer<tsorcRevampPlayer>();


			if (!CheckActive(owner))
			{
				return;
            }

            if (owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
            {
                angularSpeed2 = 0.075f;
            }
            if (!owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost || (owner.statMana <= 0))
            {
                angularSpeed2 = 0.03f;
				owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost = false;
				if (Main.netMode == NetmodeID.MultiplayerClient)
				{
					ModPacket minionPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
					minionPacket.Write(tsorcPacketID.SyncMinionRadius);
					minionPacket.Write((byte)owner.whoAmI);
					minionPacket.Write(owner.GetModPlayer<tsorcRevampPlayer>().MinionCircleRadius);
					minionPacket.Write(owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost);
					minionPacket.Send();
				}
			}

            currentAngle2 += (angularSpeed2 / (modPlayer.MinionCircleRadius * 0.001f + 1f)); 

			Vector2 offset = new Vector2(0, modPlayer.MinionCircleRadius).RotatedBy(-currentAngle2);

			Projectile.Center = owner.Center + offset;

            Visuals();
		}


		/*public override void SendExtraAI(BinaryWriter writer)
        {
			writer.Write(angularSpeed2);
		}
        public override void ReceiveExtraAI(BinaryReader reader)
        {
			angularSpeed2 = reader.ReadSingle();
		}*/
		Vector2 samplePointOffset1;
		Vector2 samplePointOffset2;
		float trailIntensity = 1;
		public override void SetEffectParameters(Effect effect)
		{
			trailWidth = 45;
			trailMaxLength = 500;

			effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture3);
			effect.Parameters["length"].SetValue(trailCurrentLength);
			float hostVel = 0;
			hostVel = Projectile.velocity.Length();
			float modifiedTime = 0.001f * hostVel;

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
			collisionEndPadding = trailPositions.Count / 2;

			effect.Parameters["samplePointOffset1"].SetValue(samplePointOffset1);
			effect.Parameters["samplePointOffset2"].SetValue(samplePointOffset2);
			effect.Parameters["fadeOut"].SetValue(trailIntensity);
			effect.Parameters["speed"].SetValue(hostVel);
			effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
			effect.Parameters["shaderColor"].SetValue(new Color(0.8f, 0.6f, 0.2f).ToVector4());
			effect.Parameters["secondaryColor"].SetValue(new Color(0.005f, 0.05f, 1f).ToVector4());
			effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
			return;
		}
		public override float CollisionWidthFunction(float progress)
		{
			return WidthFunction(progress) - 35;
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
				currentAngle2 = 0;
				InterstellarVesselGauntlet.projectiles.Clear();
			}

			if (owner.HasBuff(ModContent.BuffType<InterstellarCommander>()))
			{
				Projectile.timeLeft = 2;
			}

			return true;
		}
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
			if (hit.Crit)
			{
				target.AddBuff(ModContent.BuffType<ShockedDebuff>(), 80);
			}
			else
			{
				target.AddBuff(ModContent.BuffType<ShockedDebuff>(), 40);
			}
        }
		private void Visuals()
		{
			Projectile.rotation = currentAngle2 * -1f; 

			/*float frameSpeed = 5f;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }*/

            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.48f);
		}

		public static Texture2D texture;
		public static Texture2D glowTexture;
		public override bool PreDraw(ref Color lightColor)
        {
			if (additiveContext)
			{
				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

				if (texture == null || texture.IsDisposed)
				{
					texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Summon/Runeterra/InterstellarVesselShip", ReLogic.Content.AssetRequestMode.ImmediateLoad);
				}
				if (glowTexture == null || glowTexture.IsDisposed)
				{
					glowTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Summon/Runeterra/InterstellarVesselShip" + "Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad);
				}

				Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
				Vector2 origin = sourceRectangle.Size() / 2f;

				Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.Lerp(lightColor, Color.Orange, 0.25f), Projectile.rotation, origin, 1, SpriteEffects.None, 0f);
				Main.spriteBatch.Draw(glowTexture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation, origin, 1, SpriteEffects.None, 0f);

				Main.spriteBatch.End();
				Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
			}
			return false;
		}
    }	
}