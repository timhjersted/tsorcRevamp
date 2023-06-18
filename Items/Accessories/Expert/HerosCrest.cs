using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert;
public class HerosCrest : ModItem
{
    public static float StatMult = 6f;
    public static float Luck = 60f;
    public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(StatMult, Luck);
    public override void SetStaticDefaults()
    {
    }

    public override void SetDefaults()
    {
        Item.width = 24;
        Item.height = 24;
        Item.accessory = true;
        Item.maxStack = 1;
        Item.expert = true;
        Item.value = PriceByRarity.Yellow_8;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.GetModPlayer<HerosCrestPlayer>().HerosCrest = true;
    }
    private class HerosCrestPlayer : ModPlayer
    {
        public bool HerosCrest;

        public override void PostUpdateEquips()
        {
            if (!HerosCrest) return;
            Player.GetDamage(DamageClass.Generic) *= 1f + StatMult / 100f;
            Player.GetCritChance(DamageClass.Generic) += StatMult;
            Player.GetCritChance(DamageClass.Generic) *= 1f + StatMult / 100f;
            Player.statLifeMax2 = (int)(Player.statLifeMax2 * (1f + StatMult / 100f));
            Player.statDefense *= 1f + StatMult / 100f;
            Player.luck += Luck / 100f;
        }
        public override void ResetEffects()
        {
            HerosCrest = false;
        }
    }
}