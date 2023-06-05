using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Potions
{
    class SupremeManaPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Restores 400 mana" +
                "\nApplies 3 seconds of mana sickness" +
                "\nOnly usable through quick mana"); */
        }
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Green;
            Item.consumable = true;
            Item.value = 10000;
            Item.UseSound = SoundID.Item3;
        }
        public override bool? UseItem(Player player)    
        {
            player.statMana += 400;
            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(2);
            recipe.AddIngredient(ItemID.SuperManaPotion, 2);
            recipe.AddIngredient(ItemID.ChlorophyteOre, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 650);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();
        }
    }
}
