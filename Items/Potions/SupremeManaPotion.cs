using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class SupremeManaPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Restores 400 mana" +
                "\nApplies 3 seconds of mana sickness");
        }
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.DrinkLiquid;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Green;
            Item.consumable = true;
            Item.value = 10000;
            Item.UseSound = SoundID.Item3;
        }
        public override bool? UseItem(Player player)    
        {
            int index = player.FindBuffIndex(BuffID.ManaSickness);
            if (!player.HasBuff(BuffID.ManaSickness))
            {
                player.AddBuff(BuffID.ManaSickness, 14);
            }
            else
            {
                player.buffTime[index] += 14;
            }
            player.statMana += 400;
            return base.UseItem(player);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(2);
            recipe.AddIngredient(ItemID.SuperManaPotion, 2);
            recipe.AddIngredient(ItemID.ChlorophyteOre, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 650);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();
        }
    }
}
