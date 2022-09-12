using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class RedHerosHat : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Red Hero's hat");
            Tooltip.SetDefault("Skill: Longer invincibility after being hit, +80 max mana" +
                "\nCan be upgraded to it's master form with 80,000 Dark Souls");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 10;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.longInvince = true;
            player.statManaMax2 += 80;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BlueHerosHat>());
            recipe.AddIngredient(ItemID.SoulofSight, 2);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
