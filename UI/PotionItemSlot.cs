using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace tsorcRevamp.UI
{
	// This class wraps the vanilla ItemSlot class into a UIElement. The ItemSlot class was made before the UI system was made, so it can't be used normally with UIState. 
	// By wrapping the vanilla ItemSlot class, we can easily use ItemSlot.
	// ItemSlot isn't very modder friendly and operates based on a "Context" number that dictates how the slot behaves when left, right, or shift clicked and the background used when drawn. 
	// If you want more control, you might need to write your own UIElement.
	// I've added basic functionality for validating the item attempting to be placed in the slot via the validItem Func. 
	// See ExamplePersonUI for usage and use the Awesomify chat option of Example Person to see in action.
	internal class PotionItemSlot : UIElement
	{
		private readonly int _context;
		private readonly float _scale;
		internal Func<Item, bool> ValidItemFunc;
		public bool favorite;
		public int index = 0;
		

		public PotionItemSlot(int Index, int context = ItemSlot.Context.BankItem, float scale = 1f)
		{
			_context = context;
			_scale = scale;
			index = Index;
			favorite = false;

			Width.Set(Main.inventoryBack9Texture.Width * scale, 0f);
			Height.Set(Main.inventoryBack9Texture.Height * scale, 0f);
		}

		protected override void DrawSelf(SpriteBatch spriteBatch)
		{
			Item[] PotionItems = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().PotionBagItems;

			if(PotionItems == null)
            {
				PotionItems = new Item[PotionBagUIState.POTION_BAG_SIZE];
			}
			if(PotionItems[index] == null)
            {
				PotionItems[index] = new Item();
				PotionItems[index].SetDefaults(0);
			}

			float oldScale = Main.inventoryScale;
			Main.inventoryScale = _scale;
			Rectangle rectangle = GetDimensions().ToRectangle();

			if (ContainsPoint(Main.MouseScreen) && !PlayerInput.IgnoreMouseInterface)
			{
				Main.LocalPlayer.mouseInterface = true;

				bool valid = false;

				if(ValidItemFunc == null)
                {
					valid = true;
                }
				if (ValidItemFunc(Main.mouseItem))
				{
					valid = true;
				}
				if (Main.mouseItem.type == 0) //You can always pull stuff *out* of the bag if you're holding nothing, even if it's not valid
				{
					valid = true;
				}
				if (Main.mouseItem.type == ModContent.ItemType<Items.PotionBag>()) //No
				{
					valid = false;
				}
				if (valid)
				{
					// Handle handles all the click and hover actions based on the context.
					ItemSlot.Handle(ref PotionItems[index], _context);
				}
			}

            if (favorite)
            {
				PotionItems[index].favorited = true;
            }

			// Draw draws the slot itself and Item. Depending on context, the color will change, as will drawing other things like stack counts.
			ItemSlot.Draw(spriteBatch, ref PotionItems[index], ItemSlot.Context.InventoryAmmo, rectangle.TopLeft());
			Main.inventoryScale = oldScale;
		}
	}
}