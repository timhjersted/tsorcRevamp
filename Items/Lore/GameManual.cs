using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Lore
{
    // PLACEHOLDER sprite (copy of Items/Lore/DodgerollMemo) — replace with bespoke art.

    /// <summary>
    /// Starting-inventory reference book explaining the mod's non-vanilla systems.
    ///
    /// Paged the same way the Darksign previews class mechanics: right-click cycles a per-instance counter that
    /// ModifyTooltips switches on. Page bodies live entirely in localization as multi-line strings and are split
    /// into one TooltipLine each, so writing or reordering a page never touches C#.
    ///
    /// One page is interactive — CanUseItem only returns true on the Controls page, so left-click toggles
    /// Recommended Controls there and does nothing anywhere else. Same shape as the Darksign, which gates
    /// CanUseItem on game mode.
    /// </summary>
    public class GameManual : ModItem
    {
        /// <summary>Total pages. Must match the number of Page* entries in localization.</summary>
        public const int PageCount = 11;

        /// <summary>The one page with a left-click action (see CanUseItem / UseItem).</summary>
        private const int ControlsPage = 3;

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

        // Left-click is inert except on the Controls page, so there's no way to trigger the toggle by accident
        // while flipping through.
        public override bool CanUseItem(Player player)
        {
            return Page == ControlsPage;
        }

        public override bool? UseItem(Player player)
        {
            if (player.whoAmI != Main.myPlayer || Main.netMode == NetmodeID.Server)
            {
                return false;
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

            // "Page 3 of 11 — Controls", so flipping is predictable and the player knows it wraps.
            string heading = LangUtils.GetTextValue("Items.GameManual.Heading" + Page);
            tooltips.Insert(++index, new TooltipLine(Mod, "ManualHeader",
                LangUtils.GetTextValue("Items.GameManual.PageHeader", Page, PageCount, heading))
            { OverrideColor = new Color(255, 223, 80) });

            // Page bodies are one multi-line localization string each; split so each line lays out properly.
            string body = LangUtils.GetTextValue("Items.GameManual.Page" + Page);
            foreach (string line in body.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    tooltips.Insert(++index, new TooltipLine(Mod, "ManualBody", line.TrimEnd())
                    { OverrideColor = new Color(200, 220, 240) });
                }
            }

            tooltips.Insert(++index, new TooltipLine(Mod, "ManualTurn",
                LangUtils.GetTextValue(Page == ControlsPage ? "Items.GameManual.TurnAndToggle" : "Items.GameManual.Turn"))
            { OverrideColor = new Color(150, 150, 150) });
        }
    }
}
