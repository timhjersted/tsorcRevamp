using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Melee
{
    // Parent class for thrown spears where hitbox is centered on spear tip
    public abstract class ModdedSpearProjectileThrown : ModProjectile
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
        public override void OnSpawn(IEntitySource source)
        {
            // Since projectile is centered on spear tip for collision hitbox, offset starting
            // projectile center so sprite matches where projectile is initially held.
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Projectile.ModProjectile.Texture);
            }
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            float initialRotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.ToRadians(45f);
            Vector2 spearTipOffset = origin.RotatedBy(initialRotation);
            Projectile.Center -= spearTipOffset;
        }

        public override bool PreDraw(ref Color lightColor)
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
            Vector2 spearTipOffset = origin.RotatedBy(Projectile.rotation);
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY) + spearTipOffset,
                sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.ToRadians(45f); //This makes it rotate to face where it's moving
            //projectile.velocity.Y += (9.8f / 60); //This is its gravity. Comes out to about 0.16 per frame, which is actually really high!!
            Projectile.velocity.Y += 0.1f;

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 90, Projectile.velocity.X * -0.2f, Projectile.velocity.Y * -0.2f, 70, default(Color), 1.3f);
                Main.dust[dust].noGravity = true;
            }
        }

        public override void PostDraw(Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
            Vector2 spearTipOffset = origin.RotatedBy(Projectile.rotation);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + spearTipOffset;

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
