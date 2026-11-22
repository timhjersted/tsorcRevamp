using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.RedKnightSet
{
    [AutoloadEquip(EquipType.Head)]
    public class RedKnightHelmet : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/RedKnightSet/RedKnightHelmet";

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class RedKnightBreastplate : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/RedKnightSet/RedKnightBreastplate";

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class RedKnightGreaves : ModItem
    {
        public override string Texture => "tsorcRevamp/Content/Items/Armor/Enemy/RedKnightSet/RedKnightGreaves";

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.vanity = true;
        }
    }
}
