using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class MushroomSkewer : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Heals 55 HP and applies 30 seconds of Potion Sickness\n" //heals 55hp every 30 seconds.
                + "Potion sickness is only 15 seconds with the Philosopher's Stone effect\n"
                + "Gives Well Fed buff for 2 minutes\n"
                + "While the [c/6d8827:Bearer of the Curse] wont be healed by this,\n"
                + "they still gain some healing items' other effects such as buffs");
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
            item.value = 100;
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
            if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                player.statLife += 55;
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
                player.HealEffect(55, true);
                player.AddBuff(BuffID.PotionSickness, player.pStone ? 900 : 1800);
            }
            player.AddBuff(BuffID.WellFed, 7200); //2 min
            return true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Wood, 1);
            recipe.AddIngredient(ItemID.Mushroom, 1);
            recipe.AddTile(TileID.Campfire);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
