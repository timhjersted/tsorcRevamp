using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class ArmorOfArtorias : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Armor of Artorias");
            Tooltip.SetDefault("Enchanted armor of Artorias of the Abyss.\nPossesses the same power as the Titan Glove.\nSet bonus gives +37% damage, +50% move speed, 8 life regen, lava immunity and many other abilities.");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 30;
            Item.value = 40000;
            Item.rare = ItemRarityID.Purple;
        }

        public override void UpdateEquip(Player player)
        {
            player.kbGlove = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfArtorias").Type, 2);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
