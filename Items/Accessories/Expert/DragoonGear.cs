using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Expert
{
    public class DragoonGear : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Gear worn by Dragoons.\n" +
                                "50% increased melee damage if falling.\n" +
                                "No damage from falling.\n" +
                                "Faster Jump, which also results in a higher jump.\n" +
                                "Press the Dragoon Boots key to toggle high jump (default Z)"); */

        }

        public override void SetDefaults()
        {

            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.maxStack = 1;
            Item.expert = true;
            Item.value = PriceByRarity.Purple_11;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<DragoonBoots>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DragoonHorn>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.noFallDmg = true;
            player.GetModPlayer<tsorcRevampPlayer>().DragoonHorn = true;
            player.GetModPlayer<tsorcRevampPlayer>().DragoonBoots = true;
        }

    }
}
