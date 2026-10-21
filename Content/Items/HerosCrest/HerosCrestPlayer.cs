using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace tsorcRevamp.Content.Items.HerosCrest;


public class HerosCrestPlayer : ModPlayer
{
    public bool HerosCrest;

    public override void SaveData(TagCompound tag)
    {
        tag.Add("HerosCrest", HerosCrest);
    }
    public override void LoadData(TagCompound tag)
    {
        HerosCrest = tag.GetBool("HerosCrest");
    }
    public override void ModifyMaxStats(out StatModifier health, out StatModifier mana)
    {
        base.ModifyMaxStats(out health, out mana);
        if (HerosCrest)
        {
            health.Base += HerosCrestItem.Stats * HerosCrestItem.LuckMaxLifeMult;
        }
    }
    public override void PostUpdateEquips()
    {
        if (HerosCrest)
        {
            Player.GetDamage(DamageClass.Generic) += HerosCrestItem.Stats / 100f;
            Player.GetCritChance(DamageClass.Generic) += HerosCrestItem.Stats;
        }
    }

    public override void ModifyLuck(ref float luck)
    {
        if (HerosCrest)
        {
            luck += HerosCrestItem.Stats * 10f / 100f;
        }
    }
}