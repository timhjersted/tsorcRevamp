using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace tsorcRevamp.Projectiles.Swords.Runeterra
{
	public class SteelTempestThrust : ModProjectile
	{
		public int steeltempesthittimer = 0;
		public const int FadeInDuration = 7;
		public const int FadeOutDuration = 4;

		public const int TotalDuration = 32;

		public float CollisionWidth => 10f * Projectile.scale;

		public int Timer
		{
			get => (int)Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}
		public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 6;
			DisplayName.SetDefault("Steel Tempest Thrust");
        }


		public override void SetDefaults()
		{
			Projectile.Size = new Vector2(18); 
			Projectile.aiStyle = -1; 
			Projectile.friendly = true;
			Projectile.penetrate = -1;
			Projectile.tileCollide = false;
			Projectile.scale = 0.7f;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.ownerHitCheck = true;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 20;
			Projectile.extraUpdates = 1; 
			Projectile.timeLeft = 360;
			Projectile.hide = true;
			Projectile.width = 110;
			Projectile.height = 104;
		}
		public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            Projectile.damage = (int)(player.GetWeaponDamage(player.HeldItem) * 2f);
        }
		public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            Timer += 1;
			if (Timer >= TotalDuration)
			{
				Projectile.Kill();
				steeltempesthittimer = 0;
				return;
			}
			else
			{
				player.heldProj = Projectile.whoAmI;
			}
			Projectile.Opacity = Utils.GetLerpValue(0f, FadeInDuration, Timer, clamped: true) * Utils.GetLerpValue(TotalDuration, TotalDuration - FadeOutDuration, Timer, clamped: true);

			Vector2 playerCenter = player.RotatedRelativePoint(player.MountedCenter, reverseRotation: false, addGfxOffY: false);
			Projectile.Center = playerCenter + Projectile.velocity * (Timer - 1f);

			Projectile.spriteDirection = (Vector2.Dot(Projectile.velocity, Vector2.UnitX) >= 0f).ToDirectionInt();

			Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 - MathHelper.PiOver4 * Projectile.spriteDirection;

			SetVisualOffsets();
			Visuals();
		}

		private void SetVisualOffsets()
		{
			const int HalfSpriteWidth = 158 / 2;
			const int HalfSpriteHeight = 148 / 2;

			int HalfProjWidth = Projectile.width / 2;
			int HalfProjHeight = Projectile.height / 2;

			DrawOriginOffsetX = 0;
			DrawOffsetX = -(HalfSpriteWidth - HalfProjWidth);
			DrawOriginOffsetY = -(HalfSpriteHeight - HalfProjHeight);
		}

		public override bool ShouldUpdatePosition()
		{
			return false;
		}

		public override void CutTiles()
		{
			DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
			Vector2 start = Projectile.Center;
			Vector2 end = start + Projectile.velocity.SafeNormalize(-Vector2.UnitY) * 10f;
			Utils.PlotTileLine(start, end, CollisionWidth, DelegateMethods.CutTiles);
		}

		public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
		{
			Vector2 start = Projectile.Center;
			Vector2 end = start + Projectile.velocity * 6f;
			float collisionPoint = 0f;
			return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, CollisionWidth, ref collisionPoint);
		}
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
			Player player = Main.player[Projectile.owner];
            if (steeltempesthittimer == 0)
            {
				player.GetModPlayer<tsorcRevampPlayer>().steeltempest += 1;
				steeltempesthittimer = 1;
				if (Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().steeltempest == 2)
                {
					SoundEngine.PlaySound(SoundID.Item71, player.Center);
                }
            }
        }
        private void Visuals()
        {
			Projectile.frame = (int)((Timer / 28f) * 6f);
			if (Timer > 28f)
			{
				Projectile.frame = 0;
			}
            /*float frameSpeed = 7f;

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
        }
    }
}
