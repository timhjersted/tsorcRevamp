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
		/*
		private int _points;

		private short[] _lineStripIndices;

		private VertexPositionNormalTexture[] _pointList;

		private BasicEffect _eff;
		*/
		protected int ammoType {
			get {
				return (int)Projectile.ai[0];
			}
			set {
				Projectile.ai[0] = value;
			}
		}

        public override void SetStaticDefaults() {
			DisplayName.SetDefault("Charged Bow");
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

			//supposed to be for drawing a charge circle, but i cant get it working
			//i hate primitives
			//Main.QueueMainThreadAction(PreparePrimitives);
		}
		/*
		private void PreparePrimitives() {
			//create a list of vertices in 3d space that represents the endpoints of the lines
			_points = 16;
			double angle = MathHelper.TwoPi / _points;
			_pointList = new VertexPositionNormalTexture[_points + 1];
			_pointList[0] = new VertexPositionNormalTexture(Vector3.Zero, Vector3.Forward, Vector2.One);
			for (int i = 1; i < _points; i++) {
				_pointList[i] = new VertexPositionNormalTexture(
					new Vector3((float)Math.Round(Math.Sin(angle * i), 4), (float)Math.Round(Math.Cos(angle * i), 4), 0.0f),
					Vector3.Forward,
					Main.MouseWorld - Main.LocalPlayer.position
					);
			}

			//create an array for which endpoints should be connected
			//equivalent to [1, 2, 3, ..., n, 1]
			_lineStripIndices = new short[_points + 1];
			for (int i = 0; i < _points; i++) {
				_lineStripIndices[i] = (short)(i + 1);
			}
			_lineStripIndices[_points] = 1;

		}
		*/
		protected abstract void SetStats();

		protected virtual void SpecialBehavior() {
		}

		protected virtual void Animate() {
			Projectile.frame = (int)((Main.projFrames[Projectile.type] - 1) * charge);
			//charge circle
			/*
			GraphicsDevice gd = Main.instance.GraphicsDevice;
			int width = gd.Viewport.Width;
			int height = gd.Viewport.Height;
			Vector2 zoom = Main.GameViewMatrix.Zoom;
			Matrix view = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up) * Matrix.CreateTranslation(width / 2, height / -2, 0f) * Matrix.CreateRotationZ((float)Math.PI) * Matrix.CreateScale(zoom.X, zoom.Y, 1f);
			Matrix projection = Matrix.CreateOrthographic(width, height, 0f, 1000f);

			_eff = new BasicEffect(gd) {
				VertexColorEnabled = true
			};
			_eff.View = view;
			_eff.Projection = projection;
			foreach (EffectPass pass in _eff.CurrentTechnique.Passes) {
				pass.Apply();
			}

			Main.instance.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionNormalTexture>(
				PrimitiveType.LineStrip,
				_pointList,
				0,   // vertex buffer offset to add to each element of the index buffer
				(int)(Math.Ceil(_points * charge)),   // number of vertices to draw
				_lineStripIndices,
				0,   // first index element to read
				(int)(Math.Ceil(_points * charge) + 1)    // number of primitives to draw
			);
			*/
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

		protected void UpdateAim() {
			Projectile.timeLeft = 2;
			Player player = Main.player[Projectile.owner];
			Vector2 playerHandPos = player.RotatedRelativePoint(player.MountedCenter);
			Projectile.Center = new Vector2(playerHandPos.X - player.width, playerHandPos.Y);
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
				Projectile.velocity = aimVector * holdoutOffset;
			}
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
