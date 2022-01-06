using tsorcRevamp.UI;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;


namespace tsorcRevamp.UI
{
    class PotionBagUIState : UIState
    {
        public UIPanel PotionBagUI;
        //public tsorcUIHoverTextButton ButtonSetSpawn;
        //public tsorcUIHoverTextButton ButtonPiggyBank;
        //public tsorcUIHoverTextButton ButtonSafe;
        //public tsorcUIHoverTextButton ButtonClose;

        public const int POTION_BAG_SIZE = 28;
        public static bool Visible = false;
        public static PotionItemSlot[] PotionSlots = new PotionItemSlot[POTION_BAG_SIZE]; //Keeps track of the slots
        public override void OnInitialize()
        {
            // Here we define our container UIElement. In DragableUIPanel.cs, you can see that DragableUIPanel is a UIPanel with a couple added features.
            PotionBagUI = new UIPanel();
            PotionBagUI.SetPadding(0);
            // We need to place this UIElement in relation to its Parent. Later we will be calling `base.Append(BonfireUI);`. 
            // This means that this class, BonfireUIState, will be our Parent. Since BonfireUIState is a UIState, the Left and Top are relative to the top left of the screen.
            PotionBagUI.Left.Set(150, 0f);
            PotionBagUI.Top.Set(316, 0f);
            PotionBagUI.Width.Set(350, 0f);
            PotionBagUI.Height.Set(200, 0f);
            PotionBagUI.BackgroundColor = new Color(30, 29, 43);

            int slotIndex = 0;
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    PotionSlots[i] = new PotionItemSlot(slotIndex, ItemSlot.Context.InventoryItem, 0.85f);
                    PotionSlots[i].Left.Set(10 + (j * 48), 0);
                    PotionSlots[i].Top.Set(10 + (i * 48), 0);
                    PotionSlots[i].Width.Set(44, 0);
                    PotionSlots[i].Height.Set(44, 0);
                    PotionSlots[i].ValidItemFunc = IsValidPotion;
                    PotionSlots[i].OnRightClick += new MouseEvent(RightClickedPotionSlot);
                    PotionBagUI.Append(PotionSlots[i]);
                    slotIndex++;
                }
            }

            Append(PotionBagUI);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            PotionBagUI.Left.Set(152, 0f);
            PotionBagUI.Top.Set(316, 0f);
            PotionBagUI.Width.Set(353, 0f);
            PotionBagUI.Height.Set(208, 0f);
            if (!Main.playerInventory || (!Main.LocalPlayer.HasItem(ModContent.ItemType<Items.PotionBag>()) && (Main.mouseItem.type != ModContent.ItemType<Items.PotionBag>())))
            {
                Visible = false;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
        }


        public static bool IsValidPotion(Item item)
        {
            bool valid = false;
            if (Items.tsorcGlobalItem.potionList.Contains(item.type))
            {
                valid = true;
            }
            if (item.Name.Contains("Potion"))
            {
                valid = true;
            }
            if (item.buffTime != 0)
            {
                valid = true;
            }
            if (item.type == ModContent.ItemType<Items.Potions.PermanentPotions.PermanentAle>())
            {
                valid = true;
            }
            if (item.type == ModContent.ItemType<Items.Potions.PermanentPotions.PermanentSoup>())
            {
                valid = true;
            }
            if (item.type == ModContent.ItemType<Items.Potions.PermanentPotions.PermanentArmorDrug>())
            {
                valid = true;
            }
            if (item.type == ModContent.ItemType<Items.Potions.PermanentPotions.PermanentDemonDrug>())
            {
                valid = true;
            }

            if (item.type == ModContent.ItemType<Items.PotionBag>())
            {
                valid = false;
            }
            return valid;
        }

        private void RightClickedPotionSlot(UIMouseEvent evt, UIElement listeningElement)
        {
            //listeningElement.
        }

        private void ButtonSetSpawnClicked(UIMouseEvent evt, UIElement listeningElement)
        {

            Main.NewText("Pressed!", 255, 240, 20, false);
            PotionBagUI.Left.Set((Main.screenWidth - 160) / 2, 0f);
            PotionBagUI.Top.Set((Main.screenHeight + 120) / 2, 0f);
            PotionBagUI.Width.Set(160f, 0f);
            PotionBagUI.Height.Set(64f, 0f);
        }
    }
}