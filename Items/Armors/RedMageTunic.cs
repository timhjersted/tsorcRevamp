using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class RedMageTunic : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("-15% mana cost" +
                                "\nSet bonus gives +20 mana and +8% magic damage");
        }
        public override void SetDefaults()
        {
            item.width = 30;
            item.height = 36;
            item.defense = 7;
            item.value = 27000;
            item.rare = ItemRarityID.Blue;
        }

        public override void DrawHands(ref bool drawHands, ref bool drawArms)
        {
            drawHands = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.manaCost -= 0.15f;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Silk, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
