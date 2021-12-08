using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    public class ArmorDrugPotion : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Armor Drug");
            Tooltip.SetDefault("Increases defense by 13 for 3 minutes." +
                "\nDoes not stack with Demon Drug, Strength, or Battlefront Potions.");

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
            item.buffType = ModContent.BuffType<Buffs.ArmorDrug>();
            item.buffTime = 10800;
        }

        public override bool UseItem(Player player)
        {
            int currentBuff = 0;
            foreach (int buffType in player.buffType)
            {
                if (buffType == ModContent.BuffType<Buffs.Strength>() || buffType == ModContent.BuffType<Buffs.DemonDrug>() || buffType == ModContent.BuffType<Buffs.Battlefront>())
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
            recipe.AddIngredient(ItemID.Sapphire, 5);
            recipe.AddIngredient(ItemID.SoulofNight, 4);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
