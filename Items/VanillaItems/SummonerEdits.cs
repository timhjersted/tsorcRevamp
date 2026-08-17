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
                item.damage = 6;
            }
            if (item.type == ItemID.BabyBirdStaff)
            {
                item.damage = 7;
            }
            if (item.type == ItemID.SlimeStaff)
            {
                item.damage = 7;
            }
            if (item.type == ItemID.FlinxStaff)
            {
                item.damage = 8;
            }
            if (item.type == ItemID.VampireFrogStaff)
            {
                item.damage = 10;
            }
            if (item.type == ItemID.HornetStaff)
            {
                item.damage = 11;
            }
            if (item.type == ItemID.ImpStaff)
            {
                item.damage = 15;
            }
            if (item.type == ItemID.SpiderStaff)
            {
                item.damage = 22;
            }
            if (item.type == ItemID.SanguineStaff)
            {
                item.damage = 30;
            }
            if (item.type == ItemID.PirateStaff)
            {
                item.damage = 36;
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
                item.damage = 40;
            }
            if (item.type == ItemID.StormTigerStaff)
            {
                item.damage = 28;
            }
            if (item.type == ItemID.DeadlySphereStaff)
            {
                item.damage = 28;
            }
            if (item.type == ItemID.RavenStaff)
            {
                item.damage = 70; //SHM
            }
            if (item.type == ItemID.XenoStaff)
            {
                item.damage = 25;
            }
            
            if (item.type == ItemID.TempestStaff)
            {
                item.damage = 44;
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
            if (item.type == ItemID.ScytheWhip)
            {
                //item.damage = 160; 
            }
            #endregion

            #region Turrets/Sentries
            if (item.type == ItemID.DD2FlameburstTowerT1Popper)
            {
                item.damage = 22;
            }
            if (item.type == ItemID.DD2FlameburstTowerT2Popper)
            {
                item.damage = 56;
            }
            if (item.type == ItemID.DD2FlameburstTowerT3Popper)
            {
                item.damage = 112;
            }
            if (item.type == ItemID.DD2LightningAuraT1Popper)
            {
                item.damage = 6;
            }
            if (item.type == ItemID.DD2LightningAuraT2Popper)
            {
                item.damage = 16;
            }
            if (item.type == ItemID.DD2LightningAuraT3Popper)
            {
                item.damage = 43;
            }
            if (item.type == ItemID.HoundiusShootius) 	
            {
                item.damage = 30;
            }
            if (item.type == ItemID.StaffoftheFrostHydra) 
            {
                item.damage = 85;
            }
            if (item.type == ItemID.MoonlordTurretStaff) 
            {
                item.damage = 45;
            }
            if (item.type == ItemID.RainbowCrystalStaff) //now uses local immunity frames
            {
                item.damage = 58;
            }
            #endregion
        }
        public const float StardustDragonBaseDmgMult = 0.4f;

        public const float BeetleSummonTagStrengthBoost = 20f;
        public const float BeetleSummonCritChance = 5f;
        public const float ScrollSummonTagDurationBoost = 25f;
        public const float ScrollSummonCritChance = 10f;
        public const float ScarabTagBoost = 10f;
        public const float ScarabSummonCritChance = BeetleSummonCritChance + ScrollSummonCritChance - 4f;

        public const int LeatherWhipTagDmg = 5;
        public const int SnapthornTagDmg = 6;
        public const int SpinalTapTagDmg = 7;
        public const float FirecrackerScalingDmg = 200f;
        public const int CoolWhipTagDmg = 6;
        public const int DurendalTagDmg = 9;
        public const int DarkHarvestTagDmg = 10;
        public const float MorningStarTagDmg = 7;
        public const float MorningStarTagCritChance = 6;
        public const float KaleidoscopeTagDmg = 11;
        public const float KaleidoscopeTagCritChance = 8;
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            float SummonTagStrength = Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().SummonTagStrength;
            if (item.DamageType == DamageClass.Summon)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Damage");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "CritChance", (int)(item.crit + Main.LocalPlayer.GetTotalCritChance(DamageClass.Summon)) + Language.GetTextValue("LegacyTooltip.41")));
                }
            }
            if (item.DamageType == DamageClass.SummonMeleeSpeed)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Damage");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "CritChance", (int)(item.crit + Main.LocalPlayer.GetTotalCritChance(DamageClass.SummonMeleeSpeed)) + Language.GetTextValue("LegacyTooltip.41")));
                }
            }
            if (item.type == ItemID.BlandWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagDmg", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith((int)(LeatherWhipTagDmg * SummonTagStrength))));
                }
            }
            if (item.type == ItemID.ThornWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagDmg", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith((int)(SnapthornTagDmg * SummonTagStrength))));
                }
            }
            if (item.type == ItemID.BoneWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagDmg", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith((int)(SpinalTapTagDmg * SummonTagStrength))));
                }
            }
            if (item.type == ItemID.CoolWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagDmg", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith((int)(CoolWhipTagDmg * SummonTagStrength))));
                }
            }
            if (item.type == ItemID.FireWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip2");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex + 1, new TooltipLine(Mod, "TagDmg", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.Firecracker").FormatWith(FirecrackerScalingDmg)));
                }
            }
            if (item.type == ItemID.SwordWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagDmg", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith((int)(DurendalTagDmg * SummonTagStrength))));
                }
            }
            if (item.type == ItemID.MaceWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagDmg", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MorningStar").FormatWith((int)(MorningStarTagDmg * SummonTagStrength), (int)(MorningStarTagCritChance * SummonTagStrength))));
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
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagDmg", Language.GetTextValue("CommonItemTooltip.SummonTagDamage").FormatWith((int)(DarkHarvestTagDmg * SummonTagStrength))));
                }
            }
            if (item.type == ItemID.RainbowWhip)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "TagDmg", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.Kaleidoscope").FormatWith((int)(KaleidoscopeTagDmg * SummonTagStrength), (int)(KaleidoscopeTagCritChance * SummonTagStrength))));
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
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "SummonTagStrengthBoost", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.HerculesBeetle").FormatWith(BeetleSummonTagStrengthBoost, BeetleSummonCritChance)));
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
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "SummonTagDurationBoost", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.NecromanticScroll").FormatWith(ScrollSummonTagDurationBoost, ScrollSummonCritChance)));
                }
                int ttindex1 = tooltips.FindIndex(t => t.Name == "Tooltip1");
                if (ttindex1 != -1)
                {
                    tooltips.RemoveAt(ttindex1);
                }
                int ttindex2 = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex2 != -1)
                {
                    tooltips.RemoveAt(ttindex2);
                }
            }
            if (item.type == ItemID.PapyrusScarab)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "SummonTagStrengthDurationBoost", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.PapyrusScarab").FormatWith(ScarabTagBoost, ScarabSummonCritChance)));
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
                    damage *= 0.68f;
                }
                else if (item.type == ItemID.EmpressBlade & !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>())))
                {
                    damage *= 0.82f;
                }
                if (item.type == ItemID.XenoStaff & NPC.downedMartians)
                {
                    damage *= 1.4f;
                }
            }
        }
        public override void HoldItem(Item item, Player player)
        {
            if (item.DamageType == DamageClass.SummonMeleeSpeed && item.type != ModContent.ItemType<DarkSword>())
            {
                player.whipRangeMultiplier += player.GetAdjustedItemScale(player.HeldItem) - 1f; //apply weapon size modifiers to whip range
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


