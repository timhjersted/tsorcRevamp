using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;

namespace tsorcRevamp.Projectiles.Summon.Whips
{
	public abstract class ModdedWhip : ModProjectile
	{
		public abstract int WhipWidth { get; }
		public abstract int WhipHeight { get; }
		public abstract int WhipSegments { get; }
		public abstract float WhipRangeMult { get; }
		public abstract int DustId {  get; }
		public abstract int DustWidth { get; }
		public abstract int DustHeight { get; }
		public abstract Color DustColor { get; }
		public abstract float DustScale { get; }
        public abstract float MaxChargeTime { get; } //Updates twice a tick so /2 this for the actual amount of ticks this needs
        public abstract Vector2 WhipTipBase { get; }
        public abstract float MaxChargeDmgDivisor { get; }

        public abstract int WhipDebuffId { get; }
        public abstract int WhipDebuffDuration { get; }
        public abstract float WhipMultihitPenalty { get; }
        public abstract Color WhipLineColor { get; }
        public virtual void CustomDust(List<Vector2> points) { } //for whips that release more dust
        public virtual void CustomModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) { } //for special whips
        public virtual void CustomOnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) { } //for special whips
        public override void SetStaticDefaults()
        {
            // This makes the projectile use whip collision detection and allows flasks to be applied to it.
            ProjectileID.Sets.IsAWhip[Type] = true;
        }
        public override void SetDefaults()
        {
            Projectile.width = WhipWidth;
            Projectile.height = WhipHeight;
            Projectile.WhipSettings.Segments = WhipSegments;
            Projectile.WhipSettings.RangeMultiplier = WhipRangeMult; //only thing affecting the actual whip range

            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            Projectile.tileCollide = false;
            Projectile.ownerHitCheck = true; // This prevents the projectile from hitting through solid tiles.
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;

            Projectile.GetGlobalProjectile<tsorcGlobalProjectile>().ModdedWhip = true;
            if (MaxChargeTime > 0)
            {
                Projectile.GetGlobalProjectile<tsorcGlobalProjectile>().ChargedWhip = true;
            }
        }

        public float Timer
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public float ChargeTime
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }
        private float ChargeTimer2 = 0;
        public override void AI()
		{
			Player owner = Main.player[Projectile.owner];
			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2; // Without PiOver2, the rotation would be off by 90 degrees counterclockwise.

			Projectile.Center = Main.GetPlayerArmPosition(Projectile) + Projectile.velocity * Timer;
			// Vanilla uses Vector2.Dot(Projectile.velocity, Vector2.UnitX) here. Dot Product returns the difference between two vectors, 0 meaning they are perpendicular.
			// However, the use of UnitX basically turns it into a more complicated way of checking if the projectile's velocity is above or equal to zero on the X axis.
			Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;

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
            Dust.NewDust(Projectile.WhipPointsForCollision[points.Count - 1], DustWidth, DustHeight, DustId, 0f, 0f, 150, DustColor, DustScale);
			CustomDust(points);

            if (Timer == swingTime / 2)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Item/SummonerWhipcrack") with { Volume = tsorcGlobalProjectile.WhipVolume, PitchVariance = tsorcGlobalProjectile.WhipPitch }, points[points.Count - 1]);
                if (owner.GetModPlayer<tsorcRevampPlayer>().Goredrinker && !owner.HasBuff(ModContent.BuffType<GoredrinkerCooldown>()) && owner.GetModPlayer<tsorcRevampPlayer>().GoredrinkerReady)
                {
                    owner.GetModPlayer<tsorcRevampPlayer>().GoredrinkerSwung = true;
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/GoredrinkerSwing") with { Volume = 1f }, owner.Center);
                }
            }
            Main.NewText(
            Projectile.GetGlobalProjectile<tsorcGlobalProjectile>().ModdedWhip);
        }

        // This method handles a charging mechanic.
        // Returns true if fully charged or charging is disabled
        private bool Charge(Player owner)
        {
            // Like other whips, this whip updates twice per frame (Projectile.extraUpdates = 1), so 120 is equal to 1 second.
            if (!owner.channel || ChargeTime >= MaxChargeTime)
            {
                return true; // finished charging
            }

            ChargeTime += 1f * (owner.GetTotalAttackSpeed(DamageClass.SummonMeleeSpeed));
            ChargeTimer2 += 1f * (owner.GetTotalAttackSpeed(DamageClass.SummonMeleeSpeed));

            if (ChargeTimer2 >= (MaxChargeTime / 15))
            {
                Projectile.WhipSettings.Segments++;
                Projectile.WhipSettings.RangeMultiplier += 0.1f;
                ChargeTimer2 = 0;
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
            Vector2 WhipTip = WhipTipBase * Main.player[Main.myPlayer].whipRangeMultiplier * Projectile.WhipSettings.RangeMultiplier * player.GetModPlayer<tsorcRevampPlayer>().WhipCritHitboxSize;
            List<Vector2> points = Projectile.WhipPointsForCollision;
			if (MaxChargeTime > 0)
            {
                modifiers.SourceDamage *= MathF.Max(ChargeTime / (MaxChargeTime / MaxChargeDmgDivisor), 1f);
                modifiers.Knockback *= MathF.Max(ChargeTime / (MaxChargeTime / MaxChargeDmgDivisor), 1f);
            }
            if (Utils.CenteredRectangle(Projectile.WhipPointsForCollision[points.Count - 2], WhipTip).Intersects(target.Hitbox) | Utils.CenteredRectangle(Projectile.WhipPointsForCollision[points.Count - 1], WhipTip).Intersects(target.Hitbox))
            {
				modifiers.SetCrit();
            }
			CustomModifyHitNPC(target, ref modifiers);
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
		{
			target.AddBuff(WhipDebuffId, (int)(WhipDebuffDuration * 60 * Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().SummonTagDuration));
			Main.player[Projectile.owner].MinionAttackTargetNPC = target.whoAmI;
			Projectile.damage = (int)(Projectile.damage * WhipMultihitPenalty); // Multihit penalty. Decrease the damage the more enemies the whip hits.
			CustomOnHitNPC(target, hit, damageDone);
        }

        // This method draws a line between all points of the whip, in case there's empty space between the sprites.
        public void DrawLine(List<Vector2> list)
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
				Color color = Lighting.GetColor(element.ToTileCoordinates(), WhipLineColor);
				Vector2 scale = new Vector2(1, (diff.Length() + 2) / frame.Height);

				Main.EntitySpriteDraw(texture, pos - Main.screenPosition, frame, color, rotation, origin, scale, SpriteEffects.None, 0);

				pos += diff;
			}
		}
	}
}
