using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class ConditionOverload : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Enemies take exponentially increasing" +
                                "\nbonus damage for every debuff affecting them." +
                                "\n\"H Deimos CO farm 3/4 LF despoil\"");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().ConditionOverload = true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HellstoneBar, 3);
            recipe.AddIngredient(ItemID.CursedFlame, 3);
            recipe.AddIngredient(ItemID.Stinger, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
