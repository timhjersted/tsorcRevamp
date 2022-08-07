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

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.RainbowWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
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


