using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class DragoonArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("The legends of Arradius spoke of the one who would wear this\nYou are a master of all forces, the protector of Earth, the Hero of the age.");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 30;
            item.value = 5000;
            item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("RedHerosShirt"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 60000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
