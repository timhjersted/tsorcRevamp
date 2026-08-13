using System.Collections.Generic;
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
    }

    public float MaxManaAmplifier = 500f;
    public int ManaCostMult = 2;
    
    public const float CeruleanFlaskMaxManaScaling = 25f;
    public float MagicDamageAmp = 15f;
    public float MagicAttackSpeedAmp = 15f;

    public override void PostUpdateEquips()
    {
        if (Enabled)
        {
            if (!Player.HasBuff(BuffID.ManaSickness))
            {
                Player.GetDamage(DamageClass.Magic) *= 1f + (MagicDamageAmp / 100f);
                Player.GetAttackSpeed(DamageClass.Magic) *= 1f + (MagicAttackSpeedAmp / 100f);
            }

            MaxManaAmplifier = 400f;

            var tsorcPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
            tsorcPlayer.MaxManaAmplifier *= 1f + MaxManaAmplifier / 100f; //if anything buffs your max mana, this will multiply that properly
            tsorcPlayer.MaxManaAmplifier += MaxManaAmplifier; //then add the multiplier
        }
    }

    public override void PostUpdateMiscEffects()
    {
        Player.manaCost *= ManaCostMult;
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
        if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && (item.type == ItemID.ManaFlower || item.type == ItemID.ArcaneFlower || item.type == ItemID.MagnetFlower || item.type == ItemID.ManaCloak || item.type == ModContent.ItemType<CelestialCloak>()))
        {
            TooltipHelper.SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.CommonItemTooltip.BotCManaFlower", (int)CeruleanFlaskPlayer.CeruleanManaFlowerStrength));
        }
        if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ItemID.ManaRegenerationPotion)
        {
            TooltipHelper.SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.ManaRegenerationPotionBotC").FormatWith(CeruleanFlaskPlayer.ManaRegenPotRestorationTimerBonus));
        }

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

    private static void CustomManaStarAmount(On_PlayerStatsSnapshot.orig_ctor orig, ref PlayerStatsSnapshot self, Player player)
    {
        orig(ref self, player);
        var arcaneSorceryPlayer = player.GetModPlayer<ArcaneSorceryPlayer>();
        if (arcaneSorceryPlayer.Enabled)
        {
            self.AmountOfManaStars = player.statManaMax2 / (int)(20f * (1f + player.GetModPlayer<tsorcRevampPlayer>().MaxManaAmplifier / 100f));
        }
    }
}