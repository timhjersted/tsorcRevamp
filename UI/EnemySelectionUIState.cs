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
    // The "Quick Add" panel. Opens on the left when left-clicking empty space while holding the Enemy Debug Tome.
    // Picking an enemy attaches it to the cursor; each placement spawns its own single-NPC event using the
    // default options configured at the top of this panel. The panel stays open for rapid repeated placement.
    class EnemySelectionUIState : UIState
    {
        public bool Visible;
        private UIEnemySearchBar searchBar;
        private UIList npcList;
        private UIScrollbar scrollbar;
        public int SelectedNpcType = 0;
        // True while the cursor enemy came from this Quick Add panel (place => new single-NPC event).
        public bool QuickAddMode = false;
        // When set, the cursor enemy is a single-NPC event being relocated: placing it moves both the
        // event center and its one NPC to the new tile (rather than creating a new event).
        public DynamicSpawnEvent MovingEvent = null;
        public UIPanel panel;

        // ---- Defaults applied to each newly placed quick-add event ----
        public bool DefSave = false;
        public bool DefRing = false;
        public string DefWorld = "";
        public string DefSpawn = "";
        public int DefDust = DustID.Shadowflame;
        public float DefRadiusTiles = 25f;
        private bool defaultsInitialized = false;

        // Value labels for the default options
        private UIText saveValue;
        private UIText ringValue;
        private UIText worldConditionValue;
        private UIText conditionValue;
        private UIText dustValue;
        private UIText radiusValue;

        public override void OnInitialize()
        {
            panel = new UIPanel();
            panel.SetPadding(10);
            panel.Left.Set(20f, 0f);
            panel.Top.Set(-290f, 0.5f);
            panel.Width.Set(320f, 0f);
            panel.Height.Set(580f, 0f);
            panel.BackgroundColor = new Color(30, 30, 40) * 0.95f; // Match event menu background
            Append(panel);

            UIText titleText = new UIText("QUICK ADD", 0.9f);
            titleText.Left.Set(10, 0);
            titleText.Top.Set(10, 0);
            titleText.TextColor = new Color(255, 204, 0); // Gold
            panel.Append(titleText);

            UIText subtitle = new UIText("Each placed enemy = its own event", 0.7f);
            subtitle.Left.Set(10, 0);
            subtitle.Top.Set(32, 0);
            subtitle.TextColor = Color.DarkGray;
            panel.Append(subtitle);

            UIText closeButton = new UIText("X", 0.9f);
            closeButton.Left.Set(290, 0);
            closeButton.Top.Set(10, 0);
            closeButton.TextColor = Color.White;
            closeButton.OnMouseOver += (evt, el) => { closeButton.TextColor = Color.Red; };
            closeButton.OnMouseOut += (evt, el) => { closeButton.TextColor = Color.White; };
            closeButton.OnLeftClick += (evt, el) => { Hide(); };
            panel.Append(closeButton);

            // Save on Completion
            AddLabel("Save on Completion:", 55);
            saveValue = AddValue("[ Disabled ]", 55);
            saveValue.OnLeftClick += (evt, el) => { DefSave = !DefSave; RefreshDefaults(); };

            // Always Show Ring
            AddLabel("Always Show Ring:", 80);
            ringValue = AddValue("[ Hidden ]", 80);
            ringValue.OnLeftClick += (evt, el) => { DefRing = !DefRing; RefreshDefaults(); };

            // World Condition
            AddLabel("World Condition:", 105);
            worldConditionValue = AddValue("[ Always Active ]", 105);
            worldConditionValue.OnLeftClick += (evt, el) => {
                int i = Array.IndexOf(SpawnPointConfigUIState.WorldConditionMethods, DefWorld ?? "");
                if (i == -1) i = 0;
                DefWorld = SpawnPointConfigUIState.WorldConditionMethods[(i + 1) % SpawnPointConfigUIState.WorldConditionMethods.Length];
                RefreshDefaults();
            };

            // Spawn Condition
            AddLabel("Spawn Condition:", 130);
            conditionValue = AddValue("[ None ]", 130);
            conditionValue.OnLeftClick += (evt, el) => {
                int i = Array.IndexOf(SpawnPointConfigUIState.SpawnConditionMethods, DefSpawn ?? "");
                if (i == -1) i = 0;
                DefSpawn = SpawnPointConfigUIState.SpawnConditionMethods[(i + 1) % SpawnPointConfigUIState.SpawnConditionMethods.Length];
                RefreshDefaults();
            };

            // Trigger Dust
            AddLabel("Trigger Dust:", 155);
            dustValue = AddValue("[ Shadowflame ]", 155);
            dustValue.OnLeftClick += (evt, el) => {
                int i = Array.IndexOf(SpawnPointConfigUIState.PopularDusts, DefDust);
                if (i == -1) i = 0;
                DefDust = SpawnPointConfigUIState.PopularDusts[(i + 1) % SpawnPointConfigUIState.PopularDusts.Length];
                for (int d = 0; d < 20; d++)
                {
                    Dust.NewDust(Main.LocalPlayer.position, 16, 16, DefDust, Main.rand.Next(-4, 4), Main.rand.Next(-4, 4));
                }
                RefreshDefaults();
            };

            // Trigger Radius
            AddLabel("Trigger Radius:", 180);

            UIText radiusMinus = new UIText("[ - ]", 0.85f);
            radiusMinus.Left.Set(-140f, 1f);
            radiusMinus.Top.Set(180, 0);
            radiusMinus.TextColor = Color.LightSkyBlue;
            radiusMinus.OnMouseOver += (evt, el) => { radiusMinus.TextColor = Color.White; };
            radiusMinus.OnMouseOut += (evt, el) => { radiusMinus.TextColor = Color.LightSkyBlue; };
            radiusMinus.OnLeftClick += (evt, el) => { DefRadiusTiles = Math.Max(5, DefRadiusTiles - 5); RefreshDefaults(); };
            panel.Append(radiusMinus);

            radiusValue = new UIText("25 tiles", 0.85f);
            radiusValue.Left.Set(-105f, 1f);
            radiusValue.Top.Set(180, 0);
            radiusValue.TextColor = Color.White;
            panel.Append(radiusValue);

            UIText radiusPlus = new UIText("[ + ]", 0.85f);
            radiusPlus.Left.Set(-40f, 1f);
            radiusPlus.Top.Set(180, 0);
            radiusPlus.TextColor = Color.LightSkyBlue;
            radiusPlus.OnMouseOver += (evt, el) => { radiusPlus.TextColor = Color.White; };
            radiusPlus.OnMouseOut += (evt, el) => { radiusPlus.TextColor = Color.LightSkyBlue; };
            radiusPlus.OnLeftClick += (evt, el) => { DefRadiusTiles = Math.Min(150, DefRadiusTiles + 5); RefreshDefaults(); };
            panel.Append(radiusPlus);

            // ===== Enemy search & selection =====
            UIText searchTitle = new UIText("SELECT ENEMY TO PLACE:", 0.8f);
            searchTitle.Left.Set(10, 0);
            searchTitle.Top.Set(210, 0);
            searchTitle.TextColor = new Color(255, 204, 0);
            panel.Append(searchTitle);

            searchBar = new UIEnemySearchBar();
            searchBar.Left.Set(10, 0);
            searchBar.Top.Set(235, 0);
            searchBar.Width.Set(280, 0);
            searchBar.Height.Set(30, 0);
            searchBar.BackgroundColor = new Color(40, 40, 50) * 0.95f;
            searchBar.OnTextChanged += () => { PopulateList(searchBar.Text); };
            panel.Append(searchBar);

            npcList = new UIList();
            npcList.Left.Set(10, 0);
            npcList.Top.Set(275, 0);
            npcList.Width.Set(280, 0);
            npcList.Height.Set(285, 0);
            panel.Append(npcList);

            scrollbar = new UIScrollbar();
            scrollbar.SetView(100f, 1000f);
            scrollbar.Height.Set(285, 0);
            scrollbar.Top.Set(275, 0);
            scrollbar.Left.Set(295, 0);
            panel.Append(scrollbar);
            npcList.SetScrollbar(scrollbar);
        }

        private void AddLabel(string text, float top)
        {
            UIText label = new UIText(text, 0.85f);
            label.Left.Set(10, 0);
            label.Top.Set(top, 0);
            panel.Append(label);
        }

        private UIText AddValue(string text, float top)
        {
            UIText value = new UIText(text, 0.85f);
            value.HAlign = 1f;
            value.Left.Set(-20f, 0f);
            value.Top.Set(top, 0);
            value.TextColor = Color.White;
            value.OnMouseOver += (evt, el) => { value.TextColor = Color.Gold; };
            value.OnMouseOut += (evt, el) => { RefreshDefaults(); };
            panel.Append(value);
            return value;
        }

        public void RefreshDefaults()
        {
            saveValue.SetText(DefSave ? "[ Enabled ]" : "[ Disabled ]");
            saveValue.TextColor = DefSave ? Color.Lime : Color.Red;

            ringValue.SetText(DefRing ? "[ Visible ]" : "[ Hidden ]");
            ringValue.TextColor = DefRing ? Color.Lime : Color.Red;

            worldConditionValue.SetText($"[ {SpawnPointConfigUIState.GetWorldConditionDisplayName(DefWorld ?? "")} ]");
            worldConditionValue.TextColor = SpawnPointConfigUIState.GetConditionColor(DefWorld ?? "");

            conditionValue.SetText($"[ {SpawnPointConfigUIState.GetSpawnConditionDisplayName(DefSpawn ?? "")} ]");
            conditionValue.TextColor = SpawnPointConfigUIState.GetConditionColor(DefSpawn ?? "");

            dustValue.SetText($"[ {SpawnPointConfigUIState.GetDustName(DefDust)} ]");
            dustValue.TextColor = SpawnPointConfigUIState.GetDustColor(DefDust);

            radiusValue.SetText($"{(int)DefRadiusTiles} tiles");
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Visible && panel != null && panel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        // Hide entries that can't work as an event encounter: town/friendly NPCs, catchable critters
        // (bunnies, birds, etc.), and the target dummy. Real enemies and bosses are kept.
        //private static bool IsSelectableEnemy(NPC npc)
        //{
            //if (npc.townNPC || npc.friendly) return false;
            //if (npc.type < Main.npcCatchable.Length && Main.npcCatchable[npc.type]) return false;
            //if (npc.type == NPCID.TargetDummy) return false;
            //return true;
        //}

        public void PopulateList(string filter)
        {
            npcList.Clear();
            filter = filter.ToLower();

            List<NPC> matchingNpcs = new List<NPC>();
            for (int i = 1; i < NPCLoader.NPCCount; i++)
            {
                NPC npc = new NPC();
                npc.SetDefaults(i);
// removed to allow friendly NPCs and critters in event list: && IsSelectableEnemy(npc)
                if (npc.type != 0 
                    && (string.IsNullOrEmpty(filter) || npc.TypeName.ToLower().Contains(filter)))
                {
                    matchingNpcs.Add(npc);
                }
            }

            matchingNpcs.Sort((npc1, npc2) => string.Compare(npc1.TypeName, npc2.TypeName, StringComparison.OrdinalIgnoreCase));

            foreach (var npc in matchingNpcs)
            {
                UIText npcItem = new UIText(npc.TypeName, 0.85f);
                npcItem.TextColor = Color.White;
                npcItem.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => {
                    npcItem.TextColor = Color.Gold;
                };
                npcItem.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                    npcItem.TextColor = Color.White;
                };
                int type = npc.type;
                string typeName = npc.TypeName;
                npcItem.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                    SelectedNpcType = type;
                    QuickAddMode = true; // place => new single-NPC event
                    Main.NewText("Quick Add: " + typeName + " on cursor. Left click to drop (keeps placing), right click to stop.");
                    // Stay open for repeated placement.
                };
                npcList.Add(npcItem);
            }
        }

        public void Show()
        {
            Visible = true;

            // Default world condition to whichever world the player is currently in (only the first time).
            if (!defaultsInitialized)
            {
                if (tsorcRevampWorld.RemixMap)
                {
                    DefWorld = "RemixMapCondition";
                }
                else if (tsorcRevampWorld.OnlyAdventureMap)
                {
                    DefWorld = "OnlyAdventureMapCondition";
                }
                else
                {
                    DefWorld = "";
                }
                defaultsInitialized = true;
            }

            searchBar.Text = "";
            PopulateList("");
            RefreshDefaults();
        }

        public void Hide()
        {
            Visible = false;
            SelectedNpcType = 0;
            QuickAddMode = false;
            MovingEvent = null;
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
