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
            Item.width = 30;
            Item.height = 36;
            Item.defense = 7;
            Item.value = 27000;
            Item.rare = ItemRarityID.Blue;
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
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Silk, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 200);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}
