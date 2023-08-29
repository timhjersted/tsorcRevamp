using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors;

[AutoloadEquip(EquipType.Legs)]
public class SnarbolaxGreaves : ModItem
{
    public override void SetStaticDefaults()
    {
        Tooltip.SetDefault("Part of the TSORC Revamp Team dev set belonging to ChromaEquinox");
    }

    public override void SetDefaults()
    {
        Item.vanity = true;
        Item.width = 30;
        Item.height = 18;
        Item.rare = ItemRarityID.Expert;
        Item.value = PriceByRarity.fromItem(Item);
    }
}