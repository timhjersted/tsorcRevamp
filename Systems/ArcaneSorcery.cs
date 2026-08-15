using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using Humanizer;
using Terraria;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Magic;
using tsorcRevamp.Items.Armors.Magic;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems;

public class ArcaneSorceryPlayer : ModPlayer
{
    public bool Enabled = false;

    public override void ResetEffects()
    {
        Enabled = false;
        ManaBurn = false;
        CeruleanFlatManaGainMult = 1f;
        CeruleanFlaskMaxManaScalingMult = 1f;
    }

    public float MaxManaAmplifier = 400f;
    public float ManaCostMult = 2f;
    public bool ManaBurn = false;
    public float ManaBurnStaminaThreshold = 50f;
    public float ManaBurnCostMult = 2f;
    public float ManaBurnBadResistance = 40f;

    public const float BaseCeruleanFlaskMaxManaScalingMult = 1.6f;
    public float CeruleanFlaskMaxManaScalingMult = 1f;
    public const float BaseCeruleanFlatManaGainMult = 3f;
    public float CeruleanFlatManaGainMult = 1f;
    public float MagicDamageAmp = 20f;
    public float MagicAttackSpeedAmp = 20f;

    public override void PostUpdateBuffs()
    {
        if (Enabled)
        {
            CeruleanFlaskMaxManaScalingMult = BaseCeruleanFlaskMaxManaScalingMult;
            CeruleanFlatManaGainMult = BaseCeruleanFlatManaGainMult;
        }
    }

    public override void PostUpdateEquips()
    {
        if (Enabled)
        {
            if (!Player.HasBuff(BuffID.ManaSickness) && ManaBurn)
            {
                Player.GetDamage(DamageClass.Magic) *= 1f + (MagicDamageAmp / 100f);
                Player.GetAttackSpeed(DamageClass.Magic) *= 1f + (MagicAttackSpeedAmp / 100f);
            }

            var tsorcPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
            tsorcPlayer.MaxManaAmplifier *= 1f + MaxManaAmplifier / 100f; //if anything buffs your max mana, this will multiply that properly
            tsorcPlayer.MaxManaAmplifier += MaxManaAmplifier; //then add the multiplier
        }
    }

    public override void PostUpdateMiscEffects()
    {
        if (Enabled)
        {
            Player.manaCost *= ManaCostMult;
        }
        if (ManaBurn)
        {
            Player.manaCost *= ManaBurnCostMult;
        }
    }
}

public class ArcaneSorceryItems : GlobalItem
{
    public const float BotCManaRestorationCuffsPercentage = 80;
    public const float BotCManaStarMaxManaPercentage = 5;
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
    {
        Player player = Main.LocalPlayer;
        var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

        if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && (item.type == ItemID.CelestialMagnet || item.type == ItemID.ManaCloak || item.type == ItemID.CelestialEmblem))
        {
            TooltipHelper.SimpleGlobalModTooltip(Mod, tooltips, LangUtils.GetTextValue("Items.VanillaItems.CelestialMagnetBotC", BotCManaStarMaxManaPercentage));
        }

        if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ItemID.MagicCuffs)
        {
            TooltipHelper.SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MagicCuffsBotC", BotCManaRestorationCuffsPercentage));
        }

        if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && (item.type == ItemID.CelestialCuffs | item.type == ModContent.ItemType<CelestialCloak>()))
        {
            TooltipHelper.SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.CelestialMagnetBotC", BotCManaStarMaxManaPercentage) + "\n" + Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MagicCuffsBotC", BotCManaRestorationCuffsPercentage));
        }

        bool isWearingMagicHatGypsyRobe = player.armor[0].type == ItemID.MagicHat &&
                                          player.armor[1].type == ItemID.GypsyRobe && item.type == player.armor[2].type;
        if (tsorcRevamp.ManaIncreasingItems.ContainsKey(item.type) | isWearingMagicHatGypsyRobe)
        {
            List<int> setBonusKeys = new List<int>()
            {
                ItemID.MagicHat, ItemID.GypsyRobe, ModContent.ItemType<RedClothTunic>(), ModContent.ItemType<RedClothHat>(), ModContent.ItemType<RedClothPants>()
            };
            int manaIncrease = !isWearingMagicHatGypsyRobe ? tsorcRevamp.ManaIncreasingItems[item.type] : tsorcRevamp.ManaIncreasingItems[ItemID.MagicHat];;
            
            if (item.type == ItemID.GypsyRobe &&
                player.armor[0].type == ModContent.ItemType<RedClothHat>() &&
                player.armor[2].type == ModContent.ItemType<RedClothPants>())
            {
                manaIncrease = tsorcRevamp.ManaIncreasingItems[ModContent.ItemType<RedClothTunic>()];
            }
            bool isSetBonus = setBonusKeys.Contains(item.type) | isWearingMagicHatGypsyRobe;
            string setBonusAdd = isSetBonus ? LangUtils.GetTextValue("CommonItemTooltip.SetBonus") : "";
            int ttindex = tooltips.FindIndex(t => t.Text.Contains(manaIncrease.ToString()));
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "MaxManaIncreaseScaled", setBonusAdd + Language.GetTextValue("CommonItemTooltip.IncreasesMaxManaBy", (int)((float)manaIncrease * (1f + modPlayer.MaxManaAmplifier / 100f)))));
            }

            if (tsorcRevamp.ManaIncreasingItems.ContainsKey(item.type) && isWearingMagicHatGypsyRobe)
            {
                int manaIncrease2 = tsorcRevamp.ManaIncreasingItems[item.type];
                int ttindex1 = tooltips.FindIndex(t => t.Text.Contains(manaIncrease2.ToString()));
                if (ttindex1 != -1)
                {
                    tooltips.RemoveAt(ttindex1);
                    tooltips.Insert(ttindex1, new TooltipLine(Mod, "MaxManaIncreaseScaled", Language.GetTextValue("CommonItemTooltip.IncreasesMaxManaBy", (int)((float)manaIncrease2 * (1f + modPlayer.MaxManaAmplifier / 100f)))));
                }
            }
        }
    }
}
class ManaStarDrawAmount
{
    internal static void ApplyManaStarAmount()
    {
        On_PlayerStatsSnapshot.ctor += CustomManaStarAmount;
    }
    //the cheese below is necessary, trust
    private static void CustomManaStarAmount(On_PlayerStatsSnapshot.orig_ctor orig, ref PlayerStatsSnapshot self, Player player)
    {
        var arcaneSorceryPlayer = player.GetModPlayer<ArcaneSorceryPlayer>();
        var modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
        int trueMaxMana = player.statManaMax2;
        int trueMana = player.statMana;
        
        if (arcaneSorceryPlayer.Enabled)
        { //turn mana stats to "vanilla-like values" before orig so the proper amount of mana stars/bars are drawn
            player.statMana = (int)(trueMana / (1f + modPlayer.MaxManaAmplifier / 100f));
            player.statManaMax2 = (int)(trueMaxMana / (1f + modPlayer.MaxManaAmplifier / 100f));
        }
        
        orig(ref self, player);
        
        if (arcaneSorceryPlayer.Enabled)
        { //turn mana stats to back real stats after drawing is done
            player.statManaMax2 = trueMaxMana;
            player.statMana = trueMana;
        }
    }
}