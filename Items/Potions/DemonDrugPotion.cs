using Terraria.ID;
using Terraria.ModLoader;
using System.Collections.Generic;
using Terraria;

namespace tsorcRevamp.Items.Potions {
    public class DemonDrugPotion : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Demon Drug");
            Tooltip.SetDefault("Increases damage by 20% for 3 minutes\n" +
                                "However, also lowers defence by 20\n" +
                                "Does not stack with Armor Drug, Strength, or Battlefront Potions.");
        }

        public override void SetDefaults() {
            item.width = 14;
            item.height = 24;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useAnimation = 15;
            item.useTime = 15;
            item.useTurn = true;
            item.UseSound = SoundID.Item3;
            item.maxStack = 30;
            item.consumable = true;
            item.rare = ItemRarityID.Blue;
            item.value = 300000;
            item.buffType = ModContent.BuffType<Buffs.DemonDrug>();
            item.buffTime = 10800;
        }
        public override bool UseItem(Player player)
        {
            int currentBuff = 0;
            foreach (int buffType in player.buffType)
            {
                if (buffType == ModContent.BuffType<Buffs.Strength>() || buffType == ModContent.BuffType<Buffs.Battlefront>() || buffType == ModContent.BuffType<Buffs.ArmorDrug>())
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
            recipe.AddIngredient(ItemID.SoulofNight, 4);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
