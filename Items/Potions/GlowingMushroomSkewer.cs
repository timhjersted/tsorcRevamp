using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class GlowingMushroomSkewer : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Heals 65 HP and applies 30 seconds of Potion Sickness\n"
                + "Potion sickness is only 15 seconds with the Philosopher's Stone effect\n"
                + "Gives Well Fed buff for 5 minutes\n"
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
            Item.height = 44;
            Item.width = 44;
            Item.maxStack = 100;
            Item.scale = .6f;
            Item.value = 150;
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
                player.statLife += 65;
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
                player.HealEffect(65, true);
                player.AddBuff(BuffID.PotionSickness, player.pStone ? 900 : 1800);
            }
            player.AddBuff(BuffID.WellFed, 18000); //5 min
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.RichMahogany, 1);
            recipe.AddIngredient(ItemID.GlowingMushroom, 1);
            recipe.AddTile(TileID.Campfire);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
