using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Systems.Regain;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Lore
{
    // PLACEHOLDER sprite (copy of Items/Lore/DodgerollMemo) — replace with bespoke art.

    /// <summary>
    /// Starting-inventory reference book explaining the mod's non-vanilla systems.
    ///
    /// Paged the same way the Darksign previews class mechanics: right-click cycles a per-instance counter that
    /// ModifyTooltips switches on. Page bodies live entirely in localization as multi-line strings and are split
    /// into one TooltipLine each, so writing or reordering a page never touches C#.
    ///
    /// Three pages are interactive — CanUseItem only returns true on the Controls page and the two UI-option pages,
    /// so left-click toggles that page's setting and does nothing anywhere else. Same shape as the Darksign, which
    /// gates CanUseItem on game mode.
    /// </summary>
    public class GameManual : ModItem
    {
        /// <summary>Total pages. Must match the number of Page* entries in localization.</summary>
        public const int PageCount = 15;

        /// <summary>Pages with a left-click action (see CanUseItem / UseItem).</summary>
        private const int ControlsPage = 3;
        private const int DodgeRollPage = 6;
        private const int RegainPage = 7;
        private const int DamageNumbersPage = 14;
        private const int MinimalMinimapPage = 15;

        /// <summary>Per-instance so the book remembers where you left off, like the Darksign's ClassCounter.</summary>
        public int Page = 1;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.consumable = false;
            Item.useTime = 30;
            Item.useAnimation = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.UseSound = SoundID.MenuOpen;
            Item.rare = ItemRarityID.Blue;
            Item.value = 0;
        }

        public override bool CanRightClick()
        {
            return true;
        }

        public override void RightClick(Player player)
        {
            // Right-clicking an inventory item normally consumes one. Bumping the stack first cancels that out,
            // so the book survives being read — same trick the Darksign uses.
            Item.stack++;
            Page++;
            if (Page > PageCount)
            {
                Page = 1;
            }
        }

        // Left-click is inert except on the toggle pages, so there's no way to trigger a toggle by accident
        // while flipping through.
        public override bool CanUseItem(Player player)
        {
            if (Page == ControlsPage || Page == DamageNumbersPage || Page == MinimalMinimapPage)
            {
                return true;
            }

            return false;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server)
            {
                return false;
            }

            // The two UI-option pages flip a client-side Visual config bool and save it, so the choice survives a restart.
            // Both are read live every frame (MethodSwaps), so the change shows immediately.
            if (Page == DamageNumbersPage || Page == MinimalMinimapPage)
            {
                tsorcRevampVisualConfig visualConfig = ModContent.GetInstance<tsorcRevampVisualConfig>();
                bool nowOn;
                string messageKey;

                if (Page == DamageNumbersPage)
                {
                    visualConfig.HideEnemyDamageNumbers = !visualConfig.HideEnemyDamageNumbers;
                    nowOn = visualConfig.HideEnemyDamageNumbers;
                    messageKey = "Items.GameManual.DamageNumbersToggled";
                }
                else
                {
                    visualConfig.OnlyShowMapWhenInventoryIsOpen = !visualConfig.OnlyShowMapWhenInventoryIsOpen;
                    nowOn = visualConfig.OnlyShowMapWhenInventoryIsOpen;
                    messageKey = "Items.GameManual.MinimalMinimapToggled";
                }

                tsorcRevamp.SaveConfigToDisk(visualConfig);

                string stateText = LangUtils.GetTextValue(nowOn ? "Items.GameManual.StateOn" : "Items.GameManual.StateOff");
                Color messageColor = Color.LightGray;

                if (nowOn)
                {
                    messageColor = Color.LightGreen;
                }

                Main.NewText(LangUtils.GetTextValue(messageKey, stateText), messageColor);
                return true;
            }

            if (!tsorcRevamp.TryToggleRecommendedControlsFromItem(out bool enabled))
            {
                Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.RecommendedControls.ToggleFailed"), Color.OrangeRed);
                return false;
            }

            Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.Items.RecommendedControls." + (enabled ? "Enabled" : "Disabled")),
                enabled ? Color.LightGreen : Color.LightGray);
            return true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            int index = tooltips.FindLastIndex(t => t.Mod == "Terraria");
            if (index < 0)
            {
                index = tooltips.Count - 1;
            }

            // "Page 3 of 12 — Controls", so flipping is predictable and the player knows it wraps.
            string heading = LangUtils.GetTextValue("Items.GameManual.Heading" + Page);
            tooltips.Insert(++index, new TooltipLine(Mod, "ManualHeader",
                LangUtils.GetTextValue("Items.GameManual.PageHeader", Page, PageCount, heading))
            { OverrideColor = new Color(255, 223, 80) });

            // Every number a page quotes is read from the constant that drives it, filled into that page's {0}, {1}...
            // so balance changes can't leave the manual stale. Pages without placeholders get no args.
            // Ticks are converted to seconds and fractions to percentages here; the localization only adds the unit.
            object[] pageArgs = System.Array.Empty<object>();

            if (Page == DodgeRollPage)
            {
                int buffSeconds = Buffs.PerfectDodge.DurationTicks / 60;
                pageArgs = new object[] { Buffs.PerfectDodge.StaminaRegenBonus, Buffs.PerfectDodge.CriticalStrikeChanceBonus, buffSeconds };
            }
            else if (Page == RegainPage)
            {
                int holdSeconds = Regain.HoldTicks / 60;
                float poolCapPercent = Regain.MaxPoolFractionOfMaxLife * 100f;
                int hitWindowSeconds = Regain.ConsecutiveHitResetTicks / 60;
                pageArgs = new object[] { holdSeconds, poolCapPercent, hitWindowSeconds, Regain.FullRegainRangeTiles };
            }
            else if (Page == DamageNumbersPage || Page == MinimalMinimapPage)
            {
                // Toggle pages end with "Currently: {0}" — a coloured On/Off so the state is visible before clicking.
                tsorcRevampVisualConfig visualConfig = ModContent.GetInstance<tsorcRevampVisualConfig>();
                bool toggleIsOn = visualConfig.OnlyShowMapWhenInventoryIsOpen;

                if (Page == DamageNumbersPage)
                {
                    toggleIsOn = visualConfig.HideEnemyDamageNumbers;
                }

                string stateText = LangUtils.GetTextValue(toggleIsOn ? "Items.GameManual.StateOnColored" : "Items.GameManual.StateOffColored");
                pageArgs = new object[] { stateText };
            }

            // Page bodies are one multi-line localization string each; split so each line lays out properly.
            string body = LangUtils.GetTextValue("Items.GameManual.Page" + Page, pageArgs);
            foreach (string line in body.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    tooltips.Insert(++index, new TooltipLine(Mod, "ManualBody", line.TrimEnd())
                    { OverrideColor = new Color(200, 220, 240) });
                }
            }

            string turnKey = "Items.GameManual.Turn";

            if (Page == ControlsPage)
            {
                turnKey = "Items.GameManual.TurnAndToggle";
            }
            else if (Page == DamageNumbersPage)
            {
                turnKey = "Items.GameManual.TurnAndToggleDamageNumbers";
            }
            else if (Page == MinimalMinimapPage)
            {
                turnKey = "Items.GameManual.TurnAndToggleMinimalMinimap";
            }

            tooltips.Insert(++index, new TooltipLine(Mod, "ManualTurn", LangUtils.GetTextValue(turnKey))
            { OverrideColor = new Color(150, 150, 150) });
        }
    }
}
