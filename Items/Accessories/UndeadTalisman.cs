using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class UndeadTalisman : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Reduces damage from undead by 15");

        }

        public override void SetDefaults()
        {

            Item.width = 22;
            Item.height = 32;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Bone, 12);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 2000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().UndeadTalisman = true;
        }

    }
}
