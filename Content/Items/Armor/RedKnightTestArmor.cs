using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Head)]
    public class RedKnightTestHelmet : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class RedKnightTestBreastplate : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class RedKnightTestGreaves : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.vanity = true;
        }
    }
}
