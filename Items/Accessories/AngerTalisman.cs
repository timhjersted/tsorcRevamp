using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class AngerTalisman : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("-30 defense" +
                                "\n30% increased damage");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.RangerEmblem, 1);
            recipe.AddIngredient(ItemID.SorcererEmblem, 1);
            recipe.AddIngredient(ItemID.WarriorEmblem, 1);
            recipe.AddIngredient(ItemID.ChlorophyteBar, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 60000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.statDefense -= 30; //because 10 defense is a joke
            player.GetDamage(DamageClass.Generic) += 0.3f;

        }

    }
}
