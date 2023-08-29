using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors;

[AutoloadEquip(EquipType.Head)]
public class MaskOfTheMother : ModItem
{
    public override void SetStaticDefaults()
    {
        ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
    }
    public override void SetDefaults()
    {
        Item.vanity = true;
        Item.width = 26;
        Item.height = 30;
        Item.rare = ItemRarityID.Blue;
        Item.value = PriceByRarity.fromItem(Item);
    }
}
