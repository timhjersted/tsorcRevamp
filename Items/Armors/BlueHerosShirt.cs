using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class BlueHerosShirt : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blue Hero's Shirt");
            Tooltip.SetDefault("Set bonus grants extended breath & swimming skills plus 6% all stats boost\nAlso, +3 life regen speed, faster movement & hunter vision while in water");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 16;
            item.value = 2500;
            item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.HerosShirt, 1);
            recipe.AddIngredient(ItemID.Flipper, 1);
            recipe.AddIngredient(ItemID.DivingHelmet, 1);
            recipe.AddIngredient(ItemID.MythrilBar, 3);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
