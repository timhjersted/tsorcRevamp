using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;
using System;

namespace tsorcRevamp.Projectiles
{

    public class HeavenBall : ModProjectile
    {

        private const string ChainTexturePath = "tsorcRevamp/Projectiles/chain";

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Heaven Ball");
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;

			Projectile.usesLocalNPCImmunity = true; // Used for hit cooldown changes in the ai hook
			Projectile.localNPCHitCooldown = 10; // This facilitates custom hit cooldown logic

			Projectile.aiStyle = ProjAIStyleID.Flail;
			AIType = ProjectileID.FlowerPow;
		}
		public override Color? GetAlpha(Color lightColor)
		{
			return Color.White;
		}

        public override bool PreDrawExtras()
        {
            return false;
        }

        static Texture2D chainTexture = (Texture2D)ModContent.Request<Texture2D>(ChainTexturePath, ReLogic.Content.AssetRequestMode.ImmediateLoad);

		public override bool PreDraw(ref Color lightColor)
		{
			
			//Calculate where to draw it
			Texture2D projectileTexture = TextureAssets.Projectile[Projectile.type].Value;
			Vector2 drawPosition = Projectile.position + new Vector2(Projectile.width, Projectile.height) / 2f + Vector2.UnitY * Projectile.gfxOffY - Main.screenPosition;
			Vector2 drawOrigin = new Vector2(projectileTexture.Width, projectileTexture.Height) / 2f;
			Color drawColor = Projectile.GetAlpha(lightColor);
			drawColor.A = 127;
			drawColor *= 0.5f;
			int launchTimer = (int)Projectile.ai[1];
			if (launchTimer > 5)
				launchTimer = 5;

			SpriteEffects spriteEffects = SpriteEffects.None;
			if (Projectile.spriteDirection == -1)
				spriteEffects = SpriteEffects.FlipHorizontally;

			//Draw the main ball
			Main.EntitySpriteDraw(projectileTexture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, drawOrigin, 1, spriteEffects, 0);

			//Draw the afterimages
			for (float transparancy = 1f; transparancy >= 0f; transparancy -= 0.125f)
			{
				float opacity = 1f - transparancy;
				Vector2 drawAdjustment = Projectile.velocity * -launchTimer * transparancy;
				Main.EntitySpriteDraw(projectileTexture, drawPosition + drawAdjustment, null, drawColor * opacity, Projectile.rotation, drawOrigin, Projectile.scale * 1.15f * MathHelper.Lerp(0.5f, 1f, opacity), spriteEffects, 0);
			}
			

			//Draw the chain
			if (chainTexture == null || chainTexture.IsDisposed)
			{
				chainTexture = (Texture2D)ModContent.Request<Texture2D>(ChainTexturePath, ReLogic.Content.AssetRequestMode.ImmediateLoad);
			}
			var player = Main.player[Projectile.owner];

			Vector2 mountedCenter = player.MountedCenter;

			Vector2 chainDrawPosition = Projectile.Center;
			var remainingVectorToPlayer = mountedCenter - chainDrawPosition;

			float rotation = remainingVectorToPlayer.ToRotation() - MathHelper.PiOver2;

			if (Projectile.alpha == 0)
			{
				int direction = -1;

				if (Projectile.Center.X < mountedCenter.X)
					direction = 1;

				player.itemRotation = (float)Math.Atan2(remainingVectorToPlayer.Y * direction, remainingVectorToPlayer.X * direction);
			}

			//draw the chain
			while (true)
			{
				float length = remainingVectorToPlayer.Length();

				if (length < 25f || float.IsNaN(length))
					break;

				//12 is height of chain image
				chainDrawPosition += remainingVectorToPlayer * 12 / length;
				remainingVectorToPlayer = mountedCenter - chainDrawPosition;

				// Finally, we draw the texture at the coordinates using the lighting information of the tile coordinates of the chain section
				Color color = Lighting.GetColor((int)chainDrawPosition.X / 16, (int)(chainDrawPosition.Y / 16f));
				Main.EntitySpriteDraw(chainTexture, chainDrawPosition - Main.screenPosition, null, color, rotation, chainTexture.Size() * 0.5f, 1f, SpriteEffects.None, 0);
			}


			return false;
		}
		public override void AI()
		{
			Vector2 dropletvector = new Vector2(0, 5);
			// The only reason this code works is because the author read the vanilla code and comprehended it well enough to tack on additional logic.
			if (Main.myPlayer == Projectile.owner && Projectile.ai[0] == 2f && Projectile.ai[1] == 0f)
			{
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, dropletvector, ProjectileID.RainFriendly, Projectile.damage, Projectile.knockBack, Main.myPlayer);
				Projectile.ai[1]++;
			}
		}
	}
}
