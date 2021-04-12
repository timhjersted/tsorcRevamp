using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class GlowingMushroomSkewer : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Heals 60 HP and applies 30 seconds of Potion Sickness.\n"
                + "Potion sickness is only 20 seconds with the Philosopher's Stone effect.\n"
                + "Gives Well Fed buff for 5 minutes.");
        }

        public override void SetDefaults()
        {
            item.consumable = true;
            item.useAnimation = 17;
            item.UseSound = SoundID.Item2;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useTime = 17;
            item.height = 44;
            item.width = 44;
            item.maxStack = 100;
            item.scale = .6f;
            item.value = 150;
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
            player.statLife += 60;
            if (player.statLife > player.statLifeMax2)
            {
                player.statLife = player.statLifeMax2;
            }
            player.HealEffect(60, true);
            player.AddBuff(BuffID.PotionSickness, player.pStone ? 1200 : 1800);
            player.AddBuff(BuffID.WellFed, 18000); //5 min
            return true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.RichMahogany, 1);
            recipe.AddIngredient(ItemID.GlowingMushroom, 1);
            recipe.AddTile(TileID.Campfire);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
