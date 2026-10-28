using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Armor.BlackKnightSet
{
    // Worn by the Black Knight puppet. Vanity only and not obtainable yet (no drop or recipe planned).
    [AutoloadEquip(EquipType.Head)]
    public class BlackKnightHelmet : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Body)]
    public class BlackKnightBreastplate : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 28;
            Item.vanity = true;
        }
    }

    [AutoloadEquip(EquipType.Legs)]
    public class BlackKnightGreaves : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.vanity = true;
        }
    }
}
