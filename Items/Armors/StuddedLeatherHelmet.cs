using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors;

[AutoloadEquip(EquipType.Head)]
public class StuddedLeatherHelmet : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Increases crit chance by 5%");
    }
    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 26;
        Item.defense = 1;
        Item.rare = ItemRarityID.Orange;
        Item.value = PriceByRarity.fromItem(Item);
    }
    public override void UpdateEquip(Player player)
    {
        player.GetCritChance(DamageClass.Generic) += 5;
    }
}
