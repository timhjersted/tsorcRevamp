using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions {
    class CookedChicken : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Heals 100 HP and applies 30 seconds of Potion Sickness.\n" + "Potion sickness is only 20 seconds with the Philosopher's Stone effect.");
        }

        public override void SetDefaults() {
            item.consumable = true;
            item.useAnimation = 17;
            item.UseSound= SoundID.Item2;
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.useTime = 17;
            item.height = 16;
            item.maxStack = 100;
            item.scale = 1;
            item.value = 2;
            item.width = 20;
        }


        public override bool CanUseItem(Player player) {
            if (player.HasBuff(BuffID.PotionSickness)) {
                return false;
            }
            return true;
        }

        public override bool UseItem(Player player) {
            player.statLife += 100;
            if (player.statLife > player.statLifeMax2) {
                player.statLife = player.statLifeMax2;
            }
            player.HealEffect(100, true);
            player.AddBuff(BuffID.PotionSickness, player.pStone ? 1200 : 1800);
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DeadChicken"), 1);
            recipe.AddTile(TileID.CookingPots);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
