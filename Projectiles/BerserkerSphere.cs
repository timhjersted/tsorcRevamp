using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;

namespace tsorcRevamp.Projectiles
{

    public class BerserkerSphere : ModProjectile
    {


        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Berserker Sphere");
        }

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true; // Used for hit cooldown changes in the ai hook
            Projectile.localNPCHitCooldown = 10; // This facilitates custom hit cooldown logic

            Projectile.aiStyle = ProjAIStyleID.Flail;
            AIType = ProjectileID.TheDaoofPow;
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return Color.White;
        }

		public override bool PreDraw(ref Color lightColor)
		{
			Projectile.type = ModContent.ProjectileType<BerserkerSphere>();

			// This code handles the after images.
			if (Projectile.ai[0] == 1f)
			{
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

				for (float transparancy = 1f; transparancy >= 0f; transparancy -= 0.125f)
				{
					float opacity = 1f - transparancy;
					Vector2 drawAdjustment = Projectile.velocity * -launchTimer * transparancy;
					Main.EntitySpriteDraw(projectileTexture, drawPosition + drawAdjustment, null, drawColor * opacity, Projectile.rotation, drawOrigin, Projectile.scale * 1.15f * MathHelper.Lerp(0.5f, 1f, opacity), spriteEffects, 0);
				}
			}

			return base.PreDraw(ref lightColor);
		}

		// Another thing that won't automatically be inherited by using Projectile.aiStyle and AIType are effects that happen when the projectile hits something. Here we see the code responcible for applying the OnFire debuff to players and enemies.
		public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
		{
			if (Main.rand.NextBool(2))
			{
				target.AddBuff(Mod.Find<ModBuff>("DarkInferno").Type, 300, false);
			}
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			if (Main.rand.NextBool(4))
			{
				target.AddBuff(Mod.Find<ModBuff>("DarkInferno").Type, 180, quiet: false);
			}
		}

		// Finally, you can slightly customize the AI if you read and understand the vanilla aiStyle source code. You can't customize the range, retract speeds, or anything else. If you need to customize those things, you'll need to follow ExampleAdvancedFlailProjectile. This example spawns a Grenade right when the flail starts to retract. 
		public override void AI()
		{
			// The only reason this code works is because the author read the vanilla code and comprehended it well enough to tack on additional logic.
			if (Main.myPlayer == Projectile.owner && Projectile.ai[0] == 2f && Projectile.ai[1] == 0f)
			{
				Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ProjectileID.DeathSickle, Projectile.damage, Projectile.knockBack, Main.myPlayer);
				Projectile.ai[1]++;
			}
		}
	}
}
