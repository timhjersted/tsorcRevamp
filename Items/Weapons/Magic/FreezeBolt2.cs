using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class FreezeBolt2 : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Freeze Bolt II");
            Tooltip.SetDefault("Casts a fast-moving bolt of ice");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 17;
            item.useTime = 17;
            item.maxStack = 1;
            item.damage = 73;
            item.knockBack = 5;
            item.autoReuse = true;
            item.scale = 1;
            item.UseSound = SoundID.Item21;
            item.rare = ItemRarityID.Orange;
            item.shootSpeed = 9;
            item.mana = 12;
            item.value = 50000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.FreezeBolt>();
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("FreezeBolt"), 1);
            recipe.AddIngredient(ItemID.FallenStar, 30);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 60000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
