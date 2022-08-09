/*
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Magic
{
    public class CelestriadMage : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Automatically use mana potions when needed" +
                "\nIncreases pickup range for mana stars" +
                "\nMana Sickness now increases damage taken" +
                "\ninstead of decreasing magic damage dealt" +
                "\n10% reduced mana usage");
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.rare = ItemRarityID.Purple;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<GoldenHairpin>(), 1);
            recipe.AddIngredient(ItemID.MagnetFlower, 1);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 20);
            recipe.AddIngredient(ModContent.ItemType<SoulOfBlight>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.manaCost -= 0.1f;
            player.manaFlower = true;
            player.manaMagnet = true;
            player.GetModPlayer<tsorcRevampPlayer>().Celestriad = true;
        }
    }
}
*/