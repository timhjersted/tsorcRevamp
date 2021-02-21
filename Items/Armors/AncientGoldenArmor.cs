using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class AncientGoldenArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A lost prince's armor.\n+7% melee speed");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 8;
            item.value = 1800000;
            item.rare = ItemRarityID.Green;
        }

        public override void UpdateEquip(Player player)
        {
            player.meleeSpeed += 0.07f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.GoldChainmail, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
