using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class CookedChicken : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Heals 100 HP and applies 30 seconds of Potion Sickness\n" + "Potion sickness is only 20 seconds with the Philosopher's Stone effect");
        }

        public override void SetDefaults() {
            Item.consumable = true;
            Item.useAnimation = 17;
            Item.UseSound= SoundID.Item2;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useTime = 17;
            Item.height = 16;
            Item.maxStack = 100;
            Item.scale = 1;
            Item.value = 2;
            Item.width = 20;
            Item.healLife = 100;
        }


        public override bool CanUseItem(Player player) {
            if (player.HasBuff(BuffID.PotionSickness)) {
                return false;
            }
            return true;
        }

        public override bool? UseItem(Player player) {

            if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
            {
                if (player.statLife > player.statLifeMax2)
                {
                    player.statLife = player.statLifeMax2;
                }
                player.AddBuff(BuffID.PotionSickness, player.pStone ? 1200 : 1800);

                return true;
            }
            return false;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("DeadChicken").Type, 1);
            recipe.AddTile(TileID.CookingPots);
            
            recipe.Register();
        }
    }
}
