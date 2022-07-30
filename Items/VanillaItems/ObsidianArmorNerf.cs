using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    class ObsidianArmorNerf : GlobalItem
    {


        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.ObsidianShirt)
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "SetBonus"); //find the last tooltip line
                if (ttindex != -1)
                {// if we find one
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "SetBonus", "Increases whip range by 50% and speed by 35%,\nIncreases minion damage by 15 %"));

                }
            }
        }
        public override string IsArmorSet(Item head, Item body, Item legs)
        {
            if (head.type == ItemID.ObsidianHelm && body.type == ItemID.ObsidianShirt && legs.type == ItemID.ObsidianPants)
            {
                return "Obsidian Armor";
            }
            else return base.IsArmorSet(head, body, legs);
        }

        public override void UpdateArmorSet(Player player, string set)
        {
            if (set == "Obsidian Armor")
            {
                player.setBonus = "Increases whip range by 20% and whip speed by 25%\nIncreases minion damage by 15%";

                player.whipRangeMultiplier -= 0.3f;
                player.GetAttackSpeed(DamageClass.Summon) -= 0.1f;
            }
        }
    }
}
