using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ModLoader;

namespace tsorcRevamp.UI
{
	internal class DarkSoulCounterUIState: UIState
	{
		// For this bar we'll be using a frame texture and then a gradient inside bar, as it's one of the more simpler approaches while still looking decent.
		// Once this is all set up make sure to go and do the required stuff for most UI's in the Mod class.
		private UIText soulQuantityText;
		private UIElement area;
		private UIImage counterFrame;

		public static bool Visible = true;

		public override void OnInitialize()
		{
			// Create a UIElement for all the elements to sit on top of, this simplifies the numbers as nested elements can be positioned relative to the top left corner of this element. 
			// UIElement is invisible and has no padding. You can use a UIPanel if you wish for a background.
			area = new UIElement();
			area.Left.Set(-area.Width.Pixels - 220, 1f); // Place the resource bar to the left of the hearts.
			area.Top.Set(-area.Height.Pixels - 70, 1f); // Placing it just a bit below the top of the screen.
			area.Width.Set(200, 0f); // We will be placing the following 2 UIElements within this 182x60 area.
			area.Height.Set(50, 0f);

			counterFrame = new UIImage(ModContent.GetTexture("tsorcRevamp/UI/DarkSoulCounterFrame"));
			counterFrame.Left.Set(0, 0f);
			counterFrame.Top.Set(0, 0f);
			counterFrame.Width.Set(0, 0f);
			counterFrame.Height.Set(0, 0f);
			area.Append(counterFrame);

			soulQuantityText = new UIText("0", 0.5f, true); // text to show stat
			soulQuantityText.Width.Set(0, 0f);
			soulQuantityText.Height.Set(0, 0f);
			soulQuantityText.Top.Set(17, 0f);
			soulQuantityText.Left.Set(90, 0f);
			area.Append(soulQuantityText);

			Append(area);
		}

		public override void Update(GameTime gameTime)
		{
			var modPlayer = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>();
			var item = ModContent.ItemType<Items.DarkSoul>();
			// Setting the text per tick to update and show our DS values.
			soulQuantityText.SetText($"[i:{item}]  [c/D3D3D3:{modPlayer.darkSoulQuantity}]");
			base.Update(gameTime);
		}
	}
}
