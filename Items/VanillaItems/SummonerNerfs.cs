using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.VanillaItems
{
    class SummonerNerfs : GlobalItem
    {

        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.RainbowWhip)
            {
                item.damage = 100;
            }
            if (item.type == ItemID.ScytheWhip)
            {
                item.damage = 70;
            }
            if (item.type == ItemID.MaceWhip)
            {
                item.damage = 115;
            }
            if (item.type == ItemID.BoneWhip)
            {
                item.damage = 24;
            }
            if (item.type == ItemID.Smolstar)//Blade Staff
            {
                item.damage = 1;//Powerful tag whips were added
            }

            //Lunar items
            if (item.type == ItemID.StardustDragonStaff)
            {
                item.damage = 35;
            }
        }

        public override void UpdateInventory(Item item, Player player)
        {
            if (item.type == ItemID.EmpressBlade & !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>()))
            {
                item.damage = 55;
            } else if (item.type == ItemID.EmpressBlade & !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>()))
            {
                item.damage = 70;
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
            if (item.type == ItemID.RainbowWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagNerfed", "11 summon tag damage"));
                    tooltips.Insert(ttindex + 2, new TooltipLine(Mod, "TagNerfed2", "Reduces minion damage by 5%"));
                }
            }
            if (item.type == ItemID.EmpressBlade && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>()))
            {
                int ttindex2 = tooltips.FindLastIndex(t => t.Name == "Tooltip0");
                if (ttindex2 != -1)
                {
                    tooltips.Insert(ttindex2 + 1, new TooltipLine(Mod, "Nerfed", "The full power of this blade has been sealed by an ancient knight\nDefeat him to partially unlock its power!(Reload world too)"));
                }
            } else
            if (item.type == ItemID.EmpressBlade && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>()))
            {
                int ttindex2 = tooltips.FindLastIndex(t => t.Name == "Tooltip0");
                if (ttindex2 != -1)
                {
                    tooltips.Insert(ttindex2 + 1, new TooltipLine(Mod, "Nerfed2", "The full power of this blade has been sealed by the embodiment of chaos\nDefeat it to fully unlock its power!(Reload world too)"));
                }
            }
        }

    }
}


