using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    public class StrengthPotion : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Increases damage by 15%, critical strike chance " +
                             "\nby 2%, defense by 15, and swing speed by 15%." +
                             "\nDoes not stack with Demon Drug, Armor Drug, or Battlefront Potions.");
        }

        public override void SetDefaults() {
            item.width = 20;
            item.height = 26;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useAnimation = 15;
            item.useTime = 15;
            item.useTurn = true;
            item.UseSound = SoundID.Item3;
            item.maxStack = 30;
            item.consumable = true;
            item.rare = ItemRarityID.Blue;
            item.value = 1000;
            item.buffType = ModContent.BuffType<Buffs.Strength>();
            item.buffTime = 36000;
        }
        public override bool UseItem(Player player)
        {
            int currentBuff = 0;
            foreach (int buffType in player.buffType)
            {
                if (buffType == ModContent.BuffType<Buffs.Battlefront>() || buffType == ModContent.BuffType<Buffs.DemonDrug>() || buffType == ModContent.BuffType<Buffs.ArmorDrug>())
                {
                    player.DelBuff(currentBuff);
                }
                currentBuff++;
            }
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BottledWater, 1);
            recipe.AddIngredient(ItemID.Deathweed, 1);
            recipe.AddIngredient(ItemID.Diamond, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
