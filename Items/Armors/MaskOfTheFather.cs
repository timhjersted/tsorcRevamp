using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors;

[AutoloadEquip(EquipType.Head)]
public class MaskOfTheFather : ModItem
{
    public override void SetStaticDefaults()
    {
        ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
    }

    public override void SetDefaults()
    {
        Item.vanity = true;
        Item.width = 34;
        Item.height = 48;
        Item.rare = ItemRarityID.Blue;
        Item.value = PriceByRarity.fromItem(Item);
    }
}
