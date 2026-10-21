using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Defensive.LightCloak;
using tsorcRevamp.Content.Items.Accessories.Magic;
using tsorcRevamp.Content.Items.Accessories.Magic.DuskCrownRing;

namespace tsorcRevamp.Systems;

public class MaxHealthCalculator : ModPlayer
{
    public float LifeforceLevel;
    public float MaxHealthMultiplier;
    public override void ResetEffects()
    {
        LifeforceLevel = 0;
        MaxHealthMultiplier = 1;
    }

    public float CalculateMaxHealth(out float finalHealth)
    {
        float maxHealth = Player.statLifeMax2;
        var duskCrownRingPlayer = Player.GetModPlayer<DuskCrownRingPlayer>();
        
        if (Player.lifeForce)
        {
            MaxHealthMultiplier += LifeforceLevel / 100f;
        }

        if (duskCrownRingPlayer.Equipped)
        {
            duskCrownRingPlayer.MaxHealthPenalty();
        }
        
        return finalHealth = maxHealth * MaxHealthMultiplier;
    }

    public void ApplyMaxHealthEffects(in int maxHealth, in int currentHealth)
    {
        //effects based on health thresholds must be called here
        var lightCloakPlayer = Player.GetModPlayer<LightCloakPlayer>();
        if (lightCloakPlayer.Equipped)
        {
            lightCloakPlayer.CheckHealthThreshold(maxHealth, currentHealth);
        }
    }

    public override void PostUpdateBuffs()
    {
        if (Player.HasBuff(BuffID.Lifeforce))
        {
            Player.statLifeMax2 -= Player.statLifeMax / 5 / 20 * 20; // neutralizing vanilla effect
            LifeforceLevel = 20f;
        }
    }

    public override void PostUpdateMiscEffects()
    {
        CalculateMaxHealth(out float maxHealth);
        Player.statLifeMax2 = (int)maxHealth;
        ApplyMaxHealthEffects((int)maxHealth, in Player.statLife);
    }
}