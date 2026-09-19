using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Other.SilverSerpentRing;
using tsorcRevamp.Content.Items.Accessories.Other.SoulSerpentRing;
using tsorcRevamp.Content.Items.Armor;
using tsorcRevamp.Content.Items.Potions;

namespace tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

public class DarkSoulPlayer : ModPlayer
{
    public int SoulPickupRange = 5;
    public int ConsSoulChanceMult;
    public override void ResetEffects()
    {
        SoulPickupRange = 5;
        ConsSoulChanceMult = 0;
    }
    public static float SoulsMultiplier(Player player)
    {
        float multiplier = 1f;
        float defaultDifficultyMod = 0.04f;
        if (player.GetModPlayer<SilverSerpentRingPlayer>().SilverSerpentRing)
        {
            multiplier += SilverSerpentRingItem.SoulAmplifier / 100f;
        }
        if (player.GetModPlayer<SoulSerpentRingPlayer>().SoulSerpentRing)
        {
            multiplier += SoulSerpentRingItem.SoulAmplifier / 100f;
        }
        if (player.GetModPlayer<tsorcRevampPlayer>().SoulSiphon)
        {
            multiplier += SoulSiphonPotion.SoulAmplifier / 100f * player.GetModPlayer<tsorcRevampPlayer>().SoulSiphonScaling;
        }
        if (player.GetModPlayer<tsorcRevampPlayer>().SOADrain)
        {
            multiplier += SymbolOfAvarice.SoulAmplifier / 100f;
        }
        if (player.GetModPlayer<tsorcRevampPlayer>().VOEGDrain)
        {
            multiplier += VaultOfEndlessGreed.SoulAmplifier / 100f;
        }
        if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
        {
            multiplier += Darksign.BotCSoulDropAmplifier / 100f;
        }
        switch (Main.GameMode)
        {
            case GameModeID.Normal:
            {
                multiplier *= defaultDifficultyMod * 0.75f;
                break;
            }
            case GameModeID.Expert:
            {
                multiplier *= defaultDifficultyMod;
                break;
            }
            case GameModeID.Master:
            {
                multiplier *= defaultDifficultyMod * (1f + (MastersScroll.SoulsAmp / 100f));
                break;
            }
        }
        return multiplier;
    }
}