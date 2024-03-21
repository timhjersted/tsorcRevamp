using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    public class ArmorDrugPotion : ModItem
    {
        public static int Defense = 18;
        public static float DRIncrease = 12f;
        public static int MaxLife = 50;
        public static float BadDmg = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Defense, DRIncrease, MaxLife, BadDmg);
        public override void SetStaticDefaults()
        {
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
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = 300000;
            Item.buffType = ModContent.BuffType<Buffs.ArmorDrug>();
            Item.buffTime = 28800;
        }

        public override bool? UseItem(Player player)
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

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(4);
            recipe.AddIngredient(ItemID.EndurancePotion, 4);
            recipe.AddIngredient(ItemID.IronskinPotion, 4);
            recipe.AddIngredient(ItemID.Sapphire, 4);
            recipe.AddIngredient(ItemID.SoulofNight, 4);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();
        }
    }
}
