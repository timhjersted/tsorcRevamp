using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class MagmaGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("5% increased melee damage");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 9;
            Item.value = 40000;
            Item.rare = ItemRarityID.White;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.05f;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MoltenGreaves, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

