/*
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Summon
{
    public class ChallengersGlove : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Challenger's Glove");
            Tooltip.SetDefault("Increases whip knockback" +
                "\n12% increased whip damage and speed" +
                "\nEnables autoswing for melee weapons and whips" +
                "\nIncreases the size of whips by 10%" +
                "\nEnemies are more likely to target you");
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 40;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
            Item.defense = 8;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AvengerEmblem);
            recipe.AddIngredient(ItemID.BerserkerGlove);
            recipe.AddTile(TileID.TinkerersWorkbench);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetKnockback(DamageClass.SummonMeleeSpeed) += 2f;
            player.GetAttackSpeed(DamageClass.Summon) += 0.12f;
            player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.12f;
            player.autoReuseGlove = true;
            player.whipRangeMultiplier += 0.1f;
            player.aggro += 400;
        }
    }
}*/