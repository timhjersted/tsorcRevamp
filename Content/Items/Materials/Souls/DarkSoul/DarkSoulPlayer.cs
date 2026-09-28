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
    public const float DefaultSoulMult = 0.04f;
    public int SoulPickupRange = 5;
    public int ConsSoulChanceMult;
    public override void ResetEffects()
    {
        SoulPickupRange = 5;
        ConsSoulChanceMult = 0;
    }    
    public float SoulsMultiplier()
    {
        float multiplier = 1f;
        if (Player.GetModPlayer<SilverSerpentRingPlayer>().SilverSerpentRing)
        {
            multiplier += SilverSerpentRingItem.SoulAmplifier / 100f;
        }
        if (Player.GetModPlayer<SoulSerpentRingPlayer>().SoulSerpentRing)
        {
            multiplier += SoulSerpentRingItem.SoulAmplifier / 100f;
        }
        if (Player.GetModPlayer<tsorcRevampPlayer>().SoulSiphon)
        {
            multiplier += SoulSiphonPotion.SoulAmplifier / 100f * Player.GetModPlayer<tsorcRevampPlayer>().SoulSiphonScaling;
        }
        if (Player.GetModPlayer<tsorcRevampPlayer>().SOADrain)
        {
            multiplier += SymbolOfAvarice.SoulAmplifier / 100f;
        }
        if (Player.GetModPlayer<tsorcRevampPlayer>().VOEGDrain)
        {
            multiplier += VaultOfEndlessGreed.SoulAmplifier / 100f;
        }
        if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
        {
            multiplier += Darksign.BotCSoulDropAmplifier / 100f;
        }
        switch (Main.GameMode)
        {
            case GameModeID.Normal:
            {
                multiplier *= DefaultSoulMult * 0.75f;
                break;
            }
            case GameModeID.Expert:
            {
                multiplier *= DefaultSoulMult;
                break;
            }
            case GameModeID.Master:
            {
                multiplier *= DefaultSoulMult * (1f + (MastersScroll.SoulAmplifier / 100f));
                break;
            }
        }
        return multiplier;
    }
}