using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Items.Potions.PermanentPotions;


namespace tsorcRevamp.UI
{
    class PotionBagUIState : UIState
    {
        public UIPanel PotionBagUI;
        //public tsorcUIHoverTextButton ButtonSetSpawn;
        //public tsorcUIHoverTextButton ButtonPiggyBank;
        //public tsorcUIHoverTextButton ButtonSafe;
        //public tsorcUIHoverTextButton ButtonClose;

        public const int POTION_BAG_SIZE = 40;
        public static bool Visible = false;
        public static PotionItemSlot[] PotionSlots = new PotionItemSlot[POTION_BAG_SIZE]; //Keeps track of the slots
        public override void OnInitialize()
        {
            // Here we define our container UIElement. In DragableUIPanel.cs, you can see that DragableUIPanel is a UIPanel with a couple added features.
            PotionBagUI = new UIPanel();
            PotionBagUI.SetPadding(0);
            // We need to place this UIElement in relation to its Parent. Later we will be calling `base.Append(BonfireUI);`. 
            // This means that this class, BonfireUIState, will be our Parent. Since BonfireUIState is a UIState, the Left and Top are relative to the top left of the screen.
            PotionBagUI.Left.Set(152, 0f);
            PotionBagUI.Top.Set(316, 0f);
            PotionBagUI.Width.Set(400, 0f);
            PotionBagUI.Height.Set(255, 0f);
            PotionBagUI.BackgroundColor = new Color(30, 29, 43);
            
            PotionBagUI.OnUpdate += (UIElement affectedElement) => 
            {
                //Don't use the player's held item if the mouse is hovering over this.
                if (affectedElement.ContainsPoint(Main.MouseScreen))
                {
                    Main.LocalPlayer.mouseInterface = true;
                }
            };
            
            int slotIndex = 0;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 8; j++)
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
            if (!Main.playerInventory || Main.LocalPlayer.chest != -1 || (!Main.LocalPlayer.HasItem(ModContent.ItemType<Items.PotionBag>()) && (Main.mouseItem.type != ModContent.ItemType<Items.PotionBag>())))
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
            if (item.type == ModContent.ItemType<Items.PotionBag>()) return false;
            if (item.buffType == BuffID.WellFed || item.buffType == BuffID.WellFed2 || item.buffType == BuffID.WellFed3)
                valid = true;

            if (item.ModItem is PermanentPotion)
                valid = true;
            if (Items.tsorcGlobalItem.potionList.Contains(item.type))
            {
                valid = true;
            }
            if (item.Name.Contains("Potion"))
            {
                valid = true;
            }
            if (item.Name.Contains("Flask"))
            {
                valid = true;
            }
            if (item.buffTime != 0)
            {
                valid = true;
            }

            //Whitelist
            if (item.type == ModContent.ItemType<Items.Potions.MushroomSkewer>())
            {
                valid = true;
            }
            if (item.type == ModContent.ItemType<Items.Potions.ChickenGlowingMushroomSkewer>())
            {
                valid = true;
            }
            if (item.type == ModContent.ItemType<Items.Potions.ChickenMushroomSkewer>())
            {
                valid = true;
            }
            if (item.type == ModContent.ItemType<Items.Potions.GlowingMushroomSkewer>())
            {
                valid = true;
            }
            if (item.type == ModContent.ItemType<Items.Potions.CookedChicken>())
            {
                valid = true;
            }
            if (item.type == Terraria.ID.ItemID.Honeyfin)
            {
                valid = true;
            }

            //Blacklist

            if (item.type == ModContent.ItemType<Items.EstusFlaskShard>())
            {
                valid = false;
            }

            //Was going to remove this, but also lmao
            //if (item.type == Terraria.ID.ItemID.ToxicFlask)
            //{
            //valid = false;
            //}

            //Excluding these specifically because for now they need to be used by hand. May change in the future.
            if (item.type == ModContent.ItemType<Items.Potions.Lifegem>())
            {
                valid = false;
            }

            if (item.type == ModContent.ItemType<Items.Potions.RadiantLifegem>())
            {
                valid = false;
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
    }
}
