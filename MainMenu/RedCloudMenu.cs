using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.MainMenu
{
    public class RedCloudMenu : ModMenu
    {
        private Asset<Texture2D> logo;
        private Asset<Texture2D> background;

        public override string DisplayName => "The Story of Red Cloud";

        public override Asset<Texture2D> Logo => logo ??= ModContent.Request<Texture2D>("tsorcRevamp/MainMenu/tsorc_logo", AssetRequestMode.ImmediateLoad);

        public override void Load()
        {
            if (!Main.dedServ)
            {
                background = ModContent.Request<Texture2D>("tsorcRevamp/MainMenu/menu-background", AssetRequestMode.ImmediateLoad);
            }
        }

        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
        {
            //DrawMenuBackground(spriteBatch);

            Texture2D logoTexture = Logo.Value;
            float maxLogoWidth = Main.screenWidth * 0.58f;
            float maxLogoHeight = Main.screenHeight * 0.42f;
            logoScale = MathHelper.Min(maxLogoWidth / logoTexture.Width, maxLogoHeight / logoTexture.Height);
            logoRotation = 0f;
            logoDrawCenter = new Vector2(Main.screenWidth * 0.5f, Main.screenHeight * 0.18f);

            return true;
        }

        private void DrawMenuBackground(SpriteBatch spriteBatch)
        {
            if (background?.IsLoaded != true)
            {
                return;
            }

            Texture2D texture = background.Value;
            float scale = MathHelper.Max((float)Main.screenWidth / texture.Width, (float)Main.screenHeight / texture.Height);
            Vector2 size = texture.Size() * scale;
            Vector2 position = new Vector2((Main.screenWidth - size.X) * 0.5f, (Main.screenHeight - size.Y) * 0.5f);

            spriteBatch.Draw(texture, position, null, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
