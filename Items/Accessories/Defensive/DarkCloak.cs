using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class DarkCloak : ModItem
    {
        public static int Defense1 = 7;
        public static float LifeThreshold = 40f;
        public static int Defense2 = 13;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeThreshold, Defense2);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
            Item.defense = Defense1;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            if (player.statLife <= (int)(player.statLifeMax2 * (LifeThreshold / 100f)))
            {
                player.statDefense += Defense2;
            }
        }

    }
}
