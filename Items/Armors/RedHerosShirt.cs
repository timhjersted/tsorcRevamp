using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class RedHerosShirt : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Red Hero's Shirt");
            Tooltip.SetDefault("The legendary clothes of the master.\nSet bonus: Lava, fire block & knockback immunity\nPlus extended breath, water walk, & swim\n+14% to all stats and +4 life regen while in lava & +2 in water");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 20;
            item.value = 2500;
            item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("BlueHerosShirt"), 1);
            recipe.AddIngredient(ItemID.SoulofSight, 3);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
