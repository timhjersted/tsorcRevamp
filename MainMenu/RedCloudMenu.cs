using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.MainMenu
{
    public class RedCloudMenu : ModMenu
    {
        private const int AnimationFrameCount = 36;
        private const int AnimationFrameWidth = 960;
        private const int AnimationFrameHeight = 512;
        private const int AnimationAtlasColumns = 4;
        private const int AnimationFramesPerAtlas = 20;
        private const float AnimationSecondsPerFrame = 0.11f;

        private Asset<Texture2D> logo;
        private Asset<Texture2D>[] backgroundAtlases;

        public override string DisplayName => "The Story of Red Cloud";

        public override Asset<Texture2D> Logo => logo ??= ModContent.Request<Texture2D>("tsorcRevamp/MainMenu/tsorc_logo", AssetRequestMode.ImmediateLoad);

        public override void Load()
        {
            if (!Main.dedServ)
            {
                backgroundAtlases = new[]
                {
                    ModContent.Request<Texture2D>("tsorcRevamp/MainMenu/praise_the_sun_background_atlas_0", AssetRequestMode.ImmediateLoad),
                    ModContent.Request<Texture2D>("tsorcRevamp/MainMenu/praise_the_sun_background_atlas_1", AssetRequestMode.ImmediateLoad)
                };
            }
        }

        public override void Unload()
        {
            logo = null;
            backgroundAtlases = null;
        }

        public override bool PreDrawLogo(SpriteBatch spriteBatch, ref Vector2 logoDrawCenter, ref float logoRotation, ref float logoScale, ref Color drawColor)
        {
            DrawMenuBackground(spriteBatch);

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
            if (backgroundAtlases == null)
            {
                return;
            }

            int frame = (int)(Main.GlobalTimeWrappedHourly / AnimationSecondsPerFrame) % AnimationFrameCount;
            int atlasIndex = frame / AnimationFramesPerAtlas;
            int localFrame = frame % AnimationFramesPerAtlas;
            Asset<Texture2D> atlas = backgroundAtlases[atlasIndex];
            if (atlas?.IsLoaded != true)
            {
                return;
            }

            Texture2D texture = atlas.Value;
            Rectangle source = new Rectangle(
                localFrame % AnimationAtlasColumns * AnimationFrameWidth,
                localFrame / AnimationAtlasColumns * AnimationFrameHeight,
                AnimationFrameWidth,
                AnimationFrameHeight);

            float scale = MathHelper.Max((float)Main.screenWidth / AnimationFrameWidth, (float)Main.screenHeight / AnimationFrameHeight);
            Vector2 size = new Vector2(AnimationFrameWidth, AnimationFrameHeight) * scale;
            Vector2 position = new Vector2((Main.screenWidth - size.X) * 0.5f, (Main.screenHeight - size.Y) * 0.5f);

            spriteBatch.Draw(texture, position, source, Color.White, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }
    }
}
