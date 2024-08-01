using Humanizer;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items.Armors.Summon;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Prefixes;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.VanillaItems
{
    class SummonerEdits : GlobalItem
    {

        public override void SetDefaults(Item item)
        {
            #region Minions
            if (item.type == ItemID.AbigailsFlower)
            {
                item.damage = 5;
            }
            if (item.type == ItemID.BabyBirdStaff)
            {
                item.damage = 3;
            }
            if (item.type == ItemID.BabyBirdStaff)
            {
                float SlotsRequired = 0.5f;
            }
            if (item.type == ItemID.SlimeStaff)
            {
                item.damage = 7;
            }
            if (item.type == ItemID.FlinxStaff)
            {
                item.damage = 6;
            }
            if (item.type == ItemID.VampireFrogStaff)
            {
                item.damage = 8;
            }
            if (item.type == ItemID.HornetStaff)
            {
                item.damage = 11;
            }
            if (item.type == ItemID.ImpStaff)
            {
                item.damage = 14;
            }
            if (item.type == ItemID.SpiderStaff)
            {
                item.damage = 21;
            }
            if (item.type == ItemID.SanguineStaff)
            {
                item.damage = 29;
            }
            if (item.type == ItemID.PirateStaff)
            {
                item.damage = 37;
            }
            if (item.type == ItemID.Smolstar)
            {
                item.damage = 5;
            }
            if (item.type == ItemID.OpticStaff)
            {
                item.damage = 22;
            }
            if (item.type == ItemID.PygmyStaff)
            {
                item.damage = 37;
            }
            if (item.type == ItemID.StormTigerStaff)
            {
                item.damage = 33;
            }
            if (item.type == ItemID.DeadlySphereStaff)
            {
                item.damage = 34;
            }
            if (item.type == ItemID.RavenStaff)
            {
                item.damage = 47;
            }
            if (item.type == ItemID.XenoStaff)
            {
                item.damage = 28;
            }
            if (item.type == ItemID.TempestStaff)
            {
                //not gonna nerf this so it might actually have a use
            }
            if (item.type == ItemID.StardustCellStaff)
            {
                //not gonna nerf this so it might actually have a use
            }
            if (item.type == ItemID.StardustDragonStaff)
            {
                //nerf in another function
            }
            if (item.type == ItemID.EmpressBlade)
            {
                //nerf in another function
            }
            #endregion

            #region Whips
            if (item.type == ItemID.RainbowWhip)
            {
                item.damage = 100;
            }
            if (item.type == ItemID.ScytheWhip)
            {
                item.damage = 85;
            }
            if (item.type == ItemID.MaceWhip)
            {
                item.damage = 95;
            }
            #endregion

            #region Turrets/Sentries
            if (item.type == ItemID.HoundiusShootius)
            {
                item.damage = 25;
            }
            if (item.type == ItemID.StaffoftheFrostHydra) //buff, using static immunity frame in tml1.4.4, vanilla damage is 100
            {//now uses local iframes in globalprojectile
                item.damage = 110;
            }
            #endregion
        }
        public static int LeatherWhipTagDmg = 4;
        public static int SnapthornTagDmg = 6;
        public static int SpinalTapTagDmg = 7;
        public static float FirecrackerScalingDmg = 275f;
        public static int CoolWhipTagDmg = 6;
        public static int DurendalTagDmg = 9;
        public static int DarkHarvestTagDmg = 10;
        public static float MorningStarTagDmg = 6;
        public static float MorningStarTagCritChance = 12;
        public static float KaleidoscopeTagDmg = 10;
        public static float KaleidoscopeTagCritChance = 10;
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.DamageType == DamageClass.Summon || item.DamageType == DamageClass.SummonMeleeSpeed)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Damage");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "CritChance", (int)(item.crit + Main.LocalPlayer.GetTotalCritChance(DamageClass.Summon)) + Language.GetTextValue("LegacyTooltip.41")));
                }
            }
            if (item.type == ItemID.BlandWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagNerfed", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith(LeatherWhipTagDmg)));
                }
            }
            if (item.type == ItemID.ThornWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagNerfed", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith(SnapthornTagDmg)));
                }
            }
            if (item.type == ItemID.BoneWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagNerfed", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith(SpinalTapTagDmg)));
                }
            }
            if (item.type == ItemID.CoolWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagNerfed", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith(CoolWhipTagDmg)));
                }
            }
            if (item.type == ItemID.FireWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip2");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "TagNerfed", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.Firecracker").FormatWith(FirecrackerScalingDmg)));
                }
            }
            if (item.type == ItemID.SwordWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagNerfed", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith(DurendalTagDmg)));
                }
            }
            if (item.type == ItemID.MaceWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagNerfed", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MorningStar").FormatWith(MorningStarTagDmg, MorningStarTagCritChance)));
                }
                int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip1");
                if (ttindex1 != -1)
                {
                    tooltips.RemoveAt(ttindex1);
                }
            }
            if (item.type == ItemID.ScytheWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagNerfed", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith(DarkHarvestTagDmg)));
                }
            }
            if (item.type == ItemID.RainbowWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagNerfed", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.Kaleidoscope").FormatWith(KaleidoscopeTagDmg, KaleidoscopeTagCritChance)));
                }
                int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip1");
                if (ttindex1 != -1)
                {
                    tooltips.RemoveAt(ttindex1);
                }
            }
            if (item.type == ItemID.EmpressBlade && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>())))
            {
                int ttindex = tooltips.FindLastIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "Nerfed", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.TerraprismaTier1")));
                }
            }
            else
            if (item.type == ItemID.EmpressBlade && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>())))
            {
                int ttindex1 = tooltips.FindLastIndex(t => t.Name == "Tooltip0");
                if (ttindex1 != -1)
                {
                    tooltips.Insert(ttindex1 + 1, new TooltipLine(Mod, "StillNerfed", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.TerraprismaTier2")));
                }
            }
            if (item.type == ItemID.HerculesBeetle)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "SummonTagStrengthBoost", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.HerculesBeetle").FormatWith(MethodSwaps.ScrollSummonTagDurationBoost, MethodSwaps.BeetleSummonCritChance)));
                }
                int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip1");
                if (ttindex1 != -1)
                {
                    tooltips.RemoveAt(ttindex1);
                }
            }
            if (item.type == ItemID.NecromanticScroll)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "SummonTagDurationBoost", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.NecromanticScroll").FormatWith(MethodSwaps.ScrollSummonTagDurationBoost, MethodSwaps.ScrollSummonCritChance)));
                }
                int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip1");
                if (ttindex1 != -1)
                {
                    tooltips.RemoveAt(ttindex1);
                }
            }
            if (item.type == ItemID.PapyrusScarab)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "SummonTagStrengthDurationBoost", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.PapyrusScarab").FormatWith(MethodSwaps.BeetleSummonTagStrengthBoost, MethodSwaps.ScrollSummonTagDurationBoost, MethodSwaps.ScarabSummonCritChance)));
                }
                int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip1");
                if (ttindex1 != -1)
                {
                    tooltips.RemoveAt(ttindex1);
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
                else if (item.prefix == ModContent.PrefixType<Tenacious>())
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
        public static int FlinxFurMaxMinionIncrease = 1;
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
                player.setBonus = Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.FlinxFurChainedSetBonus").FormatWith(FlinxFurMaxMinionIncrease);
                player.maxMinions += FlinxFurMaxMinionIncrease;
            }
        }

    }
}


