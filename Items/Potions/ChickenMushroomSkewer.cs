using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class ChickenMushroomSkewer : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Heals 125 HP and applies 30 seconds of Potion Sickness\n"
                + "Potion sickness is only 25 seconds with the Philosopher's Stone effect\n"
                + "Gives Well Fed buff for 10 minutes"); */
        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.useAnimation = 17;
            Item.UseSound = SoundID.Item2;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useTime = 17;
            Item.height = 54;
            Item.width = 54;
            Item.maxStack = 9999;
            Item.scale = .6f;
            Item.value = 500;
        }


        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(BuffID.PotionSickness))
            {
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                player.statLife += 125;
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
                player.HealEffect(125, true);
                player.AddBuff(BuffID.PotionSickness, player.pStone ? 1200 : 1800);
            }
            player.AddBuff(BuffID.WellFed, 36000); //10 min
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Wood, 1);
            recipe.AddIngredient(ItemID.Mushroom, 1);
            recipe.AddIngredient(ModContent.ItemType<DeadChicken>(), 1);
            recipe.AddIngredient(ItemID.PixieDust, 1);
            recipe.AddTile(TileID.Campfire);

            recipe.Register();
        }
    }
}
