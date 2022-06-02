using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class ShadowCloakGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("17% increased movement speed\n+ Double Jump Skill");
        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.defense = 7;
            Item.value = 15000;
            Item.rare = ItemRarityID.Pink;
        }

        public override void UpdateEquip(Player player)
        {
            player.hasJumpOption_Cloud = true;
            player.moveSpeed += 0.17f;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShadowGreaves, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 1000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}

