using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class LonginusThrown : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = -1;
            Projectile.hostile = false;
            Projectile.friendly = true;
            Projectile.height = 30;
            Projectile.light = 0.7f;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.scale = 1.2f;
            Projectile.width = 30;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 300;
            Projectile.extraUpdates = 1;
        }

        public static Texture2D texture;
        public override bool PreDraw(Player player, ref Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            if (Projectile.ai[0] == 1)
            {
                ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.RedDye), Main.LocalPlayer);
                data.Apply(null);
            }

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture);
            }

            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }

        public override void ModifyDamageHitbox(ref Rectangle hitbox)
        {
            // Move damage hitbox to where spear tip is
            double spearTipOffsetX = 185 * Math.Cos(Projectile.velocity.ToRotation());
            double spearTipOffsetY = 185 * Math.Sin(Projectile.velocity.ToRotation());
            hitbox.Offset((int)spearTipOffsetX, (int)spearTipOffsetY);
        }

		public override void CutTiles()
		{
			// Move vine/pot breaking to where spear tip is
            Vector2 spearTipOffsetStarting = new Vector2(170 * MathF.Cos(Projectile.velocity.ToRotation()), 170 * MathF.Sin(Projectile.velocity.ToRotation()));
            Vector2 spearTipOffsetEnding = new Vector2(200 * MathF.Cos(Projectile.velocity.ToRotation()), 200 * MathF.Sin(Projectile.velocity.ToRotation()));
			Utils.PlotTileLine(Projectile.Center + spearTipOffsetStarting, Projectile.Center + spearTipOffsetEnding, 70, DelegateMethods.CutTiles);
		}

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.ToRadians(45f); //This makes it rotate to face where it's moving
            //projectile.velocity.Y += (9.8f / 60); //This is its gravity. Comes out to about 0.16 per frame, which is actually really high!!
            Projectile.velocity.Y += 0.1f;

            if (Main.rand.NextBool(3))
            {
                Vector2 spearTipOffset = new Vector2(185 * MathF.Cos(Projectile.velocity.ToRotation()), 185 * MathF.Sin(Projectile.velocity.ToRotation()));
                int dust = Dust.NewDust(Projectile.position + spearTipOffset, Projectile.width, Projectile.height, 90, Projectile.velocity.X * -0.2f, Projectile.velocity.Y * -0.2f, 70, default(Color), 1.3f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[0] == 1)
            {
                Vector2 spearTipOffset = new Vector2(185 * MathF.Cos(Projectile.velocity.ToRotation()), 185 * MathF.Sin(Projectile.velocity.ToRotation()));

                for (int i = 0; i < 150; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Circular(1f, 1f).SafeNormalize(Vector2.UnitX);
                    float speed = Main.rand.NextFloat(5.5f, 19f);

                    int dust1 = Dust.NewDust(Projectile.position + spearTipOffset, Projectile.width, Projectile.height, 90, 0f, 0f, 70, default, 1.55f);
                    Main.dust[dust1].velocity = direction * speed;
                    Main.dust[dust1].noGravity = true;

                    int dust2 = Dust.NewDust(Projectile.position + spearTipOffset, Projectile.width, Projectile.height, 219, 0f, 0f, 70, default, 1.95f);
                    Main.dust[dust2].velocity = direction * (speed * 1.2f);
                    Main.dust[dust2].noGravity = true;
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

                Projectile.penetrate = 15;
                Vector2 oldCenter = Projectile.Center;
                Projectile.width = 320;
                Projectile.height = 320;
                Projectile.position = oldCenter - new Vector2(Projectile.width / 2f, Projectile.height / 2f);
                Projectile.damage /= 2;
                Projectile.Damage();
            }
        }

        public override void PostDraw(Player player, Color lightColor)/* tModPorter Replace 'Main.player[Projectile.owner]' with 'player'. */
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f); 
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 1; i <= 5; i++)  
            {
                Vector2 offset = Projectile.velocity * -i * 1.2f;  
                float alpha = 0.4f * (1f - (i / 6f));  

                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    null,
                    lightColor * alpha,  
                    Projectile.rotation,
                    origin,
                    Projectile.scale,
                    SpriteEffects.None,
                    0
                );
            }
        }

        public override bool PreKill(int timeleft)
        {
            Projectile.type = ProjectileID.WoodenArrowHostile;

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 219, 0, 0, 0, default, 1.4f);
            }
            return true;
        }
    }
}