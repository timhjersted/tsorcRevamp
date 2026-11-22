using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.VanillaItems.Summoner;

public class VanillaSummonerPlayer : ModPlayer
{
    public bool HerculesBeetle;
    public bool NecromanticScroll;
    public bool PapyrusScarab;
    public override void ResetEffects()
    {
        HerculesBeetle = false;
        NecromanticScroll = false;
        PapyrusScarab = false;
    }

    public override void Load()
    {
        On_Player.ApplyEquipFunctional += VanillaSummonerEquipmentEdits;
        On_Player.GrantArmorBenefits += VanillaSummonerArmorEdits;
    }

    private void VanillaSummonerArmorEdits(On_Player.orig_GrantArmorBenefits orig, Player self, Item armorPiece)
    {
        if (armorPiece.type == ItemID.SquireGreatHelm)
        {
            self.lifeRegen -= VanillaSummonerItems.SquireHelmLifeRegenPerSec * 2;
            
            self.maxMinions += VanillaSummonerItems.SquireHelmMinionSlots;
            //vanilla already grants sentry slot
        }
        if (armorPiece.type == ItemID.SquirePlating)
        {
            self.GetDamage(DamageClass.Melee) -= VanillaSummonerItems.SquirePlatingMeleeDmg / 100f;
            
            self.maxMinions += VanillaSummonerItems.SquirePlatingMinionSlots;
            //vanilla already grants summon dmg
        }
        if (armorPiece.type == ItemID.SquireGreaves)
        {
            self.GetCritChance(DamageClass.Melee) -= VanillaSummonerItems.SquireGreavesMeleeCrit;
            
            self.GetCritChance(DamageClass.Summon) += VanillaSummonerItems.SquireGreavesCritChance;
            //vanilla already grants summon dmg and move speed
        }
        if (armorPiece.type == ItemID.SquireAltHead)
        {
            self.GetDamage(DamageClass.Melee) -= VanillaSummonerItems.ValhallaKnightHelmMeleeDmg / 100f;
            
            self.maxMinions += VanillaSummonerItems.ValhallaKnightHelmMinionSlots;
            //vanilla already grants sentry slots and summon dmg
        }
        if (armorPiece.type == ItemID.SquireAltShirt)
        {
            self.lifeRegen -= VanillaSummonerItems.ValhallaKnightBreastplateLifeRegenPerSec * 2;
            
            self.maxMinions += VanillaSummonerItems.ValhallaKnightBreastplateMinionSlots;
            //vanilla already grants summon dmg
        }
        if (armorPiece.type == ItemID.SquireAltPants)
        {
            self.GetCritChance(DamageClass.Melee) -= VanillaSummonerItems.ValhallaKnightGreavesMeleeCritChance;
            
            self.GetCritChance(DamageClass.Summon) += VanillaSummonerItems.ValhallaKnightGreavesCritChance;
            //vanilla already grants summon dmg and move speed
        }
        orig(self, armorPiece);
    }

    private void VanillaSummonerEquipmentEdits(On_Player.orig_ApplyEquipFunctional orig, Player self, Item currentItem, bool hideVisual)
    {
        if (currentItem.type == ItemID.HerculesBeetle)
        {
            self.GetDamage(DamageClass.Summon) -= 0.15f;
            self.GetCritChance(DamageClass.Summon) += VanillaSummonerItems.BeetleSummonCritChance;
            HerculesBeetle = true;
        }
        if (currentItem.type == ItemID.NecromanticScroll)
        {
            self.GetDamage(DamageClass.Summon) -= 0.1f;
            self.maxMinions -= 1;
            self.GetCritChance(DamageClass.Summon) += VanillaSummonerItems.ScrollSummonCritChance;
            NecromanticScroll = true;
        }
        if (currentItem.type == ItemID.PapyrusScarab)
        {
            self.GetDamage(DamageClass.Summon) -= 0.15f;
            self.GetCritChance(DamageClass.Summon) += VanillaSummonerItems.ScarabSummonCritChance;
            PapyrusScarab = true;
        }
        orig(self, currentItem, hideVisual);
    }

    public override void PostUpdateEquips()
    {
        var modPlayer = Player.GetModPlayer<tsorcRevampPlayer>();
        if (PapyrusScarab)
        {
            modPlayer.SummonTagStrength += VanillaSummonerItems.ScarabTagBoost / 100f * (HerculesBeetle ? 0 : 1f);
            modPlayer.SummonTagDuration += VanillaSummonerItems.ScarabTagBoost / 100f * (NecromanticScroll ? 0 : 1f);
        }
        if (HerculesBeetle)
        {
            modPlayer.SummonTagStrength += VanillaSummonerItems.BeetleSummonTagStrengthBoost / 100f;
        }
        if (NecromanticScroll)
        {
            modPlayer.SummonTagDuration += VanillaSummonerItems.ScrollSummonTagDurationBoost / 100f;
        }
    }
}