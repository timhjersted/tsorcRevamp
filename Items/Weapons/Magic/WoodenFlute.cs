using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class WoodenFlute : ModItem {
        public override void SetDefaults() {
            item.damage = 10;
            item.height = 10;
            item.knockBack = 4;
            item.maxStack = 1;
            item.rare = ItemRarityID.White;
            item.scale = 1;
            item.shootSpeed = 10;
            item.magic = true;
            item.mana = 2;
            item.useAnimation = 45;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 45;
            item.value = PriceByRarity.White_0;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.MusicalNote>();
        }
        
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Wood, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 220);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
