using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert;
public class HerosCrest : ModItem
{
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
            Player.GetDamage(DamageClass.Generic) *= 1.06f;
            Player.GetCritChance(DamageClass.Generic) += 6;
            Player.GetCritChance(DamageClass.Generic) *= 1.06f;
            Player.statLifeMax2 = (int)(Player.statLifeMax2 * 1.06f);
            Player.statDefense *= 1.06f;
            Player.luck += 0.6f;
        }
        public override void ResetEffects()
        {
            HerosCrest = false;
        }
    }
}