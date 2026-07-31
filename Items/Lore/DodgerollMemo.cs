using System.Collections.Generic;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Lore
{
    class DodgerollMemo : ModItem
    {

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.consumable = false;
            Item.value = 5000;
            Item.rare = ItemRarityID.Orange;
        }

        /// <summary>
        /// The "press X to roll" line is appended here rather than baked into the tooltip string, so it always
        /// names the key actually bound. It used to hard-code Left Alt, which was wrong for anyone on
        /// Recommended Controls (Left Shift) or their own binding.
        /// </summary>
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            List<string> assigned = tsorcRevamp.DodgerollKey.GetAssignedKeys();
            string key = assigned.Count > 0
                ? assigned[0]
                : LangUtils.GetTextValue("Keybinds.Dodge Roll.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");

            tooltips.Add(new TooltipLine(Mod, "DodgerollKey", LangUtils.GetTextValue("Items.DodgerollMemo.RollKey", key)));
            tooltips.Add(new TooltipLine(Mod, "DodgerollRebind", LangUtils.GetTextValue("Items.DodgerollMemo.RebindHint")));
        }
    }
}