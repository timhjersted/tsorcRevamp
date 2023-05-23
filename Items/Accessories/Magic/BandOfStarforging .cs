using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Defensive;

namespace tsorcRevamp.Items.Accessories.Magic
{
    //[AutoloadEquip(EquipType.HandsOn)]
    public class BandOfStarforging : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BandOfPhenomenalCosmicPower>());
            recipe.AddIngredient(ModContent.ItemType<EssenceOfMana>());
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 2);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 66000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.lifeRegen += 4;
            player.statManaMax2 += 100;
            player.manaRegenBonus += 50;
            player.manaRegenDelayBonus += 1.5f;
            player.statManaMax2 = (int)(player.statManaMax2 * 1.5f);
            player.manaCost -= 0.1f;
        }

    }
}
