using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class AncientDemonGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+25% movement, can walk on heated grounds.\n+5% magic damage");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 9;
            item.value = 3000000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.25f;
            player.fireWalk = true;
            player.magicDamage += 0.05f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MoltenGreaves, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

