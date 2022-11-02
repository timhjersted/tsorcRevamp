using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    public class StrengthPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases damage by 10%," +
                             "\ndefense by 15, and all attack speed by 15%" +
                             "\nDoes not stack with Demon Drug, Armor Drug, or Battlefront Potions.");
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = 1000;
            Item.buffType = ModContent.BuffType<Buffs.Strength>();
            Item.buffTime = 28800;
        }
        public override bool? UseItem(Player player)
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

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(4);
            recipe.AddIngredient(ItemID.Teacup, 4);
            recipe.AddIngredient(ItemID.Deathweed, 4);
            recipe.AddIngredient(ItemID.Diamond, 4);
            recipe.AddIngredient(ItemID.SoulofNight, 4);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();
        }
    }
}
