using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenDiamondMace : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A mace made of a large diamond. Has a 1 in 15 chance to heal 20 life.");
        }

        public override void SetDefaults() {
            item.autoReuse = true;
            item.useTurn = true;
            item.rare = ItemRarityID.Blue;
            item.damage = 90;
            item.height = 36;
            item.knockBack = 10;
            item.scale = 1.3f;
            item.melee = true;
            item.useAnimation = 15;
            item.useTime = 15;
            item.UseSound = SoundID.Item1;
            item.value = 1090000;
            item.width = 36;
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            if (Main.rand.Next(15) == 0) {
                player.statLife += 20;
                if (player.statLife > player.statLifeMax2) {
                    player.statLife = player.statLifeMax2;
                }
            }
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.AdamantiteBar, 10);
            recipe.AddIngredient(ItemID.Diamond, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 66000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
