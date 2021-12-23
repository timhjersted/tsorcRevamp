using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Input;
using System.Reflection;
using Terraria.ModLoader.Config;
using System;

namespace tsorcRevamp.UI
{
	internal class EstusFlaskUIState : UIState
	{
		// For this bar we'll be using a frame texture and then a gradient inside bar, as it's one of the more simpler approaches while still looking decent.
		// Once this is all set up make sure to go and do the required stuff for most UI's in the Mod class.
		private UIText chargesCurrent;
		private UIElement area;
		private UIImage estusFlask_empty;
		private UIImage estusFlask_full;

		public static bool Visible = true;
		int initialTextPosX = 133; //was 60

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

			estusFlask_empty = new UIImage(ModContent.GetTexture("tsorcRevamp/UI/EstusFlask_empty"));
			estusFlask_empty.Left.Set(10, 0f);
			estusFlask_empty.Top.Set(0, 0f);
			estusFlask_empty.Width.Set(64, 0f);
			estusFlask_empty.Height.Set(76, 0f);

			/*estusFlask_full = new UIImage(ModContent.GetTexture("tsorcRevamp/UI/EstusFlask_full"));
			estusFlask_full.Left.Set(0, 0f);
			estusFlask_full.Top.Set(0, 0f);
			estusFlask_full.Width.Set(64, 0f);
			estusFlask_full.Height.Set(76, 0f);*/

			chargesCurrent = new UIText("0", 0.5f, true); // text to show stat
			chargesCurrent.Width.Set(20, 0f);
			chargesCurrent.Height.Set(30, 0f);
			chargesCurrent.Top.Set(50, 0f);
			chargesCurrent.Left.Set(0, 0f);

			area.Append(chargesCurrent);
			area.Append(estusFlask_empty);
			//area.Append(estusFlask_full);

			Append(area);
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			// This prevents drawing unless we are BotC and have reveived the flask from the Emerald Herald
			if (!(Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().ReceivedGift))
			{
				return;
			}

			base.Draw(spriteBatch);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			base.DrawSelf(spriteBatch);

			Player player = Main.LocalPlayer;
			var estusPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>();

			if (player.whoAmI == Main.myPlayer && !Main.gameMenu)
			{

				int chargesCurrent = estusPlayer.estusChargesCurrent;
				int chargesMax = estusPlayer.estusChargesMax;
				float chargesPercentage = (float)chargesCurrent / chargesMax;
				chargesPercentage = Utils.Clamp(chargesPercentage, 0f, 1f); // Clamping it to 0-1f so it doesn't go over that.

				//Main.NewText(chargesPercentage);

				Texture2D textureFull = ModContent.GetTexture("tsorcRevamp/UI/EstusFlask_full");
				//Texture2D textureEmpty = ModContent.GetTexture("tsorcRevamp/UI/EstusFlask_empty");
				//Texture2D textureCharges = ModContent.GetTexture("tsorcRevamp/UI/EstusFlask_charges");
				Rectangle hitbox = estusFlask_empty.GetInnerDimensions().ToRectangle();

				//Rectangle emptyDestination = new Rectangle(textureOrigin.X, textureOrigin.Y, textureEmpty.Width, textureEmpty.Height);
				//Rectangle chargesDestination = new Rectangle(textureOrigin.X, textureOrigin.Y, textureEmpty.Width, textureEmpty.Height);
				//Rectangle fullDestination = new Rectangle(0, 0, textureFull.Height, textureFull.Height);


				//Main.spriteBatch.Draw(textureEmpty, emptyDestination, Color.White);
				//Main.spriteBatch.Draw(textureCharges, chargesDestination, Color.White);
				Main.spriteBatch.Draw(MethodSwaps.Crop(textureFull, new Rectangle(0, (int)(textureFull.Height * (1 - chargesPercentage)), textureFull.Width, textureFull.Height)), new Rectangle(ConfigInstance.EstusFlaskPosX, ConfigInstance.EstusFlaskPosY, textureFull.Width, textureFull.Height), Color.White);

			}
		}
		public override void Update(GameTime gameTime)
		{
			if (!(Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().ReceivedGift))
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

			var modPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>();
			// Setting the text per tick to update and show our resource values.
			chargesCurrent.SetText($"{modPlayer.estusChargesCurrent}");
			base.Update(gameTime);
		}
	}
}
