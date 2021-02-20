using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    public class BoostPotion : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Increases critical strike chance by 5%");

        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 30;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useAnimation = 15;
            item.useTime = 15;
            item.useTurn = true;
            item.UseSound = SoundID.Item3;
            item.maxStack = 30;
            item.consumable = true;
            item.rare = ItemRarityID.Blue;
            item.value = 5000;
            item.buffType = ModContent.BuffType<Buffs.Boost>();
            item.buffTime = 14400;
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BattlePotion, 5);
            recipe.AddIngredient(ItemID.Deathweed, 5);
            recipe.AddIngredient(ItemID.Gel, 5);
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(this, 5);
            recipe.AddRecipe();
        }
    }
}
