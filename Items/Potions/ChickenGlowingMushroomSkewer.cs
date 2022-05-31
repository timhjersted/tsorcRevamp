using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class ChickenGlowingMushroomSkewer : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Heals 150 HP and applies 30 seconds of Potion Sickness\n"
                + "Potion sickness is only 20 seconds with the Philosopher's Stone effect\n"
                + "Gives Well Fed buff for 15 minutes"
                + "While the [c/6d8827:Bearer of the Curse] wont be healed by this,\n"
                + "they still gain some healing items' other effects such as buffs");
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
            Item.maxStack = 100;
            Item.scale = .6f;
            Item.value = 750;
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
                player.statLife += 150;
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
                player.HealEffect(150, true);
                player.AddBuff(BuffID.PotionSickness, player.pStone ? 1200 : 1800);
            }
            player.AddBuff(BuffID.WellFed, 54000); //15 min
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.RichMahogany, 1);
            recipe.AddIngredient(ItemID.GlowingMushroom, 1);
            recipe.AddIngredient(Mod.GetItem("DeadChicken"), 1);
            recipe.AddIngredient(ItemID.PixieDust, 1);
            recipe.AddTile(TileID.Campfire);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
