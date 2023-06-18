using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class ProtectRing : ModItem
    {
        public static int Defense = 30;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Defense);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Cyan_9;
            Item.rare = ItemRarityID.Cyan;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(ItemID.Emerald, 1);
            recipe.AddIngredient(ModContent.ItemType<FlameOfTheAbyss>(), 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.AddBuff(ModContent.BuffType<Buffs.Protect>(), 60, false);
        }

    }
}

