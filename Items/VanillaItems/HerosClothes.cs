using Humanizer;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    class HerosClothes : GlobalItem
    {

        public static float CritChance = 5f;
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.HerosShirt)
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria"); //find the last tooltip line
                if (ttindex != -1)
                {// if we find one
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "CritChance", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.HerosShirt").FormatWith(CritChance)));
                }
            }
        }
        public override string IsArmorSet(Item head, Item body, Item legs)
        {
            if (head.type == ItemID.HerosHat && body.type == ItemID.HerosShirt && legs.type == ItemID.HerosPants)
            {
                return "Hero's Clothes";
            }
            else return base.IsArmorSet(head, body, legs);
        }

        public override void UpdateArmorSet(Player player, string set)
        {
            if (set == "Hero's Clothes")
            {
                player.GetCritChance(DamageClass.Generic) += CritChance / 100f;
            }
        }

        public override void SetDefaults(Item item)
        {

            if (item.type == ItemID.HerosHat)
            {
                item.vanity = false;
                item.defense = 4;
            }
            if (item.type == ItemID.HerosShirt)
            {
                item.vanity = false;
                item.defense = 7;
            }
            if (item.type == ItemID.HerosPants)
            {
                item.vanity = false;
                item.defense = 4;
            }
        }
    }
}
