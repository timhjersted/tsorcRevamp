using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Debug;
using Terraria.GameInput;

namespace tsorcRevamp.UI
{
    class EnemySelectionUIState : UIState
    {
        public bool Visible;
        private UIEnemySearchBar searchBar;
        private UIList npcList;
        private UIScrollbar scrollbar;
        public int SelectedNpcType = 0;
        public UIPanel panel;

        public override void OnInitialize()
        {
            panel = new UIPanel();
            panel.SetPadding(0);
            panel.Left.Set(20f, 0f);
            panel.Top.Set(-200f, 0.5f);
            panel.Width.Set(300f, 0f);
            panel.Height.Set(400f, 0f);
            panel.BackgroundColor = new Color(30, 30, 40) * 0.95f; // Match event menu background
            Append(panel);

            searchBar = new UIEnemySearchBar();
            searchBar.Left.Set(10, 0);
            searchBar.Top.Set(10, 0);
            searchBar.Width.Set(280, 0);
            searchBar.Height.Set(30, 0);
            searchBar.BackgroundColor = new Color(40, 40, 50) * 0.95f; // Sleek dark search background
            searchBar.OnTextChanged += () => { PopulateList(searchBar.Text); };
            panel.Append(searchBar);

            npcList = new UIList();
            npcList.Left.Set(10, 0);
            npcList.Top.Set(50, 0);
            npcList.Width.Set(260, 0);
            npcList.Height.Set(340, 0);
            panel.Append(npcList);

            scrollbar = new UIScrollbar();
            scrollbar.SetView(100f, 1000f);
            scrollbar.Height.Set(340, 0);
            scrollbar.Top.Set(50, 0);
            scrollbar.Left.Set(275, 0);
            panel.Append(scrollbar);
            npcList.SetScrollbar(scrollbar);

            UIText closeButton = new UIText("X", 0.9f);
            closeButton.Left.Set(280, 0);
            closeButton.Top.Set(5, 0);
            closeButton.TextColor = Color.White;
            closeButton.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => {
                closeButton.TextColor = Color.Red;
            };
            closeButton.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                closeButton.TextColor = Color.White;
            };
            closeButton.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                Hide();
            };
            panel.Append(closeButton);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Visible && panel != null && panel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        public void PopulateList(string filter)
        {
            npcList.Clear();
            filter = filter.ToLower();

            List<NPC> matchingNpcs = new List<NPC>();
            for (int i = 1; i < NPCLoader.NPCCount; i++)
            {
                NPC npc = new NPC();
                npc.SetDefaults(i);
                
                if (npc.type != 0 && (string.IsNullOrEmpty(filter) || npc.TypeName.ToLower().Contains(filter)))
                {
                    matchingNpcs.Add(npc);
                }
            }

            // Sort matching NPCs alphabetically by TypeName
            matchingNpcs.Sort((npc1, npc2) => string.Compare(npc1.TypeName, npc2.TypeName, StringComparison.OrdinalIgnoreCase));

            foreach (var npc in matchingNpcs)
            {
                UIText npcItem = new UIText(npc.TypeName, 0.85f);
                npcItem.TextColor = Color.White;
                npcItem.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => {
                    npcItem.TextColor = Color.Gold; // Gold hover effect
                };
                npcItem.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                    npcItem.TextColor = Color.White;
                };
                int type = npc.type;
                npcItem.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                    SelectedNpcType = type;
                    Main.NewText("Selected " + npc.TypeName + " for spawning. Left click in world to place. Right click to cancel.");
                    Hide();
                };
                npcList.Add(npcItem);
            }
        }

        public void Show()
        {
            Visible = true;
            SelectedNpcType = 0;
            searchBar.Text = "";
            PopulateList("");
        }

        public void Hide()
        {
            Visible = false;
            if (searchBar != null)
            {
                searchBar.Focused = false;
            }
            Main.blockInput = false;
            EnemyDebugTome.JustClosedUI = true;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (!Visible) return;
            base.Draw(spriteBatch);
        }

        public override bool ContainsPoint(Vector2 point)
        {
            return panel != null && panel.ContainsPoint(point);
        }
    }

    class UIEnemySearchBar : UIPanel
    {
        public string Text = "";
        public bool Focused = false;
        private int textBlinkerCount = 0;
        private int textBlinkerState = 0;

        public event System.Action OnTextChanged;

        public UIEnemySearchBar()
        {
            OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                Focused = true;
            };
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
            
            if (ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }

            Vector2 textPos = GetInnerDimensions().Position();
            textPos.Y -= 4f;

            if (!Focused)
            {
                if (string.IsNullOrEmpty(Text))
                {
                    Utils.DrawBorderString(spriteBatch, "Search...", textPos, Color.Gray);
                }
                else
                {
                    Utils.DrawBorderString(spriteBatch, Text, textPos, Color.White);
                }
            }
            else
            {
                Main.blockInput = true;
                Terraria.GameInput.PlayerInput.WritingText = true;

                if (Main.keyState.IsKeyDown(Keys.Escape) || Main.keyState.IsKeyDown(Keys.Enter) || (Main.mouseLeft && !ContainsPoint(Main.MouseScreen)))
                {
                    Focused = false;
                    Main.blockInput = false;
                }
                else
                {
                    string newText = Main.GetInputText(Text);
                    if (newText != Text)
                    {
                        Text = newText;
                        OnTextChanged?.Invoke();
                    }
                }

                textBlinkerCount++;
                if (textBlinkerCount >= 20)
                {
                    textBlinkerState = (textBlinkerState + 1) % 2;
                    textBlinkerCount = 0;
                }

                string displayString = Text + (textBlinkerState == 1 ? "|" : "");
                Utils.DrawBorderString(spriteBatch, displayString, textPos, Color.White);
            }
        }
    }
}
