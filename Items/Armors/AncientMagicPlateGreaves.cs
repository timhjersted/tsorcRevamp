using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class AncientMagicPlateGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The long forgotten greaves.\n+20% movement.");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 4;
            item.value = 8000000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.20f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CobaltLeggings, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

