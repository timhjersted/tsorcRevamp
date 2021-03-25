using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class BowOfEarendil : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Bow of Earendil");
            Tooltip.SetDefault("Always aim for the heart" +
                               "\nLegendary");

        }

        public override void SetDefaults() {

            item.damage = 90;
            item.height = 58;
            item.width = 20;
            item.ranged = true;
            item.knockBack = 15f;
            item.maxStack = 1;
            item.noMelee = true;
            item.rare = ItemRarityID.Pink;
            item.scale = 0.9f;
            item.shoot = AmmoID.Arrow;
            item.shootSpeed = 16;
            item.useAmmo = AmmoID.Arrow;
            item.useAnimation = 16;
            item.useTime = 16;
            item.autoReuse = true;
            item.UseSound = SoundID.Item5;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.value = 500000;

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofSight, 1);
            recipe.AddIngredient(ItemID.MoltenFury, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
