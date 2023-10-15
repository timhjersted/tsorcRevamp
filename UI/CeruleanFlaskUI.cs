using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace tsorcRevamp.UI
{
    internal class CeruleanFlaskUIState : UIState
    {
        // For this bar we'll be using a frame texture and then a gradient inside bar, as it's one of the more simpler approaches while still looking decent.
        // Once this is all set up make sure to go and do the required stuff for most UI's in the Mod class.
        private UIElement area;
        public static bool Visible = true;

        public static tsorcRevampConfig ConfigInstance = ModContent.GetInstance<tsorcRevampConfig>();
        public static Vector2 DrawPos = new Vector2(ConfigInstance.CeruleanFlaskPosX, ConfigInstance.CeruleanFlaskPosY);

        public override void OnInitialize()
        {
            // Create a UIElement for all the elements to sit on top of, this simplifies the numbers as nested elements can be positioned relative to the top left corner of this element. 
            // UIElement is invisible and has no padding. You can use a UIPanel if you wish for a background.
            area = new UIElement();
            area.Left.Set(-(ConfigInstance.CeruleanFlaskPosX), 1f); // Place the resource bar to the left of the hearts.
            area.Top.Set(-(ConfigInstance.CeruleanFlaskPosY), 1f); // Placing it just a bit below the top of the screen.
            area.Width.Set(160, 0f); // We will be placing the following 2 UIElements within this 182x60 area.
            area.Height.Set(40, 0f);

            Append(area);
        }

        Texture2D textureFull;
        Texture2D textureEmpty;
        Texture2D textureCharges;
        public override void Draw(SpriteBatch spriteBatch)
        {
            // This prevents drawing unless we are BotC and haven't hidden it
            if (!Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse || ConfigInstance.HideCeruleanFlask)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            var ceruleanPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampCeruleanPlayer>();

            if (player.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                int chargesFrameCount = 13;
                int chargesCurrent = ceruleanPlayer.ceruleanChargesCurrent;
                int chargesMax = ceruleanPlayer.ceruleanChargesMax;
                float chargesPercentage = (float)chargesCurrent / chargesMax;
                chargesPercentage = Utils.Clamp(chargesPercentage, 0f, 1f); // Clamping it to 0-1f so it doesn't go over that.


                UsefulFunctions.EnsureLoaded(ref textureFull, "tsorcRevamp/UI/CeruleanFlask_full");
                UsefulFunctions.EnsureLoaded(ref textureEmpty, "tsorcRevamp/UI/CeruleanFlask_empty");
                UsefulFunctions.EnsureLoaded(ref textureCharges, "tsorcRevamp/UI/CeruleanFlask_charges");

                int frameHeight = textureCharges.Height / chargesFrameCount;
                int frame;
                frame = ceruleanPlayer.ceruleanChargesCurrent;

                int drawFrame = frameHeight * frame;
                Rectangle sourceRectangle = new Rectangle(0, drawFrame, textureCharges.Width, frameHeight);
                Color numbercolor;
                if (ceruleanPlayer.ceruleanChargesCurrent == 0) { numbercolor = Color.LightPink; }
                else { numbercolor = Color.White; }


                int cropAmount = (int)(textureFull.Height * (1 - chargesPercentage));
                Main.spriteBatch.Draw(textureEmpty, new Rectangle(Main.screenWidth - ConfigInstance.CeruleanFlaskPosX + 4, Main.screenHeight - ConfigInstance.CeruleanFlaskPosY, textureFull.Width, textureFull.Height), Color.White);
                Main.spriteBatch.Draw(textureCharges, new Vector2(Main.screenWidth - ConfigInstance.CeruleanFlaskPosX + 4, Main.screenHeight - ConfigInstance.CeruleanFlaskPosY - 20), sourceRectangle, numbercolor, 0, new Vector2(0, 0), 1.3f, SpriteEffects.None, 1);

                //the cropped texture is shorter, so its Y position needs to be offset by the height difference
                Rectangle overlaySourceRectangle = new Rectangle(0, cropAmount, textureFull.Width, textureFull.Height - cropAmount);
                Main.spriteBatch.Draw(textureFull, new Vector2(Main.screenWidth - ConfigInstance.CeruleanFlaskPosX + 4, Main.screenHeight - ConfigInstance.CeruleanFlaskPosY + cropAmount), overlaySourceRectangle, numbercolor, 0, new Vector2(0, 0), 1f, SpriteEffects.None, 1);

            }
            base.Draw(spriteBatch);
        }

        public override void Update(GameTime gameTime)
        {
            if (!Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                return;
            }

            if ((-area.Left.Pixels) != ConfigInstance.CeruleanFlaskPosX)
            {
                area.Left.Pixels = -ConfigInstance.CeruleanFlaskPosX;
            }

            if ((-area.Top.Pixels) != ConfigInstance.CeruleanFlaskPosY)
            {
                area.Top.Pixels = -ConfigInstance.CeruleanFlaskPosY;
            }
        }
    }
}
