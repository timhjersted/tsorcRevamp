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
    class SpawnPointConfigUIState : UIState
    {
        public bool Visible;
        public DynamicSpawnEvent CurrentEvent;

        public UIPanel panel;
        private UIList npcList;
        private UIScrollbar scrollbar;
        private UIText eventDetailsText;

        private UIText saveLabel;
        private UIText saveValue;
        private UIText ringLabel;
        private UIText ringValue;
        private UIText dustLabel;
        private UIText dustValue;

        private UIText radiusLabel;
        private UIText radiusMinus;
        private UIText radiusValue;
        private UIText radiusPlus;

        private UIText worldConditionLabel;
        private UIText worldConditionValue;

        private UIText conditionLabel;
        private UIText conditionValue;

        private UIText spawnsTitle;
        private UIText searchTitle;

        private UIEnemySearchBar searchBar;
        private UIList npcSearchList;
        private UIScrollbar searchScrollbar;

        // Tracks whether the multi-NPC sections (configured spawns + add-enemy search) are currently attached.
        private bool lowerSectionsAttached = true;

        // ── Edit NPC stats panel ──────────────────────────────────────────────────
        private DynamicSpawnEntry editingNpc;
        private int editingNpcIndex = -1; // index of editingNpc within CurrentEvent.Npcs, for rebinding after a save/reload
        private bool editPanelAttached;

        private UIText editTitle;

        private UIText healthLabel;
        private UINumericInputField healthInput;

        private UIText soulsLabel;
        private UINumericInputField soulsInput;

        private UIText damageLabel;
        private UINumericInputField damageInput;

        private UIText defenseLabel;
        private UINumericInputField defenseInput;

        private UIText spawnTextLabel;
        private UIEnemySearchBar spawnTextInput;

        private UIText editBackButton;
        // ─────────────────────────────────────────────────────────────────────────

        // World-level gate. Stored in DynamicSpawnEvent.WorldCondition.
        internal static readonly string[] WorldConditionMethods = new string[]
        {
            "", // Always Active
            "OnlyAdventureMapCondition",
            "RemixMapCondition"
        };

        // Spawn condition (progression / time-of-day). Stored in DynamicSpawnEvent.MapCondition. "" = None.
        internal static readonly string[] SpawnConditionMethods = new string[]
        {
            "", // None
            "NormalModeCustomCondition",
            "HardModeCustomCondition",
            "SuperHardModeCustomCondition",
            "NightCustomCondition",
            "PreEoCCustomCondition",
            "PreEoWCustomCondition",
            "PostEoWCustomCondition",
            "PreSkeletronCustomCondition",
            "PreMechCustomCondition",
            "GolemDownedCustomCondition",
            "EoLDownedCondition",
            "CultistDownedCondition",
            "ThoriumActiveCondition"
        };

        internal static string GetWorldConditionDisplayName(string methodName)
        {
            return methodName switch
            {
                "" => "Always Active",
                "OnlyAdventureMapCondition" => "Adventure Map Only",
                "RemixMapCondition" => "Remix Map Only",
                _ => methodName
            };
        }

        internal static string GetSpawnConditionDisplayName(string methodName)
        {
            return string.IsNullOrEmpty(methodName) ? "None" : GetConditionDisplayName(methodName);
        }

        internal static string GetConditionDisplayName(string methodName)
        {
            return methodName switch
            {
                "" => "Always Active",
                "NormalModeCustomCondition" => "Pre-Hardmode Only",
                "HardModeCustomCondition" => "Hardmode Only",
                "SuperHardModeCustomCondition" => "Super-Hardmode Only",
                "NightCustomCondition" => "Nighttime Only",
                "OnlyAdventureMapCondition" => "Adventure Map Only",
                "RemixMapCondition" => "Remix Map Only",
                "PreEoCCustomCondition" => "Before Eye of Cthulhu",
                "PreEoWCustomCondition" => "Before Eater of Worlds",
                "PostEoWCustomCondition" => "After Eater of Worlds",
                "PreSkeletronCustomCondition" => "Before Skeletron",
                "PreMechCustomCondition" => "Before Mech Bosses",
                "GolemDownedCustomCondition" => "After Golem",
                "EoLDownedCondition" => "Before Empress of Light",
                "CultistDownedCondition" => "Before Lunatic Cultist",
                "ThoriumActiveCondition" => "Thorium Mod Active",
                _ => methodName
            };
        }

        internal static Color GetConditionColor(string methodName)
        {
            return methodName switch
            {
                "" => Color.White,
                "NormalModeCustomCondition" => Color.LightGreen,
                "HardModeCustomCondition" => Color.Orange,
                "SuperHardModeCustomCondition" => Color.Crimson,
                "NightCustomCondition" => Color.MediumPurple,
                "OnlyAdventureMapCondition" => Color.SkyBlue,
                "RemixMapCondition" => Color.Pink,
                "ThoriumActiveCondition" => Color.Turquoise,
                _ => Color.Yellow
            };
        }

        internal static readonly int[] PopularDusts = new int[]
        {
            DustID.Shadowflame,
            DustID.Torch,
            DustID.HallowedTorch,
            DustID.IceTorch,
            DustID.GreenTorch,
            DustID.GoldFlame,
            DustID.BoneTorch,
            DustID.PurpleTorch
        };

        internal static string GetDustName(int dustId)
        {
            return dustId switch
            {
                DustID.Shadowflame => "Shadowflame",
                DustID.Torch => "Fire",
                DustID.HallowedTorch => "Hallow Pink",
                DustID.IceTorch => "Ice Blue",
                DustID.GreenTorch => "Jungle Green",
                DustID.GoldFlame => "Golden Flame",
                DustID.BoneTorch => "Bone White",
                DustID.PurpleTorch => "Corruption Purple",
                _ => "Custom"
            };
        }

        internal static Color GetDustColor(int dustId)
        {
            return dustId switch
            {
                DustID.Shadowflame => Color.MediumPurple,
                DustID.Torch => Color.OrangeRed,
                DustID.HallowedTorch => Color.HotPink,
                DustID.IceTorch => Color.SkyBlue,
                DustID.GreenTorch => Color.LightGreen,
                DustID.GoldFlame => Color.Gold,
                DustID.BoneTorch => Color.White,
                DustID.PurpleTorch => Color.Purple,
                _ => Color.White
            };
        }

        public override void OnInitialize()
        {
            panel = new UIPanel();
            panel.SetPadding(10);
            panel.Left.Set(-200f, 0.833f); // Centered in the right third of screen
            panel.Top.Set(-300f, 0.5f); // Centered vertically
            panel.Width.Set(400f, 0f); // 20px wider single column width
            panel.Height.Set(625f, 0f); // Taller height to stack everything (extra row for World Condition)
            panel.BackgroundColor = new Color(30, 30, 40) * 0.95f; // Sleek dark panel
            Append(panel);

            // Title with standard size (large = false)
            UIText titleText = new UIText("EVENT CONFIG", 0.9f);
            titleText.Left.Set(10, 0);
            titleText.Top.Set(10, 0);
            titleText.TextColor = new Color(255, 204, 0); // Gold
            panel.Append(titleText);

            eventDetailsText = new UIText("Event ID: ...", 0.75f);
            eventDetailsText.Left.Set(10, 0);
            eventDetailsText.Top.Set(30, 0);
            eventDetailsText.TextColor = Color.DarkGray;
            panel.Append(eventDetailsText);

            // Red Delete button
            UIText deleteButton = new UIText("[ Delete Event ]", 0.8f);
            deleteButton.TextColor = Color.Crimson;
            deleteButton.Left.Set(190, 0);
            deleteButton.Top.Set(10, 0);
            deleteButton.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => {
                deleteButton.TextColor = Color.Red;
            };
            deleteButton.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                deleteButton.TextColor = Color.Crimson;
            };
            deleteButton.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                if (CurrentEvent != null)
                {
                    tsorcScriptedEvents.DynamicEvents.Remove(CurrentEvent);
                    tsorcScriptedEvents.SaveDynamicEvents();
                    Hide();
                }
            };
            panel.Append(deleteButton);

            // Close button "X"
            UIText closeButton = new UIText("X", 0.9f);
            closeButton.Left.Set(370, 0); // Far right of 400px panel
            closeButton.Top.Set(10, 0);
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

            // Option 1: Save on Completion
            saveLabel = new UIText("Save on Completion:", 0.85f);
            saveLabel.Left.Set(10, 0);
            saveLabel.Top.Set(60, 0);
            panel.Append(saveLabel);

            saveValue = new UIText("[ Enabled ]", 0.85f);
            saveValue.HAlign = 1f;
            saveValue.Left.Set(-20f, 0f);
            saveValue.Top.Set(60, 0);
            saveValue.TextColor = Color.Lime;
            saveValue.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => {
                saveValue.TextColor = Color.White;
            };
            saveValue.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                RefreshDetails();
            };
            saveValue.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                if (CurrentEvent != null)
                {
                    CurrentEvent.SaveOnCompletion = !CurrentEvent.SaveOnCompletion;
                    tsorcScriptedEvents.SaveDynamicEvents();
                    RefreshDetails();
                }
            };
            panel.Append(saveValue);

            // Option 2: Always Show Ring
            ringLabel = new UIText("Always Show Ring:", 0.85f);
            ringLabel.Left.Set(10, 0);
            ringLabel.Top.Set(85, 0);
            panel.Append(ringLabel);

            ringValue = new UIText("[ Visible ]", 0.85f);
            ringValue.HAlign = 1f;
            ringValue.Left.Set(-20f, 0f);
            ringValue.Top.Set(85, 0);
            ringValue.TextColor = Color.Lime;
            ringValue.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => {
                ringValue.TextColor = Color.White;
            };
            ringValue.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                RefreshDetails();
            };
            ringValue.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                if (CurrentEvent != null)
                {
                    CurrentEvent.VisibleRing = !CurrentEvent.VisibleRing;
                    tsorcScriptedEvents.SaveDynamicEvents();
                    RefreshDetails();
                }
            };
            panel.Append(ringValue);

            // Option 3: World Condition (Adventure / Remix / Always Active)
            worldConditionLabel = new UIText("World Condition:", 0.85f);
            worldConditionLabel.Left.Set(10, 0);
            worldConditionLabel.Top.Set(110, 0);
            panel.Append(worldConditionLabel);

            worldConditionValue = new UIText("[ Always Active ]", 0.85f);
            worldConditionValue.HAlign = 1f;
            worldConditionValue.Left.Set(-20f, 0f);
            worldConditionValue.Top.Set(110, 0);
            worldConditionValue.TextColor = Color.White;
            worldConditionValue.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => {
                worldConditionValue.TextColor = Color.Gold;
            };
            worldConditionValue.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                RefreshDetails();
            };
            worldConditionValue.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                if (CurrentEvent != null)
                {
                    int currentIndex = System.Array.IndexOf(WorldConditionMethods, CurrentEvent.WorldCondition ?? "");
                    if (currentIndex == -1) currentIndex = 0;
                    int nextIndex = (currentIndex + 1) % WorldConditionMethods.Length;
                    CurrentEvent.WorldCondition = WorldConditionMethods[nextIndex];
                    tsorcScriptedEvents.SaveDynamicEvents();
                    RefreshDetails();
                }
            };
            panel.Append(worldConditionValue);

            // Option 4: Spawn Condition (progression / time-of-day, None by default)
            conditionLabel = new UIText("Spawn Condition:", 0.85f);
            conditionLabel.Left.Set(10, 0);
            conditionLabel.Top.Set(135, 0);
            panel.Append(conditionLabel);

            conditionValue = new UIText("[ None ]", 0.85f);
            conditionValue.HAlign = 1f;
            conditionValue.Left.Set(-20f, 0f);
            conditionValue.Top.Set(135, 0);
            conditionValue.TextColor = Color.White;
            conditionValue.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => {
                conditionValue.TextColor = Color.Gold;
            };
            conditionValue.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                RefreshDetails();
            };
            conditionValue.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                if (CurrentEvent != null)
                {
                    int currentIndex = System.Array.IndexOf(SpawnConditionMethods, CurrentEvent.MapCondition ?? "");
                    if (currentIndex == -1) currentIndex = 0;
                    int nextIndex = (currentIndex + 1) % SpawnConditionMethods.Length;
                    CurrentEvent.MapCondition = SpawnConditionMethods[nextIndex];
                    tsorcScriptedEvents.SaveDynamicEvents();
                    RefreshDetails();
                }
            };
            panel.Append(conditionValue);

            // Option 5: Trigger Dust Type
            dustLabel = new UIText("Trigger Dust:", 0.85f);
            dustLabel.Left.Set(10, 0);
            dustLabel.Top.Set(160, 0);
            panel.Append(dustLabel);

            dustValue = new UIText("[ Shadowflame ]", 0.85f);
            dustValue.HAlign = 1f;
            dustValue.Left.Set(-20f, 0f);
            dustValue.Top.Set(160, 0);
            dustValue.TextColor = Color.Violet;
            dustValue.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => {
                dustValue.TextColor = Color.White;
            };
            dustValue.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                RefreshDetails();
            };
            dustValue.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                if (CurrentEvent != null)
                {
                    int currentIndex = System.Array.IndexOf(PopularDusts, CurrentEvent.TriggerDust);
                    if (currentIndex == -1) currentIndex = 0;
                    int nextIndex = (currentIndex + 1) % PopularDusts.Length;
                    CurrentEvent.TriggerDust = PopularDusts[nextIndex];

                    // Spawn dust burst around the player to visually preview it!
                    for (int i = 0; i < 20; i++)
                    {
                        Dust.NewDust(Main.LocalPlayer.position, 16, 16, CurrentEvent.TriggerDust, Main.rand.Next(-4, 4), Main.rand.Next(-4, 4));
                    }

                    tsorcScriptedEvents.SaveDynamicEvents();
                    RefreshDetails();
                }
            };
            panel.Append(dustValue);

            // Option 6: Trigger Radius Control
            radiusLabel = new UIText("Trigger Radius:", 0.85f);
            radiusLabel.Left.Set(10, 0);
            radiusLabel.Top.Set(185, 0);
            panel.Append(radiusLabel);

            radiusMinus = new UIText("[ - ]", 0.85f);
            radiusMinus.Left.Set(-140f, 1f);
            radiusMinus.Top.Set(185, 0);
            radiusMinus.TextColor = Color.LightSkyBlue;
            radiusMinus.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => {
                radiusMinus.TextColor = Color.White;
            };
            radiusMinus.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                radiusMinus.TextColor = Color.LightSkyBlue;
            };
            radiusMinus.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                if (CurrentEvent != null)
                {
                    float currentTiles = (float)Math.Sqrt(CurrentEvent.Radius) / 16f;
                    currentTiles = Math.Max(5, currentTiles - 5);
                    CurrentEvent.Radius = (float)Math.Pow(currentTiles * 16, 2);
                    tsorcScriptedEvents.SaveDynamicEvents();
                    RefreshDetails();
                }
            };
            panel.Append(radiusMinus);

            radiusValue = new UIText("30 tiles", 0.85f);
            radiusValue.Left.Set(-105f, 1f);
            radiusValue.Top.Set(185, 0);
            radiusValue.TextColor = Color.White;
            panel.Append(radiusValue);

            radiusPlus = new UIText("[ + ]", 0.85f);
            radiusPlus.Left.Set(-40f, 1f);
            radiusPlus.Top.Set(185, 0);
            radiusPlus.TextColor = Color.LightSkyBlue;
            radiusPlus.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => {
                radiusPlus.TextColor = Color.White;
            };
            radiusPlus.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                radiusPlus.TextColor = Color.LightSkyBlue;
            };
            radiusPlus.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                if (CurrentEvent != null)
                {
                    float currentTiles = (float)Math.Sqrt(CurrentEvent.Radius) / 16f;
                    currentTiles = Math.Min(150, currentTiles + 5);
                    CurrentEvent.Radius = (float)Math.Pow(currentTiles * 16, 2);
                    tsorcScriptedEvents.SaveDynamicEvents();
                    RefreshDetails();
                }
            };
            panel.Append(radiusPlus);

            // Configured spawns title (large = false)
            spawnsTitle = new UIText("CONFIGURED ENEMY SPAWNS:", 0.8f);
            spawnsTitle.Left.Set(10, 0);
            spawnsTitle.Top.Set(215, 0);
            spawnsTitle.TextColor = Color.SkyBlue;
            panel.Append(spawnsTitle);

            // Scrollable NPC List
            npcList = new UIList();
            npcList.Left.Set(10, 0);
            npcList.Top.Set(240, 0);
            npcList.Width.Set(355, 0); // Wider list
            npcList.Height.Set(110, 0); // Shorter list (stacked vertically)
            panel.Append(npcList);

            // Scrollbar
            scrollbar = new UIScrollbar();
            scrollbar.SetView(100f, 1000f);
            scrollbar.Height.Set(110, 0);
            scrollbar.Top.Set(240, 0);
            scrollbar.Left.Set(370, 0); // Moved to right edge of 400px panel
            panel.Append(scrollbar);
            npcList.SetScrollbar(scrollbar);

            // ================= NPC SEARCH & SELECTION (STACKED BELOW) =================
            searchTitle = new UIText("SELECT ENEMY TO PLACE:", 0.8f);
            searchTitle.Left.Set(10, 0);
            searchTitle.Top.Set(360, 0); // Stacked below configured spawns
            searchTitle.TextColor = new Color(255, 204, 0); // Gold Title
            panel.Append(searchTitle);

            searchBar = new UIEnemySearchBar();
            searchBar.Left.Set(10, 0);
            searchBar.Top.Set(385, 0);
            searchBar.Width.Set(355, 0); // Wider search bar
            searchBar.Height.Set(30, 0);
            searchBar.BackgroundColor = new Color(40, 40, 50) * 0.95f;
            searchBar.OnTextChanged += () => { PopulateSearchList(searchBar.Text); };
            panel.Append(searchBar);

            npcSearchList = new UIList();
            npcSearchList.Left.Set(10, 0);
            npcSearchList.Top.Set(425, 0);
            npcSearchList.Width.Set(355, 0); // Wider search list
            npcSearchList.Height.Set(185, 0); // Fills remaining height
            panel.Append(npcSearchList);

            searchScrollbar = new UIScrollbar();
            searchScrollbar.SetView(100f, 1000f);
            searchScrollbar.Height.Set(185, 0);
            searchScrollbar.Top.Set(425, 0);
            searchScrollbar.Left.Set(370, 0); // Moved to right edge of 400px panel
            panel.Append(searchScrollbar);
            npcSearchList.SetScrollbar(searchScrollbar);

            // ================= EDIT NPC STATS PANEL =================
            // Elements are permanently appended here at Y=-2000 (off-screen / clipped).
            // ShowEditPanel() moves them to visible positions; DetachEditElements() returns them here.
            InitEditPanel();
        }

        // Creates all edit-panel elements and immediately appends them to the panel at Y=-2000
        // so they are always part of the layout tree but invisible until ShowEditPanel positions them.
        private void InitEditPanel()
        {
            const float hidden = -2000f;

            editTitle = new UIText("EDIT NPC", 0.8f);
            editTitle.Left.Set(10, 0);
            editTitle.Top.Set(hidden, 0);
            editTitle.TextColor = new Color(255, 204, 0);
            panel.Append(editTitle);

            healthLabel = MakeStatLabel("Health:");
            healthLabel.Top.Set(hidden, 0);
            panel.Append(healthLabel);
            healthInput = MakeStatInput(v => { if (editingNpc != null) editingNpc.CustomHealth = v; });
            healthInput.Top.Set(hidden, 0);
            panel.Append(healthInput);

            soulsLabel = MakeStatLabel("Souls (Value):");
            soulsLabel.Top.Set(hidden, 0);
            panel.Append(soulsLabel);
            soulsInput = MakeStatInput(v => { if (editingNpc != null) editingNpc.CustomSouls = v; });
            soulsInput.Top.Set(hidden, 0);
            panel.Append(soulsInput);

            damageLabel = MakeStatLabel("Damage:");
            damageLabel.Top.Set(hidden, 0);
            panel.Append(damageLabel);
            damageInput = MakeStatInput(v => { if (editingNpc != null) editingNpc.CustomDamage = v; });
            damageInput.Top.Set(hidden, 0);
            panel.Append(damageInput);

            defenseLabel = MakeStatLabel("Defense:");
            defenseLabel.Top.Set(hidden, 0);
            panel.Append(defenseLabel);
            defenseInput = MakeStatInput(v => { if (editingNpc != null) editingNpc.CustomDefense = v; });
            defenseInput.Top.Set(hidden, 0);
            panel.Append(defenseInput);

            spawnTextLabel = new UIText("Spawn Text:", 0.85f);
            spawnTextLabel.Left.Set(10, 0);
            spawnTextLabel.Top.Set(hidden, 0);
            spawnTextLabel.TextColor = Color.White;
            panel.Append(spawnTextLabel);

            spawnTextInput = new UIEnemySearchBar();
            spawnTextInput.Left.Set(10, 0);
            spawnTextInput.Top.Set(hidden, 0);
            spawnTextInput.Width.Set(355, 0);
            spawnTextInput.Height.Set(28, 0);
            spawnTextInput.BackgroundColor = new Color(40, 40, 50) * 0.95f;
            spawnTextInput.OnTextChanged += () =>
            {
                if (CurrentEvent != null)
                {
                    CurrentEvent.TextToDisplay = spawnTextInput.Text;
                    tsorcScriptedEvents.SaveDynamicEvents();
                }
            };
            panel.Append(spawnTextInput);

            editBackButton = new UIText("[ ← Back ]", 0.85f);
            editBackButton.Left.Set(10, 0);
            editBackButton.Top.Set(hidden, 0);
            editBackButton.TextColor = Color.LightSkyBlue;
            editBackButton.OnMouseOver += (e, el) => editBackButton.TextColor = Color.White;
            editBackButton.OnMouseOut += (e, el) => editBackButton.TextColor = Color.LightSkyBlue;
            editBackButton.OnLeftClick += (e, el) => HideEditPanel();
            panel.Append(editBackButton);
        }

        private UIText MakeStatLabel(string text)
        {
            var label = new UIText(text, 0.85f);
            label.Left.Set(10, 0);
            label.TextColor = Color.White;
            return label;
        }

        // Builds a typeable numeric field, right-aligned. setValue receives the parsed value
        // (null when the field is empty → revert to the NPC default) and is saved immediately.
        private UINumericInputField MakeStatInput(Action<int?> setValue)
        {
            var input = new UINumericInputField();
            input.Left.Set(-130f, 1f);
            input.Width.Set(120f, 0f);
            input.Height.Set(24f, 0f);
            input.BackgroundColor = new Color(40, 40, 50) * 0.95f;
            input.OnValueChanged += () =>
            {
                setValue(input.Value);
                tsorcScriptedEvents.SaveDynamicEvents();
            };
            return input;
        }

        // Loads a stat field's current text + placeholder for the NPC being edited.
        private void PopulateStatField(UINumericInputField input, int? custom, int defaultValue)
        {
            input.Focused = false;
            input.Text = custom.HasValue ? custom.Value.ToString() : "";
            input.Placeholder = $"Default ({defaultValue})";
        }

        // All elements that make up the edit panel, for bulk attach/detach.
        private UIElement[] EditElements() => new UIElement[]
        {
            editTitle,
            healthLabel, healthInput,
            soulsLabel, soulsInput,
            damageLabel, damageInput,
            defenseLabel, defenseInput,
            spawnTextLabel, spawnTextInput,
            editBackButton
        };

        public void ShowEditPanel(DynamicSpawnEntry npc)
        {
            editingNpc = npc;
            // Remember its position so we can rebind after SaveDynamicEvents() reloads (and replaces) the list.
            editingNpcIndex = CurrentEvent != null ? CurrentEvent.Npcs.IndexOf(npc) : -1;

            // Single-NPC events: lower sections hidden, so edit panel starts at Y=215.
            // Multi-NPC events: edit panel appears below the spawns list at Y=360.
            bool isSingle = CurrentEvent?.SingleNpcMarker == true;
            float startY = isSingle ? 215f : 360f;

            editTitle.Top.Set(startY, 0);

            float rowY = startY + 25f;
            float rowH = 28f;

            PositionStatRow(healthLabel, healthInput, rowY); rowY += rowH;
            PositionStatRow(soulsLabel, soulsInput, rowY); rowY += rowH;
            PositionStatRow(damageLabel, damageInput, rowY); rowY += rowH;
            PositionStatRow(defenseLabel, defenseInput, rowY); rowY += rowH + 5f;

            spawnTextLabel.Top.Set(rowY, 0); rowY += 20f;
            spawnTextInput.Top.Set(rowY, 0); rowY += 35f;

            // Back button only shown for multi-NPC (single-NPC has nothing to go back to).
            editBackButton.Top.Set(isSingle ? -2000f : rowY, 0);

            // Populate fields from the NPC's custom values / defaults.
            NPC temp = new NPC();
            temp.SetDefaults(npc.NpcID);
            editTitle.SetText($"EDIT  {temp.TypeName.ToUpper()}");
            PopulateStatField(healthInput, npc.CustomHealth, temp.lifeMax);
            PopulateStatField(soulsInput, npc.CustomSouls, (int)(temp.value / 10));
            PopulateStatField(damageInput, npc.CustomDamage, temp.damage);
            PopulateStatField(defenseInput, npc.CustomDefense, temp.defense);
            spawnTextInput.Text = CurrentEvent?.TextToDisplay ?? "";

            // For multi-NPC: hide the search section the first time to make room for the edit panel.
            if (!editPanelAttached && !isSingle)
            {
                panel.RemoveChild(searchTitle);
                panel.RemoveChild(searchBar);
                panel.RemoveChild(npcSearchList);
                panel.RemoveChild(searchScrollbar);
            }

            editPanelAttached = true;

            // Recompute all layout now that positions have been updated.
            panel.Recalculate();
        }

        // Lays out a stat row: label on the left, numeric field on the right, at vertical position y.
        private void PositionStatRow(UIText label, UINumericInputField input, float y)
        {
            label.Top.Set(y + 2f, 0); // nudge label to vertically center against the field
            input.Top.Set(y, 0);
        }

        public void HideEditPanel()
        {
            if (!editPanelAttached) return;

            DetachEditElements();

            // Restore search section (for multi-NPC events).
            if (CurrentEvent != null && !CurrentEvent.SingleNpcMarker)
            {
                panel.Append(searchTitle);
                panel.Append(searchBar);
                panel.Append(npcSearchList);
                panel.Append(searchScrollbar);
            }
        }

        // Moves all edit elements off-screen (Y=-2000) and clears edit state.
        // Elements remain in the panel's child list so they don't need to be re-appended later.
        // Does NOT restore the search section — callers that need it (HideEditPanel) handle that.
        private void DetachEditElements()
        {
            foreach (var el in EditElements())
            {
                el.Top.Set(-2000f, 0);
            }
            spawnTextInput.Focused = false;
            Main.blockInput = false;
            editingNpc = null;
            editingNpcIndex = -1;
            editPanelAttached = false;
            panel.Recalculate();
        }

        // SaveDynamicEvents() serializes then reloads the event list, replacing every object instance.
        // After it re-links CurrentEvent, it calls this so the edit panel points at the live entry again —
        // otherwise stat edits after the first keystroke land on an orphaned object and never persist.
        public void RebindEditingNpc()
        {
            if (editingNpc == null) return;
            if (CurrentEvent != null && editingNpcIndex >= 0 && editingNpcIndex < CurrentEvent.Npcs.Count)
            {
                editingNpc = CurrentEvent.Npcs[editingNpcIndex];
            }
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (Visible && panel != null && panel.ContainsPoint(Main.MouseScreen))
            {
                Main.LocalPlayer.mouseInterface = true;
            }
        }

        public void SetEvent(DynamicSpawnEvent ev)
        {
            CurrentEvent = ev;

            // Backward-compat: older events stored Adventure/Remix in MapCondition. Migrate it to WorldCondition
            // so the split menus display and cycle correctly.
            if (ev != null && string.IsNullOrEmpty(ev.WorldCondition)
                && (ev.MapCondition == "OnlyAdventureMapCondition" || ev.MapCondition == "RemixMapCondition"))
            {
                ev.WorldCondition = ev.MapCondition;
                ev.MapCondition = "";
                tsorcScriptedEvents.SaveDynamicEvents();
            }

            // Dismiss any open edit panel from a previous event.
            // HideEditPanel restores the search section for multi-NPC events before SetLowerSectionsVisible
            // decides which sections to show next.
            if (editPanelAttached)
            {
                HideEditPanel();
            }

            // Quick-add events are locked to a single NPC, so hide the multi-NPC add/list sections.
            SetLowerSectionsVisible(ev == null || !ev.SingleNpcMarker);

            RefreshDetails();
            RefreshList();

            // For single-NPC events, auto-open the edit panel for the one NPC.
            if (ev != null && ev.SingleNpcMarker && ev.Npcs.Count > 0)
            {
                ShowEditPanel(ev.Npcs[0]);
            }
        }

        // Attaches/detaches the "configured spawns" list and the "select enemy to place" search section.
        private void SetLowerSectionsVisible(bool visible)
        {
            if (visible == lowerSectionsAttached) return;

            if (visible)
            {
                panel.Append(spawnsTitle);
                panel.Append(npcList);
                panel.Append(scrollbar);
                panel.Append(searchTitle);
                panel.Append(searchBar);
                panel.Append(npcSearchList);
                panel.Append(searchScrollbar);
            }
            else
            {
                panel.RemoveChild(spawnsTitle);
                panel.RemoveChild(npcList);
                panel.RemoveChild(scrollbar);
                panel.RemoveChild(searchTitle);
                panel.RemoveChild(searchBar);
                panel.RemoveChild(npcSearchList);
                panel.RemoveChild(searchScrollbar);
            }

            lowerSectionsAttached = visible;
        }

        public void RefreshDetails()
        {
            if (CurrentEvent == null) return;

            eventDetailsText.SetText($"Event ID: {CurrentEvent.EventID.Substring(0, 8)}...");

            if (CurrentEvent.SaveOnCompletion)
            {
                saveValue.SetText("[ Enabled ]");
                saveValue.TextColor = Color.Lime;
            }
            else
            {
                saveValue.SetText("[ Disabled ]");
                saveValue.TextColor = Color.Red;
            }

            if (CurrentEvent.VisibleRing)
            {
                ringValue.SetText("[ Visible ]");
                ringValue.TextColor = Color.Lime;
            }
            else
            {
                ringValue.SetText("[ Hidden ]");
                ringValue.TextColor = Color.Red;
            }

            worldConditionValue.SetText($"[ {GetWorldConditionDisplayName(CurrentEvent.WorldCondition ?? "")} ]");
            worldConditionValue.TextColor = GetConditionColor(CurrentEvent.WorldCondition ?? "");

            conditionValue.SetText($"[ {GetSpawnConditionDisplayName(CurrentEvent.MapCondition ?? "")} ]");
            conditionValue.TextColor = GetConditionColor(CurrentEvent.MapCondition ?? "");

            dustValue.SetText($"[ {GetDustName(CurrentEvent.TriggerDust)} ]");
            dustValue.TextColor = GetDustColor(CurrentEvent.TriggerDust);

            float currentTiles = (float)Math.Sqrt(CurrentEvent.Radius) / 16f;
            radiusValue.SetText($"{(int)currentTiles} tiles");
        }

        public void RefreshList()
        {
            npcList.Clear();
            if (CurrentEvent == null) return;

            foreach (var npc in CurrentEvent.Npcs)
            {
                NPC temp = new NPC();
                temp.SetDefaults(npc.NpcID);

                var capturedNpc = npc;

                // Row container — UIList stacks these vertically.
                UIElement row = new UIElement();
                row.Width.Set(0f, 1f);
                row.Height.Set(22f, 0f);

                // NPC name (truncated at 22 chars to leave room for buttons)
                string nameText = temp.TypeName;
                if (nameText.Length > 22) nameText = nameText.Substring(0, 20) + "..";
                UIText nameLabel = new UIText(nameText, 0.78f);
                nameLabel.Left.Set(0, 0);
                nameLabel.Top.Set(2, 0);
                nameLabel.TextColor = Color.White;
                row.Append(nameLabel);

                // [Edit] button
                UIText editBtn = new UIText("[Edit]", 0.78f);
                editBtn.Left.Set(-85f, 1f);
                editBtn.Top.Set(2, 0);
                editBtn.TextColor = Color.SkyBlue;
                editBtn.OnMouseOver += (e, el) => editBtn.TextColor = Color.White;
                editBtn.OnMouseOut += (e, el) => editBtn.TextColor = Color.SkyBlue;
                editBtn.OnLeftClick += (e, el) => ShowEditPanel(capturedNpc);
                row.Append(editBtn);

                // [Del] button
                UIText delBtn = new UIText("[Del]", 0.78f);
                delBtn.Left.Set(-30f, 1f);
                delBtn.Top.Set(2, 0);
                delBtn.TextColor = Color.Crimson;
                delBtn.OnMouseOver += (e, el) => delBtn.TextColor = Color.Red;
                delBtn.OnMouseOut += (e, el) => delBtn.TextColor = Color.Crimson;
                delBtn.OnLeftClick += (e, el) =>
                {
                    if (editingNpc == capturedNpc) HideEditPanel();
                    CurrentEvent.Npcs.Remove(capturedNpc);
                    tsorcScriptedEvents.SaveDynamicEvents();
                    RefreshList();
                };
                row.Append(delBtn);

                npcList.Add(row);
            }

            if (!string.IsNullOrEmpty(CurrentEvent.CustomAction) && CurrentEvent.CustomAction.Contains("Boulderfall"))
            {
                UIText boulderItem = new UIText("Falling Boulder (From Event Action)", 0.8f);
                boulderItem.TextColor = Color.Orange;
                npcList.Add(boulderItem);
            }
        }

        public void PopulateSearchList(string filter)
        {
            npcSearchList.Clear();
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
                    npcItem.TextColor = Color.Gold;
                };
                npcItem.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => {
                    npcItem.TextColor = Color.White;
                };
                int type = npc.type;
                npcItem.OnLeftClick += (UIMouseEvent evt, UIElement listeningElement) => {
                    var enemyUI = ModContent.GetInstance<tsorcRevamp>().EnemySelectionUI;
                    enemyUI.SelectedNpcType = type;
                    enemyUI.QuickAddMode = false; // Placing into the currently-open event, not a new quick-add event
                    Main.NewText("Selected " + npc.TypeName + " for spawning. Left click in world to place. Right click to cancel.");
                };
                npcSearchList.Add(npcItem);
            }
        }

        public void Show()
        {
            Visible = true;
            if (searchBar != null)
            {
                searchBar.Text = "";
            }
            PopulateSearchList("");
        }

        public void Hide()
        {
            Visible = false;

            // Clean up edit panel state on close; HideEditPanel also restores search section.
            if (editPanelAttached)
            {
                HideEditPanel();
            }

            CurrentEvent = null;
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

    // A click-to-focus text field that only accepts digits. Empty = no override (the Placeholder,
    // e.g. "Default (200)", shows what value will be used instead). Fires OnValueChanged on edit;
    // read the parsed result from Value (null when empty).
    class UINumericInputField : UIPanel
    {
        public string Text = "";
        public bool Focused = false;
        public string Placeholder = "";
        private int textBlinkerCount = 0;
        private int textBlinkerState = 0;
        private const int MaxDigits = 9; // keeps values within int range

        public event System.Action OnValueChanged;

        /// <summary>Parsed integer value, or null when the field is empty (use the default).</summary>
        public int? Value => int.TryParse(Text, out int v) ? v : (int?)null;

        public UINumericInputField()
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

            // Vertically center the text within the field box (was top-aligned, which read a few px low next to labels).
            CalculatedStyle inner = GetInnerDimensions();
            Vector2 textPos = inner.Position();
            float lineH = Terraria.GameContent.FontAssets.MouseText.Value.LineSpacing * 0.85f;
            textPos.Y += (inner.Height - lineH) / 2f;

            if (!Focused)
            {
                if (string.IsNullOrEmpty(Text))
                {
                    Utils.DrawBorderString(spriteBatch, Placeholder, textPos, Color.Gray, 0.85f);
                }
                else
                {
                    Utils.DrawBorderString(spriteBatch, Text, textPos, Color.Yellow, 0.85f);
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
                        // Keep digits only and cap the length so it stays a valid int.
                        string filtered = new string(newText.Where(char.IsDigit).ToArray());
                        if (filtered.Length > MaxDigits) filtered = filtered.Substring(0, MaxDigits);
                        if (filtered != Text)
                        {
                            Text = filtered;
                            OnValueChanged?.Invoke();
                        }
                    }
                }

                textBlinkerCount++;
                if (textBlinkerCount >= 20)
                {
                    textBlinkerState = (textBlinkerState + 1) % 2;
                    textBlinkerCount = 0;
                }

                string displayString = Text + (textBlinkerState == 1 ? "|" : "");
                Utils.DrawBorderString(spriteBatch, displayString, textPos, Color.White, 0.85f);
            }
        }
    }
}
