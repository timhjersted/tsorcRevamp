using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    public class DemonDrugPotion : ModItem
    {
        public static float DmgMultiplier = 22f;
        public static int BadDefense = 15;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DmgMultiplier, BadDefense);
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Demon Drug");
            /* Tooltip.SetDefault("Multiplies damage by 22%, lowers defense by 15\n" +
                               "Does not stack with Armor Drug, Strength, or Battlefront Potions."); */
        }

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 24;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = 300000;
            Item.buffType = ModContent.BuffType<Buffs.DemonDrug>();
            Item.buffTime = 28800;
        }
        public override bool? UseItem(Player player)
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

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(4);
            recipe.AddIngredient(ItemID.MagicPowerPotion, 4);
            recipe.AddIngredient(ItemID.ArcheryPotion, 4);
            recipe.AddIngredient(ItemID.Ale, 4);
            recipe.AddIngredient(ItemID.SummoningPotion, 4);
            recipe.AddIngredient(ItemID.SoulofNight, 4);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();
        }
    }
}
