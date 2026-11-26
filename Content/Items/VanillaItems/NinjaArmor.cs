using System.Collections.Generic;
using Humanizer;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.VanillaItems
{
    class NinjaArmor : GlobalItem
    {
        public const float MaxStamina = 8f;
        public const float StaminaRegen = 8f;

        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.NinjaHood)
            {
                item.vanity = false;
                item.defense = 3;
            }
            if (item.type == ItemID.NinjaShirt)
            {
                item.vanity = false;
                item.defense = 4;
            }
            if (item.type == ItemID.NinjaPants)
            {
                item.vanity = false;
                item.defense = 3;
            }
        }

        public override string IsArmorSet(Item head, Item body, Item legs)
        {
            if (head.type == ItemID.NinjaHood && body.type == ItemID.NinjaShirt && legs.type == ItemID.NinjaPants)
            {
                return "Ninja Armor";
            }

            return base.IsArmorSet(head, body, legs);
        }

        public override void UpdateArmorSet(Player player, string set)
        {
            if (set == "Ninja Armor")
            {
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2 *= 1f + MaxStamina / 100f;
                player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceGainMult += StaminaRegen / 100f;
                player.setBonus += "\n" + LangUtils.GetTextValue("Items.VanillaItems.NinjaSetBonus", MaxStamina, StaminaRegen);
            }
        }

        private void AddNinjaTooltip(string key, List<TooltipLine> tooltips, params object[] args)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0"); //try Tooltipx until you're in the right place or just add more to ttindex below
            if (ttindex != -1)
            {
                tooltips.Insert(ttindex + 1, new TooltipLine(Mod, key, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems." + key).FormatWith(args)));
            }
        }
    }
}
