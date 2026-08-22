using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;

namespace tsorcRevamp.Items.Accessories.Defensive.Rings
{
    public class BloodbaneRing : ModItem
    {
        public const float LifeRegen = 1f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeRegen / 2f);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 5;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SilverRing>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BloodredMossClump>(), 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.buffImmune[BuffID.Poisoned] = true;
            player.buffImmune[BuffID.Bleeding] = true;
            player.lifeRegen += (int)LifeRegen;
        }

    }
}

