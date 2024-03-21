using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Potions
{
    class GreaterRestorationPotion : ModItem
    {
        public static int Healing = 125;
        public static int Sickness = 40;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Sickness);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.maxStack = Item.CommonMaxStack;
            Item.potion = true;
            Item.healLife = Healing;
            Item.rare = ItemRarityID.Green;
            Item.consumable = true;
            Item.value = 10000;
            Item.UseSound = SoundID.Item3;
        }
        public override void GetHealLife(Player player, bool quickHeal, ref int healValue)
        {
            healValue = Healing;
        }
        public override bool? UseItem(Player player)
        {
            player.ClearBuff(BuffID.PotionSickness);
            player.AddBuff(BuffID.PotionSickness, Sickness * 60);
            return base.UseItem(player);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(2);
            recipe.AddIngredient(ItemID.GreaterHealingPotion, 2);
            recipe.AddIngredient(ItemID.ChlorophyteOre, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 750);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();
        }
    }
}
