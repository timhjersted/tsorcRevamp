using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Damage.ShadowmoonCloak;
using tsorcRevamp.Content.Items.Accessories.Defensive.DarkCloak;
using tsorcRevamp.Content.Items.Accessories.Defensive.LightCloak;
using tsorcRevamp.Content.Items.Accessories.Defensive.MythrilGlove;
using tsorcRevamp.Content.Items.Accessories.Magic;
using tsorcRevamp.Content.Items.Accessories.Magic.DuskCrownRing;
using tsorcRevamp.Content.Items.Accessories.Other.DragoonCloak;
using tsorcRevamp.Content.Items.Armor.Magic.DragonScaleSet;
using tsorcRevamp.Content.Items.Armor.Magic.MimeticSet;
using tsorcRevamp.Content.Items.Armor.Magic.RTQ2Set;
using tsorcRevamp.Content.Items.Armor.Melee.CrystalSet;
using tsorcRevamp.Content.Items.Armor.Ranged.ShellSet;
using tsorcRevamp.Content.Items.Armor.ShadowCloakSet;
using tsorcRevamp.Content.Items.Armor.Summon.AncientDemon;
using tsorcRevamp.Content.Items.Armor.SupremeDragoonSet;
using tsorcRevamp.Content.Items.VanillaItems.FrozenTurtleShell;
using tsorcRevamp.Content.Items.VanillaItems.PaladinsShield;

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
        
        var frozenTurtleShellPlayer = Player.GetModPlayer<FrozenTurtleShellPlayer>();

        var paladinShieldPlayer = Player.GetModPlayer<PaladinsShieldPlayer>();

        var mythrilGlovePlayer = Player.GetModPlayer<MythrilGlovePlayer>();

        var shellSetPlayer = Player.GetModPlayer<ShellSetPlayer>();
        
        var mimeticSetPlayer = Player.GetModPlayer<MimeticSetPlayer>();
        
        var shadowCloakSetPlayer = Player.GetModPlayer<ShadowCloakSetPlayer>();

        var rtq2SetPlayer = Player.GetModPlayer<RTQ2SetPlayer>();
        
        var demonSetPlayer = Player.GetModPlayer<AncientDemonSetPlayer>();
        
        var dragonScaleSetPlayer = Player.GetModPlayer<DragonScaleSetPlayer>();
        
        var crystalEnchantedSetPlayer = Player.GetModPlayer<CrystalEnchantedSetPlayer>();
        
        var crystalSightedSetPlayer = Player.GetModPlayer<CrystalSightedSetPlayer>();
        
        #endregion

        #region Dragoon Cloaks

        var lightCloakPlayer = Player.GetModPlayer<LightCloakPlayer>();
        int lightCloakThreshold = (int)(maxHealth * (LightCloakItem.LifeThreshold / 100f));
        
        var darkCloakPlayer = Player.GetModPlayer<DarkCloakPlayer>();
        int darkCloakThreshold = (int)(maxHealth * (DarkCloakItem.LifeThreshold / 100f));
        
        var shadowmoonCloakPlayer = Player.GetModPlayer<ShadowmoonCloakPlayer>();
        int shadowmoonCloakThreshold = (int)(maxHealth * (ShadowmoonCloakItem.LifeThreshold / 100f));
        
        var dragoonCloakPlayer = Player.GetModPlayer<DragoonCloakPlayer>();
        int dragoonCloakThreshold = (int)(maxHealth * (DragoonCloakItem.LifeThreshold / 100f));
        
        var supremeDragoonCloakPlayer = Player.GetModPlayer<SupremeDragoonCloakPlayer>();
        
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

        if (frozenTurtleShellPlayer.Equipped && currentHealth <= maxHealth / 2) //under 50%
        {
            frozenTurtleShellPlayer.ApplyIceBarrier();
        }

        if (paladinShieldPlayer.Equipped && currentHealth >= maxHealth / 4) //over 25%
        {
            paladinShieldPlayer.ApplyPaladinsShield();
        }

        if (mythrilGlovePlayer.Equipped)
        {
            mythrilGlovePlayer.ApplyMythrilGlove(currentHealth, (int)(maxHealth * MythrilGloveItem.Threshold1 / 100f), (int)(maxHealth * MythrilGloveItem.Threshold2 / 100f));
        }

        if (shellSetPlayer.Equipped)
        {
            shellSetPlayer.ApplyShellSetBonus(currentHealth, (int)(maxHealth * ShellHelmet.LifeThreshold / 100f));
        }
        if (mimeticSetPlayer.Equipped)
        {
            mimeticSetPlayer.ApplyMimeticSetBonus(currentHealth, (int)(maxHealth * MimeticSuit.LifeThreshold / 100f));
        }
        if (shadowCloakSetPlayer.Equipped)
        {
            shadowCloakSetPlayer.ApplyShadowCloakSetBonus(currentHealth, (int)(maxHealth * ShadowCloakPlateMail.LifeThreshold / 100f));
        }
        if (rtq2SetPlayer.Equipped)
        {
            rtq2SetPlayer.ApplyRTQ2SetBonus(currentHealth, (int)(maxHealth * RTQ2Chestplate.LifeThreshold / 100f));
        }
        if (demonSetPlayer.Equipped)
        {
            demonSetPlayer.ApplyDemonSetBonus(currentHealth, (int)(maxHealth * AncientDemonArmor.LifeThreshold / 100f));
        }
        if (dragonScaleSetPlayer.Equipped)
        {
            dragonScaleSetPlayer.ApplyDragonScaleSetBonus(currentHealth, (int)(maxHealth * DragonScaleMail.LifeThreshold / 100f));
        }
        if (crystalEnchantedSetPlayer.Equipped)
        {
            crystalEnchantedSetPlayer.ApplyCrystalEnchantedSetBonus(currentHealth, (int)(maxHealth * CrystalEnchantedHelmet.LifeThreshold / 100f));
        }
        if (crystalSightedSetPlayer.Equipped)
        {
            crystalSightedSetPlayer.ApplyCrystalSightedSetBonus(currentHealth, (int)(maxHealth * CrystalSightedHelmet.LifeThreshold / 100f));
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