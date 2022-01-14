using tsorcRevamp.UI;
using Terraria.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.UI.Elements;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.UI
{
    class MinimapBonfireUIState : UIState
    {
        public UIPanel MinimapBonfireUI;
        //public tsorcUIHoverTextButton ButtonSetSpawn;
        //public tsorcUIHoverTextButton ButtonPiggyBank;
        //public tsorcUIHoverTextButton ButtonSafe;
        //public tsorcUIHoverTextButton ButtonClose;

        public static bool Visible = false;
        public override void OnInitialize()
        {
            // Here we define our container UIElement. In DragableUIPanel.cs, you can see that DragableUIPanel is a UIPanel with a couple added features.
            MinimapBonfireUI = new UIPanel();
            MinimapBonfireUI.SetPadding(0);
            // We need to place this UIElement in relation to its Parent. Later we will be calling `base.Append(BonfireUI);`. 
            // This means that this class, BonfireUIState, will be our Parent. Since BonfireUIState is a UIState, the Left and Top are relative to the top left of the screen.
            MinimapBonfireUI.Left.Set(150, 0f);
            MinimapBonfireUI.Top.Set(316, 0f);
            MinimapBonfireUI.Width.Set(350, 0f);
            MinimapBonfireUI.Height.Set(200, 0f);
            MinimapBonfireUI.BackgroundColor = new Color(30, 29, 43);

            

            Append(MinimapBonfireUI);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            MinimapBonfireUI.Left.Set(152, 0f);
            MinimapBonfireUI.Top.Set(316, 0f);
            MinimapBonfireUI.Width.Set(353, 0f);
            MinimapBonfireUI.Height.Set(208, 0f);
            if (!Main.playerInventory || Main.LocalPlayer.chest != -1 || (!Main.LocalPlayer.HasItem(ModContent.ItemType<Items.PotionBag>()) && (Main.mouseItem.type != ModContent.ItemType<Items.PotionBag>())))
            {
                Visible = false;
            }
        }

        protected override void DrawSelf(SpriteBatch spriteBatch)
        {
            base.DrawSelf(spriteBatch);
        }


       

        
        public static List<Vector2> GetActiveBonfires()
        {
            List<Vector2> BonfireList = new List<Vector2>();
            int bonfireType = ModContent.TileType<Tiles.BonfireCheckpoint>();
            for (int i = 1; i < (Main.tile.GetUpperBound(0) - 1); i++)
            {
                for (int j = 1; j < (Main.tile.GetUpperBound(1) - 1); j++)
                {                   
                    //Check if each tile is a bonfire, and has a bonfire tile to its right and below it, but none to its left and above it. Only the top left corner of each bonfire is valid for this.
                    if ((Main.tile[i, j] != null && Main.tile[i, j].active() && Main.tile[i, j].type == bonfireType) && (Main.tile[i - 1, j] == null || !Main.tile[i - 1, j].active() || Main.tile[i - 1, j].type != bonfireType) && (Main.tile[i, j - 1] == null || !Main.tile[i, j - 1].active() || Main.tile[i, j - 1].type != bonfireType))
                    {
                        if (Main.tile[i, j].frameY / 74 == 0)
                        {
                            BonfireList.Add(new Vector2(i, j));
                        }
                    }                    
                }
            }

            return BonfireList;
        }
    }
}