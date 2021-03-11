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
            item.width = 22;
            item.height = 18;
            item.defense = 7;
            item.value = 1500000;
            item.rare = ItemRarityID.Pink;
        }

        public override void UpdateEquip(Player player)
        {
            player.doubleJumpCloud = true;
            player.moveSpeed += 0.17f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.ShadowGreaves, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

