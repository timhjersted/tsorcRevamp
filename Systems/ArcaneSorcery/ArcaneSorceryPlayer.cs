using System.Linq;
using Terraria;
using Terraria.GameContent.UI.ResourceSets;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;

namespace tsorcRevamp.Systems.ArcaneSorcery;

public class ArcaneSorceryPlayer : ModPlayer
{
    public bool ArcaneSorcerer = false;

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
    
    public float ManaBurnMagicDamageAmp = 20f;
    public float ManaBurnMagicAttackSpeedAmp = 20f;

    public override void ResetEffects()
    {
        ArcaneSorcerer = false;
        ManaBurn = false;
        CeruleanFlatManaGainMult = 1f;
        CeruleanFlaskMaxManaScalingMult = 1f;
    }

    public override void PreUpdateBuffs()
    {
        if (ArcaneSorcery.Enabled)
        {
            if (Player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (Player.HeldItem.DamageType == DamageClass.Magic)
                {
                    Player.AddBuff(ModContent.BuffType<ArcaneSorcery>(), 3); //buff is only added when player holds magic weapon and used as a condition for exactly that
                }
                ArcaneSorcerer = true; //this is the actual check for most conditions, since they often involve max mana it'd be awkward if your max mana fluctuated based on held item
            }
        }
    }

    public override void PostUpdateBuffs()
    {
        if (ArcaneSorcerer)
        {
            CeruleanFlaskMaxManaScalingMult = BaseCeruleanFlaskMaxManaScalingMult;
            CeruleanFlatManaGainMult = BaseCeruleanFlatManaGainMult;
        }
    }

    public override void PostUpdateEquips()
    {
        if (ArcaneSorcerer)
        {            
            if (Main.npc.Any(n => n?.active == true && n.boss && n != Main.npc[200]) || !Player.HasBuff(ModContent.BuffType<Bonfire>()))
            {
                Player.manaRegenDelay = 100; //effectively disables mana regen outside of bonfires
            }
            
            if (ManaBurn)
            {
                Player.GetDamage(DamageClass.Magic) *= 1f + (ManaBurnMagicDamageAmp / 100f);
                Player.GetAttackSpeed(DamageClass.Magic) *= 1f + (ManaBurnMagicAttackSpeedAmp / 100f);
            }

            var tsorcPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
            tsorcPlayer.MaxManaAmplifier *= 1f + MaxManaAmplifier / 100f; //if anything buffs your max mana, this will multiply that properly
            tsorcPlayer.MaxManaAmplifier += MaxManaAmplifier; //then add the multiplier
        }
    }

    public override void PostUpdateMiscEffects()
    {
        if (ArcaneSorcerer)
        {
            Player.manaCost *= ManaCostMult;
        }
        if (ManaBurn)
        {
            Player.manaCost *= ManaBurnCostMult;
        }
    }
}