using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert;

public class RingOfPower : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("A great ring of power gifted to men." +
                            "\nCasts darkness and battle potion effects on wearer." +
                            "\n+25% crit chance. ");
    }

    public override void SetDefaults()
    {
        Item.width = 28;
        Item.height = 38;
        Item.accessory = true;
        Item.value = PriceByRarity.Red_10;
        Item.expert = true;
    }

    public override void UpdateEquip(Player player)
    {
        player.noKnockback = true;
        player.AddBuff(BuffID.Darkness, 500, false);
        player.AddBuff(BuffID.Battle, 500, false);
        player.GetCritChance(DamageClass.Generic) += 25;
    }

}

