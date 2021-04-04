using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class ChickenMushroomSkewer : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Heals 125 HP and applies 30 seconds of Potion Sickness.\n"
                + "Potion sickness is only 25 seconds with the Philosopher's Stone effect.\n"
                + "Gives Well Fed buff for 10 minutes.");
        }

        public override void SetDefaults()
        {
            item.consumable = true;
            item.useAnimation = 17;
            item.UseSound = SoundID.Item2;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useTime = 17;
            item.height = 54;
            item.width = 54;
            item.maxStack = 100;
            item.scale = .6f;
            item.value = 500;
        }


        public override bool CanUseItem(Player player)
        {
            if (player.HasBuff(BuffID.PotionSickness))
            {
                return false;
            }
            return true;
        }

        public override bool UseItem(Player player)
        {
            player.statLife += 125;
            if (player.statLife > player.statLifeMax2)
            {
                player.statLife = player.statLifeMax2;
            }
            player.HealEffect(125, true);
            player.AddBuff(BuffID.PotionSickness, player.pStone ? 1200 : 1800);
            player.AddBuff(BuffID.WellFed, 36000); //10 min
            return true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Wood, 1);
            recipe.AddIngredient(ItemID.Mushroom, 1);
            recipe.AddIngredient(mod.GetItem("DeadChicken"), 1);
            recipe.AddTile(TileID.Campfire);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
