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
            Item.width = 18;
            Item.height = 18;
            Item.defense = 5;
            Item.value = 1000000;
            Item.rare = ItemRarityID.Green;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.25f;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilGreaves, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

