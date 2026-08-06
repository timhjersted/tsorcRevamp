using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems;

public class AccessoryPrefixTooltipEdits : GlobalItem
{

    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) //alter tooltips
    {
        if (item.prefix == PrefixID.Arcane)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccMaxMana");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "RealMaxManaGain", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Arcane", AccessoryPrefixEditsStats.ArcaneManaGain, AccessoryPrefixEditsStats.ArcaneManaCostReduction)));
            }
        }
        if (item.prefix == PrefixID.Hard)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccDefense");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "RealDR", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Hard", (int)AccessoryPrefixEditsStats.HardResistanceGain)));
            }
        }
        if (item.prefix == PrefixID.Guarding)
        {
            int ttindex = tooltips.FindIndex(t => t.Name == "PrefixAccDefense");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "RealDR", LangUtils.GetTextValue("CommonItemTooltip.AccessoryPrefixes.Guarding", (int)AccessoryPrefixEditsStats.GuardingResistanceGain)));
            }
        }
    }
}

class AccessoryPrefixEditsStats
{
    public const int ArcaneManaGain = 40;
    public const float ArcaneManaCostReduction = 4f;
    public const float HardResistanceGain = 2f;
    public const float GuardingResistanceGain = 3f;
    internal static void ApplyAccessoryPrefixEdits()
    {
        On_Player.GrantPrefixBenefits += CustomAccessoryPrefixStats;
    }        
    //determines the stats gained by accessory prefixes
        private static void CustomAccessoryPrefixStats(On_Player.orig_GrantPrefixBenefits orig, Player self, Item item)
        {
            if (item.prefix == PrefixID.Hard) //default +1def
            {
                self.endurance += HardResistanceGain / 100f; 
            }
            if (item.prefix == PrefixID.Guarding) //default +2def
            {
                self.endurance += GuardingResistanceGain / 100f; 
            }
            if (item.prefix == PrefixID.Armored)
            {
                self.statDefense += 3;
            }
            if (item.prefix == PrefixID.Warding)
            {
                self.statDefense += 4;
            }
            if (item.prefix == PrefixID.Arcane) //default +20 max mana
            {
                self.statManaMax2 += ArcaneManaGain; 
                self.manaCost -= ArcaneManaCostReduction / 100f;
            }
            if (item.prefix == PrefixID.Precise)
            {
                self.GetCritChance(DamageClass.Generic) += 2f;
            }
            if (item.prefix == PrefixID.Lucky)
            {
                self.GetCritChance(DamageClass.Generic) += 4f;
            }
            if (item.prefix == PrefixID.Jagged)
            {
                self.GetDamage(DamageClass.Generic) += 0.01f;
            }
            if (item.prefix == PrefixID.Spiked)
            {
                self.GetDamage(DamageClass.Generic) += 0.02f;
            }
            if (item.prefix == PrefixID.Angry)
            {
                self.GetDamage(DamageClass.Generic) += 0.03f;
            }
            if (item.prefix == PrefixID.Menacing)
            {
                self.GetDamage(DamageClass.Generic) += 0.04f;
            }
            if (item.prefix == PrefixID.Brisk)
            {
                self.moveSpeed += 0.01f;
            }
            if (item.prefix == PrefixID.Fleeting)
            {
                self.moveSpeed += 0.02f;
            }
            if (item.prefix == PrefixID.Hasty2)
            {
                self.moveSpeed += 0.03f;
            }
            if (item.prefix == PrefixID.Quick2)
            {
                self.moveSpeed += 0.04f;
            }
            if (item.prefix == PrefixID.Wild)
            {
                self.GetAttackSpeed(DamageClass.Melee) += 0.01f;
            }
            if (item.prefix == PrefixID.Rash)
            {
                self.GetAttackSpeed(DamageClass.Melee) += 0.02f;
            }
            if (item.prefix == PrefixID.Intrepid)
            {
                self.GetAttackSpeed(DamageClass.Melee) += 0.03f;
            }
            if (item.prefix == PrefixID.Violent)
            {
                self.GetAttackSpeed(DamageClass.Melee) += 0.04f;
            }
            PrefixLoader.ApplyAccessoryEffects(self, item);
        }
}