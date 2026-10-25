using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Damage.ShadowmoonCloak;
using tsorcRevamp.Content.Items.Accessories.Defensive.DarkCloak;
using tsorcRevamp.Content.Items.Accessories.Defensive.LightCloak;
using tsorcRevamp.Content.Items.Accessories.Magic;
using tsorcRevamp.Content.Items.Accessories.Magic.DuskCrownRing;
using tsorcRevamp.Content.Items.Accessories.Other.DragoonCloak;
using tsorcRevamp.Content.Items.Armor.SupremeDragoonSet;

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

        #region Variables

        var lightCloakPlayer = Player.GetModPlayer<LightCloakPlayer>();
        int lightCloakThreshold = (int)(maxHealth * (LightCloakItem.LifeThreshold / 100f));
        
        var darkCloakPlayer = Player.GetModPlayer<DarkCloakPlayer>();
        int darkCloakThreshold = (int)(maxHealth * (DarkCloakItem.LifeThreshold / 100f));
        
        var shadowmoonCloakPlayer = Player.GetModPlayer<ShadowmoonCloakPlayer>();
        int shadowmoonCloakThreshold = (int)(maxHealth * (ShadowmoonCloakItem.LifeThreshold / 100f));
        
        var dragoonCloakPlayer = Player.GetModPlayer<DragoonCloakPlayer>();
        int dragoonCloakThreshold = (int)(maxHealth * (DragoonCloakItem.LifeThreshold / 100f));
        
        var supremeDragoonCloakPlayer = Player.GetModPlayer<SupremeDragoonCloakPlayer>();

        #endregion

        #region Dragoon Cloaks

        if (supremeDragoonCloakPlayer.Equipped)
        {
            supremeDragoonCloakPlayer.ApplySupremeDragoonCloak(currentHealth, dragoonCloakThreshold, SupremeDragoonArmor.DragoonCloakEfficiency / 100f);
        }
        if (dragoonCloakPlayer.Equipped)
        {
            dragoonCloakPlayer.ApplyDragoonCloak(currentHealth, dragoonCloakThreshold);
        }
        if (lightCloakPlayer.Equipped)
        {
            lightCloakPlayer.ApplyLightCloak(currentHealth, lightCloakThreshold);
        }
        if (darkCloakPlayer.Equipped)
        {
            darkCloakPlayer.ApplyDarkCloak(currentHealth, darkCloakThreshold);
        }
        if (shadowmoonCloakPlayer.Equipped)
        {
            shadowmoonCloakPlayer.ApplyShadowMoonCloak(currentHealth, shadowmoonCloakThreshold);
        }

        #endregion
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