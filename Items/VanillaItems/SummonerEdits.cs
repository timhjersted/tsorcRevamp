using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items.Armors.Summon;
using tsorcRevamp.Prefixes;

namespace tsorcRevamp.Items.VanillaItems
{
    class SummonerEdits : GlobalItem
    {

        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.RainbowWhip) //nerf
            {
                item.damage = 100;
            }
            if (item.type == ItemID.ScytheWhip) //nerf
            {
                item.damage = 85;
            }
            if (item.type == ItemID.MaceWhip) //nerf
            {
                item.damage = 95;
            }
            if (item.type == ItemID.BoneWhip) //nerf
            {
                item.damage = 24;
            }

            if (item.type == ItemID.StaffoftheFrostHydra) //buff so it's an actually decent reward
            {
                item.damage = 160;
            }

            //Lunar items
            if (item.type == ItemID.StardustDragonStaff) //holy the scaling on this weapon is insane, it needs a nerf
            {
                item.damage = 40;
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
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagNerfed", "10 summon tag damage"));
                }
                int ttindex2 = tooltips.FindIndex(t => t.Name == "Tooltip1");
                if (ttindex2 != -1)
                {
                    tooltips.RemoveAt(ttindex2);
                    tooltips.Insert(ttindex2, new TooltipLine(Mod, "TagCritNerfed", "5% summon tag critical strike chance"));
                }
            }
            if (item.type == ItemID.MaceWhip)
            {
                int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex1 != -1)
                {
                    tooltips.RemoveAt(ttindex1);
                    tooltips.Insert(ttindex1, new TooltipLine(Mod, "TagNerfed", "4 summon tag damage"));
                }
                int ttindex2 = tooltips.FindIndex(t => t.Name == "Tooltip1");
                if (ttindex2 != -1)
                {
                    tooltips.RemoveAt(ttindex2);
                    tooltips.Insert(ttindex2, new TooltipLine(Mod, "TagCritNerfed", "6% summon tag critical strike chance"));
                }
            }
            if (item.type == ItemID.EmpressBlade && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>())))
            {
                int ttindex2 = tooltips.FindLastIndex(t => t.Name == "Tooltip0");
                if (ttindex2 != -1)
                {
                    tooltips.Insert(ttindex2 + 1, new TooltipLine(Mod, "Nerfed", "The full power of this blade has been sealed by an ancient knight\nDefeat him to partially unlock its power!"));
                }
            } else
            if (item.type == ItemID.EmpressBlade && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>())))
            {
                int ttindex2 = tooltips.FindLastIndex(t => t.Name == "Tooltip0");
                if (ttindex2 != -1)
                {
                    tooltips.Insert(ttindex2 + 1, new TooltipLine(Mod, "Nerfed2", "The full power of this blade has been sealed by the embodiment of chaos\nDefeat it to fully unlock its power!"));
                }
            }
        }
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (tsorcRevampWorld.NewSlain != null)
            {
                if (item.type == ItemID.EmpressBlade & !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>())))
                {
                    damage *= 0.75f;
                }
                else if (item.type == ItemID.EmpressBlade & !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>())))
                {
                    damage *= 0.88f;
                }
            }
        }
        public override void HoldItem(Item item, Player player)
        {
            float scaleDelta;
            if (item.DamageType == DamageClass.SummonMeleeSpeed)
            {
                if (item.prefix == ModContent.PrefixType<Brave>())
                {
                    scaleDelta = -0.1f;
                }
                else if (item.prefix == ModContent.PrefixType<Reckless>())
                {
                    scaleDelta = -0.2f;
                }
                else
                    switch (item.prefix)
                {

                    case PrefixID.Large:

                        scaleDelta = 0.12f;
                        break;

                    case PrefixID.Massive:

                        scaleDelta = 0.18f;
                        break;

                    case PrefixID.Dangerous:

                        scaleDelta = 0.06f;
                        break;

                    case PrefixID.Tiny:

                        scaleDelta = -0.18f;
                        break;

                    case PrefixID.Terrible:

                        scaleDelta = -0.14f;
                        break;

                    case PrefixID.Small:

                        scaleDelta = -0.1f;
                        break;

                    case PrefixID.Unhappy:

                        scaleDelta = -0.1f;
                        break;

                    case PrefixID.Bulky:

                        scaleDelta = 0.1f;
                        break;

                    case PrefixID.Shameful:

                        scaleDelta = 0.1f;
                        break;

                    case PrefixID.Legendary:

                        scaleDelta = 0.1f;
                        break;

                    default:
                        scaleDelta = 0;
                        break;
                }
                player.whipRangeMultiplier += scaleDelta;
            }
        }
        public override string IsArmorSet(Item head, Item body, Item legs)
        {
            if (head.type == ModContent.ItemType<OldChainCoif>() && body.type == ItemID.FlinxFurCoat && legs.type == ModContent.ItemType<OldChainGreaves>())
            {
                return "FlinxFurChained";
            }
            else return base.IsArmorSet(head, body, legs);
        }
        public override void UpdateArmorSet(Player player, string set)
        {
            if (set == "FlinxFurChained")
            {
                player.setBonus = "Increases your max number of minions by 1";

                player.maxMinions += 1;
            }
        }

    }
}


