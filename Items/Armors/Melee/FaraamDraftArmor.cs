using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Head)]
    public class FaraamDraftHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Green;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class FaraamDraftBreastplate : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Green;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class FaraamDraftGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = true;
            ArmorIDs.Legs.Sets.HidesTopSkin[Item.legSlot] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Green;
            Item.vanity = true;
        }
    }
}
