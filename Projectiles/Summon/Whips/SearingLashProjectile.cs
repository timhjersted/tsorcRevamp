using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Summon.Whips
{
	public class SearingLashProjectile : ModProjectile
	{
        public override void SetStaticDefaults()
		{
			// This makes the projectile use whip collision detection and allows flasks to be applied to it.
			ProjectileID.Sets.IsAWhip[Type] = true;
		}

		public override void SetDefaults()
		{
			Projectile.width = 12;
			Projectile.height = 38;
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.DamageType = DamageClass.SummonMeleeSpeed;
			Projectile.tileCollide = false;
			Projectile.ownerHitCheck = true; // This prevents the projectile from hitting through solid tiles.
			Projectile.extraUpdates = 1;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
			Projectile.WhipSettings.Segments = 20;
			Projectile.WhipSettings.RangeMultiplier = 1.32f; //only thing affecting the actual whip range
		}

		private float Timer
		{
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		private float ChargeTime
		{
			get => Projectile.ai[1];
			set => Projectile.ai[1] = value;
		}

		public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2; // Without PiOver2, the rotation would be off by 90 degrees counterclockwise.

			Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * Timer;
			// Vanilla uses Vector2.Dot(Projectile.velocity, Vector2.UnitX) here. Dot Product returns the difference between two vectors, 0 meaning they are perpendicular.
			// However, the use of UnitX basically turns it into a more complicated way of checking if the projectile's velocity is above or equal to zero on the X axis.
			Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;

			// remove these 3 lines if you don't want the charging mechanic
			if (!Charge(owner))
			{
				return; // timer doesn't update while charging, freezing the animation at the start.
			}


			Timer++;

			float swingTime = owner.itemAnimationMax * Projectile.MaxUpdates;
			if (Timer >= swingTime || owner.itemAnimation <= 0)
			{
				Projectile.Kill();
				return;
			}

			owner.heldProj = Projectile.whoAmI;
			// Plays a whipcrack sound at the tip of the whip.
			List<Vector2> points = Projectile.WhipPointsForCollision;
			Projectile.FillWhipControlPoints(Projectile, points);
			Dust.NewDust(Projectile.WhipPointsForCollision[points.Count - 1], 10, 10, DustID.Flare, 0f, 0f, 150, default(Color), 1f);
			Dust.NewDust(Projectile.WhipPointsForCollision[points.Count - 1], 10, 10, 25, 0f, 0f, 150, default(Color), 1f);
			if (Timer == swingTime / 2)
			{
				SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Item/SummonerWhipcrack") with { Volume = 0.6f, PitchVariance = 0.3f }, points[points.Count - 1]);
			}
		}

		// This method handles a charging mechanic.
		// If you remove this, also remove Item.channel = true from the item's SetDefaults.
		// Returns true if fully charged
		//Causes sound error if removed idk why
		private bool Charge(Player owner)
		{
			// Like other whips, this whip updates twice per frame (Projectile.extraUpdates = 1), so 120 is equal to 1 second.
			if (!owner.channel || ChargeTime >= 120)
			{
				return true; // finished charging
			}

			ChargeTime++;

			if (ChargeTime % 12 == 0) // 1 segment and 6% range per 12 tick of charge.
			{
				Projectile.WhipSettings.RangeMultiplier += 0.15f;
				Projectile.WhipSettings.Segments++;
			}

			owner = Main.player[Projectile.owner];
			Vector2 mountedCenter = owner.MountedCenter;
			Vector2 unitVectorTowardsMouse = mountedCenter.DirectionTo(Main.MouseWorld).SafeNormalize(Vector2.UnitX * owner.direction);
			owner.ChangeDir((unitVectorTowardsMouse.X > 0f) ? 1 : (-1));
			Projectile.velocity = unitVectorTowardsMouse * 4;

			// Reset the animation and item timer while charging.
			owner.itemAnimation = owner.itemAnimationMax;
			owner.itemTime = owner.itemTimeMax;

			return false; // still charging
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player player = Main.player[Main.myPlayer];
			modifiers.SourceDamage *= MathF.Max(ChargeTime / 45f, 1f);
            modifiers.Knockback *= MathF.Max(ChargeTime / 45f, 1f);
            Vector2 WhipTip = new Vector2(10, 12) * Main.player[Main.myPlayer].whipRangeMultiplier * Projectile.WhipSettings.RangeMultiplier * player.GetModPlayer<tsorcRevampPlayer>().WhipCritHitboxSize;
            List<Vector2> points = Projectile.WhipPointsForCollision;
            if (Utils.CenteredRectangle(Projectile.WhipPointsForCollision[points.Count - 2], WhipTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(Projectile.WhipPointsForCollision[points.Count - 1], WhipTip).Intersects(target.Hitbox))
            {
                modifiers.SetCrit();
            }
        }


        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>();
            modPlayer.SearingLashStacks = ChargeTime / 40 + 1;
            target.AddBuff(ModContent.BuffType<Buffs.Summon.WhipDebuffs.SearingLashDebuff>(), (int)(modPlayer.SearingLashStacks * 150 * modPlayer.SummonTagDuration));
			target.AddBuff(BuffID.OnFire, (int)(modPlayer.SearingLashStacks * 150));
			player.MinionAttackTargetNPC = target.whoAmI;
			Projectile.damage = (int)(Projectile.damage * 0.7f); // Multihit penalty. Decrease the damage the more enemies the whip hits.
		}

		// This method draws a line between all points of the whip, in case there's empty space between the sprites.
		private void DrawLine(List<Vector2> list)
		{
			Texture2D texture = TextureAssets.FishingLine.Value;
			Rectangle frame = texture.Frame();
			Vector2 origin = new Vector2(frame.Width / 2, 2);

			Vector2 pos = list[0];
			for (int i = 0; i < list.Count - 1; i++)
			{
				Vector2 element = list[i];
				Vector2 diff = list[i + 1] - element;

				float rotation = diff.ToRotation() - MathHelper.PiOver2;
				Color color = Lighting.GetColor(element.ToTileCoordinates(), Color.OrangeRed);
				Vector2 scale = new Vector2(1, (diff.Length() + 2) / frame.Height);

				Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, SpriteEffects.None, 0);

				pos += diff;
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			List<Vector2> list = new List<Vector2>();
			Projectile.FillWhipControlPoints(Projectile, list);

			DrawLine(list);

			//Main.DrawWhip_WhipBland(Projectile, list);
			// The code below is for custom drawing.
			// If you don't want that, you can remove it all and instead call one of vanilla's DrawWhip methods, like above.
			// However, you must adhere to how they draw if you do.

			SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

			Main.instance.LoadProjectile(Type);
			Texture2D texture = TextureAssets.Projectile[Type].Value;

			Vector2 pos = list[0];

			for (int i = 0; i < list.Count - 1; i++)
			{
				// These two values are set to suit this projectile's sprite, but won't necessarily work for your own.
				// You can change them if they don't!
				Rectangle frame = new Rectangle(0, 0, 12, 40);
				Vector2 origin = new Vector2(4, 15);
				float scale = 1;

				// These statements determine what part of the spritesheet to draw for the current segment.
				// They can also be changed to suit your sprite.
				if (i == list.Count - 2)
				{
					frame.Y = 60;
					frame.Height = 12;

					// For a more impactful look, this scales the tip of the whip up when fully extended, and down when curled up.
					Projectile.GetWhipSettings(Projectile, out float timeToFlyOut, out int _, out float _);
					float t = Timer / timeToFlyOut;
					scale = MathHelper.Lerp(0.5f, 1.5f, Utils.GetLerpValue(0.1f, 0.7f, t, true) * Utils.GetLerpValue(0.9f, 0.7f, t, true));
				}
				else if (i > 0)
				{
					frame.Y = 40;
					frame.Height = 20;
				}

				Vector2 element = list[i];
				Vector2 diff = list[i + 1] - element;

				float rotation = diff.ToRotation() - MathHelper.PiOver2; // This projectile's sprite faces down, so PiOver2 is used to correct rotation.
				Color color = Lighting.GetColor(element.ToTileCoordinates());

				Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, flip, 0);

				pos += diff;
			}
			return false;
		}
	}
}
