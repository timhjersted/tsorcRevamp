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