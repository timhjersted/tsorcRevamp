using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Legs)]
    public class SmoughGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 10;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().BurdenOfSmough = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CopperGreaves);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

