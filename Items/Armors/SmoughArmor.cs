using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class SmoughArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Smough's Armor");
            Tooltip.SetDefault("");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.vanity = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return head.type == ModContent.ItemType<SmoughHelmet>() && legs.type == ModContent.ItemType<SmoughGreaves>();
        }

        public override void UpdateArmorSet(Player player)
        {
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 250);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
