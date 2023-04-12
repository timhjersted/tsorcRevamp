using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {

	public abstract class ChargedBowHeld : ModProjectile {
		protected int minDamage;

		protected int maxDamage;

		protected float minVelocity;

		protected float maxVelocity;

		protected float chargeRate;

		protected SoundStyle soundtype = SoundID.Item5 with { PitchVariance = 0.2f };

		protected float holdoutOffset;

		protected bool fired;

		protected float charge;

		protected Vector2 aimVector;

		protected Texture2D pointTexture;

		protected int ammoType {
			get {
				return (int)Projectile.ai[0];
			}
			set {
				Projectile.ai[0] = value;
			}
		}

        public override void SetStaticDefaults() {
			// DisplayName.SetDefault("Charged Bow");
			ProjectileID.Sets.NeedsUUID[Projectile.type] = true;
        }
		public sealed override void SetDefaults() {
			Projectile.width = 16;
			Projectile.height = 16;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.tileCollide = false;
			Projectile.alpha = 0;
			Projectile.timeLeft = 999999; //"ummm zeo if you hold left click for 4.6 irl hours the bow disappears!!!! please fix!!!" 
            SetStats();
            pointTexture = ModContent.Request<Texture2D>("tsorcRevamp/Textures/ChargePoint", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

		protected abstract void SetStats();

		protected virtual void SpecialBehavior() {
		}

		protected virtual void Animate() {
            Projectile.frame = (int)((Main.projFrames[Projectile.type] - 1) * charge);

        }

		protected abstract void Shoot();

		public override void SendExtraAI(BinaryWriter writer) {
			writer.WriteVector2(aimVector);
		}

		public override void ReceiveExtraAI(BinaryReader reader) {
			aimVector = reader.ReadVector2();
		}


		public sealed override void AI() {
			Player player = Main.player[Projectile.owner];
			Projectile.damage = 0;
			if (!player.channel)
				Projectile.Kill();
			Animate();
			SpecialBehavior();
			UpdateAim();
			if (player.channel && !fired) {
				Projectile.timeLeft = Math.Max(Projectile.timeLeft, 2);
				if (charge < 1f) {
					charge += chargeRate;
				}
			}
			else {
				Shoot();
				SoundEngine.PlaySound(soundtype, player.Center);
				fired = true;
			}
		}

		protected virtual void UpdateAim() {
			Projectile.timeLeft = 2;
			Player player = Main.player[Projectile.owner];
			Vector2 playerHandPos = player.RotatedRelativePoint(player.MountedCenter);
			Projectile.Center = new Vector2(playerHandPos.X, playerHandPos.Y);
			Projectile.rotation = Projectile.velocity.ToRotation() + (float)Math.PI / 2f;
			player.heldProj = Projectile.whoAmI;
			player.itemRotation = (Projectile.velocity * Projectile.direction).ToRotation();
			player.itemAnimation = player.itemTime = 2;
			player.ChangeDir(Projectile.direction);
			
			if (Projectile.owner == Main.myPlayer) {
				//update character visuals while aiming
				aimVector = Vector2.Normalize(Main.MouseWorld - playerHandPos);
				aimVector = Vector2.Normalize(Vector2.Lerp(Vector2.Normalize(Projectile.velocity), aimVector, 0.6f)); //taken straight from RedLaserBeam, thanks past me!
				if (aimVector != Projectile.velocity) {
					Projectile.netUpdate = true; //update the bow visually to other players when we change aim
				}
				Projectile.velocity = aimVector;
			}
			Projectile.spriteDirection = Projectile.direction;
		}
        public override void PostDraw(Color lightColor) {
            DrawPoints();
        }

        protected virtual void DrawPoints() {
            if (ModContent.GetInstance<tsorcRevampConfig>().ChargeCircleOpacity == 0) return;

            //forces the projectile to be drawn after liquids, and incidentally wires
            if (!Main.instance.DrawCacheProjsOverWiresUI.Contains(Projectile.whoAmI)) Main.instance.DrawCacheProjsOverWiresUI.Add(Projectile.whoAmI);

            int maxPoints = 72;
            int points = (int)(charge * maxPoints) + 1;
            float opacity = (float)ModContent.GetInstance<tsorcRevampConfig>().ChargeCircleOpacity / 200;
            if (pointTexture == null || pointTexture.IsDisposed) pointTexture = ModContent.Request<Texture2D>("tsorcRevamp/Textures/ChargePoint", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Rectangle srect = new(0, 0, pointTexture.Width, pointTexture.Height);
            Vector2 origin = new(2, 2);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < points - 1; i++) {
                Vector2 pos = (Main.MouseScreen + new Vector2(6, 6)) - (Vector2.UnitY * 24).RotatedBy(MathHelper.ToRadians((360 / maxPoints) * i));
                Main.EntitySpriteDraw(pointTexture, pos, srect, Color.White * opacity, 0f, origin, 1f, SpriteEffects.None, 0);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin();

        }


		/// <summary>
		/// Get the value [amount]% of the way from [min] to [max]
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		/// <param name="amount">A percentage (0-1)</param>
		/// <returns></returns>
		protected static float LerpFloat(float min, float max, float amount) {
			float diff = max - min;
			return min + diff * amount;
		}
	}
}
