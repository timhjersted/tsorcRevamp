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
        public static Vector2 DrawPos = new Vector2(ConfigInstance.EstusFlaskPosX, ConfigInstance.EstusFlaskPosY);

        public override void OnInitialize()
        {
            // Create a UIElement for all the elements to sit on top of, this simplifies the numbers as nested elements can be positioned relative to the top left corner of this element. 
            // UIElement is invisible and has no padding. You can use a UIPanel if you wish for a background.
            area = new UIElement();
            area.Left.Set(-(ConfigInstance.EstusFlaskPosX), 1f); // Place the resource bar to the left of the hearts.
            area.Top.Set(-(ConfigInstance.EstusFlaskPosY), 1f); // Placing it just a bit below the top of the screen.
            area.Width.Set(160, 0f); // We will be placing the following 2 UIElements within this 182x60 area.
            area.Height.Set(40, 0f);

            Append(area);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // This prevents drawing unless we are BotC and have received the flask from the Emerald Herald
            if (!Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            var estusPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>();

            if (player.whoAmI == Main.myPlayer && !Main.gameMenu)
            {
                int chargesFrameCount = 7;
                int chargesCurrent = estusPlayer.estusChargesCurrent;
                int chargesMax = estusPlayer.estusChargesMax;
                float chargesPercentage = (float)chargesCurrent / chargesMax;
                chargesPercentage = Utils.Clamp(chargesPercentage, 0f, 1f); // Clamping it to 0-1f so it doesn't go over that.

                Texture2D textureFull = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/UI/EstusFlask_full");
                Texture2D textureEmpty = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/UI/EstusFlask_empty");
                Texture2D textureCharges = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/UI/EstusFlask_charges");

                int frameHeight = textureCharges.Height / chargesFrameCount;
                int frame;
                if (estusPlayer.estusChargesCurrent == 6) { frame = 6; } //Don't think there will be more than 5 flask charges but 6 just in case
                else if (estusPlayer.estusChargesCurrent == 5) { frame = 5; }
                else if (estusPlayer.estusChargesCurrent == 4) { frame = 4; }
                else if (estusPlayer.estusChargesCurrent == 3) { frame = 3; }
                else if (estusPlayer.estusChargesCurrent == 2) { frame = 2; }
                else if (estusPlayer.estusChargesCurrent == 1) { frame = 1; }
                else { frame = 0; }

                int drawFrame = frameHeight * frame;
                Rectangle sourceRectangle = new Rectangle(0, drawFrame, textureCharges.Width, frameHeight);
                Color numbercolor;
                if (estusPlayer.estusChargesCurrent == 0) { numbercolor = Color.LightPink; }
                else { numbercolor = Color.White; }


                int cropAmount = (int)(textureFull.Height * (1 - chargesPercentage));
                Texture2D croppedTextureFull = UsefulFunctions.Crop(textureFull, new Rectangle(0, cropAmount, textureFull.Width, textureFull.Height));
                Main.spriteBatch.Draw(textureEmpty, new Rectangle(Main.screenWidth - ConfigInstance.EstusFlaskPosX + 4, Main.screenHeight - ConfigInstance.EstusFlaskPosY, textureFull.Width, textureFull.Height), Color.White);
                Main.spriteBatch.Draw(textureCharges, new Vector2(Main.screenWidth - ConfigInstance.EstusFlaskPosX + 4, Main.screenHeight - ConfigInstance.EstusFlaskPosY - 20), sourceRectangle, numbercolor, 0, new Vector2(0, 0), 1.3f, SpriteEffects.None, 1);

                //the cropped texture is shorter, so its Y position needs to be offset by the height difference
                Main.spriteBatch.Draw(croppedTextureFull, new Rectangle(Main.screenWidth - ConfigInstance.EstusFlaskPosX + 4, Main.screenHeight + cropAmount - ConfigInstance.EstusFlaskPosY, textureFull.Width, textureFull.Height), Color.White);
            }
            base.Draw(spriteBatch);
        }

        public override void Update(GameTime gameTime)
        {
            if (!Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse )
            {
                return;
            }

            if ((-area.Left.Pixels) != ConfigInstance.EstusFlaskPosX)
            {
                area.Left.Pixels = -ConfigInstance.EstusFlaskPosX;
            }

            if ((-area.Top.Pixels) != ConfigInstance.EstusFlaskPosY)
            {
                area.Top.Pixels = -ConfigInstance.EstusFlaskPosY;
            }
        }
    }
}
