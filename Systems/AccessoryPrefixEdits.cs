using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems;

public class AccessoryPrefixTooltipEdits : GlobalItem
{
    public override void Load()
    {
        On_Player.GrantPrefixBenefits += CustomAccessoryPrefixStats;
    }
    public const int HardDefense = 2;
    public const float HardThornsGain = 10f;
    public const int GuardingDefense = 1;
    public const float GuardingStaminaRegenBonus = 5f;
    public const float ArmoredResistanceGain = 4f;
    //Warding unchanged
    public const int JaggedFlatCritDmgBonus = 4;
    public const float SpikedThornsGain = 25f;
    public const int SpikedBadDefense = 2;
    public const float AngryDmg = 2f;
    public const float AngryStaminaRegenBonus = 3f;
    //Menacing unchanged
    public const float PreciseRangedCritChance = 5f;
    public const float LuckyCritChance = 4f;
    public const float LuckyPercentLuck = 4f;
    public const float BriskMoveSpeed = 3f;
    public const float BriskDmg = 1f;
    public const float FleetingMovespeed = 9f;
    public const float FleetingBadResistance = 2f;
    public const float HastyMovespeed = 7f;
    public const float HastyBadCrit = 2f;
    public const float QuickMovespeed = 5f;
    public const float WildAttackSpeed = 3f;
    public const float WildBadResistance = 3f;
    public const float RashAttackSpeed = 5f;
    public const float RashBadLifeRegen = 1f;
    public const float IntrepidAttackSpeed = 1f;
    public const int IntrepidDefense = 1;
    public const int ArcaneManaGain = 20;
    public const float ArcaneManaCostReduction = 4f;
        private static void CustomAccessoryPrefixStats(On_Player.orig_GrantPrefixBenefits orig, Player self, Item item)
        {
            var modPlayer = self.GetModPlayer<tsorcRevampPlayer>();
            var staminaPlayer = self.GetModPlayer<tsorcRevampStaminaPlayer>();
            if (item.prefix == PrefixID.Hard) //default +1def
            {
                self.statDefense += HardDefense;
                self.thorns += HardThornsGain / 100f; 
            }
            if (item.prefix == PrefixID.Guarding) //default +2def
            {
                self.statDefense += GuardingDefense;
                staminaPlayer.staminaResourceGainMult += GuardingStaminaRegenBonus / 100f;
            }
            if (item.prefix == PrefixID.Armored) //default +3def
            {
                self.endurance += ArmoredResistanceGain / 100f;
            }
            if (item.prefix == PrefixID.Warding) //default
            {
                self.statDefense += 4;
            }
            if (item.prefix == PrefixID.Jagged) //default +1% dmg
            {
                modPlayer.JaggedFlatCritDmgBonus += JaggedFlatCritDmgBonus;
            }
            if (item.prefix == PrefixID.Spiked) //default +2% dmg
            {
                self.thorns += SpikedThornsGain / 100f;
                self.statDefense -= 2;
            }
            if (item.prefix == PrefixID.Angry) //default +3% dmg
            {
                self.GetDamage(DamageClass.Generic) += AngryDmg / 100f;
                staminaPlayer.staminaResourceGainMult += AngryStaminaRegenBonus / 100f;
            }
            if (item.prefix == PrefixID.Menacing) //default
            {
                self.GetDamage(DamageClass.Generic) += 0.04f;
            }
            if (item.prefix == PrefixID.Precise) //default +2 crit chance
            {
                self.GetCritChance(DamageClass.Ranged) += PreciseRangedCritChance;
            }
            if (item.prefix == PrefixID.Lucky) //default +4 crit chance
            {
                self.GetCritChance(DamageClass.Generic) += LuckyCritChance;
                self.luck += LuckyPercentLuck / 100f;
            }
            if (item.prefix == PrefixID.Brisk) //default 1% movespeed
            {
                self.moveSpeed += BriskMoveSpeed / 100f;
                self.GetDamage(DamageClass.Generic) += BriskDmg / 100f;
            }
            if (item.prefix == PrefixID.Fleeting) //default 2% movespeed
            {
                self.moveSpeed += FleetingMovespeed / 100f;
                self.endurance -= FleetingBadResistance / 100f;
            }
            if (item.prefix == PrefixID.Hasty2) //default 3% movespeed
            {
                self.moveSpeed += HastyMovespeed / 100f;
                self.GetCritChance(DamageClass.Generic) -= HastyBadCrit;
            }
            if (item.prefix == PrefixID.Quick2) //default 4% movespeed
            {
                self.moveSpeed += QuickMovespeed / 100f;
            }
            if (item.prefix == PrefixID.Wild) //default 1% melee speed
            {
                self.GetAttackSpeed(DamageClass.Generic) += WildAttackSpeed / 100f;
                self.endurance -= WildBadResistance / 100f;
            }
            if (item.prefix == PrefixID.Rash) //default 2% melee speed
            {
                self.GetAttackSpeed(DamageClass.Generic) += RashAttackSpeed / 100f;
                modPlayer.RashBadLifeRegen += (int)RashBadLifeRegen;
            }
            if (item.prefix == PrefixID.Intrepid) //default 3% melee speed
            {
                self.GetAttackSpeed(DamageClass.Generic) += IntrepidAttackSpeed / 100f;
                self.statDefense += IntrepidDefense;
            }
            if (item.prefix == PrefixID.Violent) //default
            {
                self.GetAttackSpeed(DamageClass.Melee) += 0.04f;
            }
            if (item.prefix == PrefixID.Arcane) //default +20 max mana
            {
                self.statManaMax2 += ArcaneManaGain; 
                self.manaCost -= ArcaneManaCostReduction / 100f;
            }
            PrefixLoader.ApplyAccessoryEffects(self, item);
        }

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) //alter tooltips
    {
        Player player = Main.LocalPlayer;
        var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
        if (item.prefix == PrefixID.Hard)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccDefense");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "Thorns", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Hard", HardDefense, (int)HardThornsGain)));
            }
        }
        if (item.prefix == PrefixID.Guarding)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccDefense");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "StaminaBonuses", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Guarding", GuardingDefense, (int)GuardingStaminaRegenBonus)));
            }
        }
        if (item.prefix == PrefixID.Armored)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccDefense");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "RealDR", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Armored", (int)ArmoredResistanceGain)));
            }
        }
        //Warding unchanged
        if (item.prefix == PrefixID.Jagged)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccDamage");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "FlatCritDmg", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Jagged", JaggedFlatCritDmgBonus)));
            }
        }
        if (item.prefix == PrefixID.Spiked)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccDamage");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "NastyThorns", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Spiked", (int)SpikedThornsGain, SpikedBadDefense)));
            }
        }
        if (item.prefix == PrefixID.Angry)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccDamage");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "RAWR", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Angry", (int)AngryDmg, (int)AngryStaminaRegenBonus)));
            }
        }
        //Menacing unchanged
        if (item.prefix == PrefixID.Precise)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccCritChance");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "RangedCritChance", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Precise", (int)PreciseRangedCritChance)));
            }
        }
        if (item.prefix == PrefixID.Lucky)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccCritChance");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "RealLuckyCrits", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Lucky", (int)LuckyCritChance, (int)LuckyPercentLuck)));
            }
        }
        if (item.prefix == PrefixID.Brisk)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccMoveSpeed");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "QuickButSharpTone", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Brisk", (int)BriskMoveSpeed, (int)BriskDmg)));
            }
        }
        if (item.prefix == PrefixID.Fleeting)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccMoveSpeed");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "FleetingTruly", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Fleeting", (int)FleetingMovespeed, (int)FleetingBadResistance)));
            }
        }
        if (item.prefix == PrefixID.Hasty2)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccMoveSpeed");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "CanGoTooFast", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Hasty", (int)HastyMovespeed, (int)HastyBadCrit)));
            }
        }
        if (item.prefix == PrefixID.Quick2)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccMoveSpeed");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "RealCritChance", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Quick", (int)QuickMovespeed)));
            }
        }
        if (item.prefix == PrefixID.Wild)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccMeleeSpeed");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "AbsolutelyWild", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Wild", (int)WildAttackSpeed, (int)WildBadResistance)));
            }
        }
        if (item.prefix == PrefixID.Rash)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccMeleeSpeed");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "UrSick", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Rash", (int)RashAttackSpeed, RashBadLifeRegen / 2f)));
            }
        }
        if (item.prefix == PrefixID.Intrepid)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccMeleeSpeed");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "CourageousFearlessIDK", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Intrepid", (int)IntrepidAttackSpeed, IntrepidDefense)));
            }
        }
        if (item.prefix == PrefixID.Arcane)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccMaxMana");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "ManaEfficiency", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Arcane", (int)((float)ArcaneManaGain * (1f + modPlayer.MaxManaAmplifier / 100f)), ArcaneManaCostReduction)));
            }
        }
    }
}