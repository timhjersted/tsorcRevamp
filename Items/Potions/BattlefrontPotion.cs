using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    public class BattlefrontPotion : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Increases damage by 30%, critical strike chance " +
                             "\nby 6%, defense by 12, and swing speed by 20%." +
                              "\nAlso gives Thorns and Battle potion effects." +
                             "\nDoes not stack with Demon Drug, Armor Drug, or Strength Potions.");
        }

        public override void SetDefaults() {
            Item.width = 24;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = 5000;
            Item.buffType = ModContent.BuffType<Buffs.Battlefront>();
            Item.buffTime = 28800;
        }

        public override bool? UseItem(Player player)
        {
            int currentBuff = 0;
            foreach (int buffType in player.buffType)
            {
                if (buffType == ModContent.BuffType<Buffs.Strength>() || buffType == ModContent.BuffType<Buffs.DemonDrug>() || buffType == ModContent.BuffType<Buffs.ArmorDrug>())
                {
                    player.DelBuff(currentBuff);
                }
                currentBuff++;
            }
            return true;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BattlePotion, 1);
            recipe.AddIngredient(ItemID.ThornsPotion, 1);
            recipe.AddIngredient(ItemID.IronskinPotion, 1);
            recipe.AddIngredient(ItemID.ArcheryPotion, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("BoostPotion").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<StrengthPotion>(), 1);
            recipe.AddIngredient(ModContent.ItemType<ArmorDrugPotion>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DemonDrugPotion>(), 1);
            recipe.AddTile(TileID.Bottles);
            
            recipe.Register();
        }
    }
}
