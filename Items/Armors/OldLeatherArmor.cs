using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class OldLeatherArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Set bonus: +5% Ranged Damage, +3 Ranged Crit\nArmor can be upgraded for 500 Dark Souls a piece");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 4;
            Item.value = 18000;
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Leather, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 65);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
