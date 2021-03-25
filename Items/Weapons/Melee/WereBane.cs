using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class WereBane : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("WereBane");
            Tooltip.SetDefault("A sword used to kill werewolves instantly." +
                                "\nDoes 8x damage to werewolves.");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Green;
            item.damage = 32;
            item.height = 42;
            item.knockBack = 9;
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
            recipe.AddIngredient(ItemID.GoldBroadsword, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 4);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            if (target.type == NPCID.Werewolf) damage *= 8;
        }
    }
}
