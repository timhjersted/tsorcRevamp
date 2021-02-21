using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class AncientDragonScaleGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Known to be treasured by assassins.\n+25% movement.");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 5;
            item.value = 1000000;
            item.rare = ItemRarityID.Green;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.25f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MythrilGreaves, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

