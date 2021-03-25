using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class WereBuster : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A spiked mace made to kill werewolves." +
                                "\nDoes 16x damage to werewolves.");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Green;
            item.damage = 37;
            item.height = 42;
            item.knockBack = 10;
            item.melee = true;
            item.autoReuse = true;
            item.useTurn = true;
            item.useTime = 21;
            item.useAnimation = 21;
            item.value = 100000;
            item.width = 42;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.UseSound = SoundID.Item1;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MoonCharm, 1);
            recipe.AddIngredient(ItemID.CobaltBar, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            if (target.type == NPCID.Werewolf) damage *= 16;
        }
    }
}
