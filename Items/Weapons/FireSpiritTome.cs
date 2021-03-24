using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons {
    class FireSpiritTome : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons fire spirits at incredible speed.");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 30;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 5;
            item.useTime = 5;
            item.maxStack = 1;
            item.damage = 30;
            item.knockBack = 8;
            item.autoReuse = true;
            item.scale = 1.3f;
            item.UseSound = SoundID.Item9;
            item.rare = ItemRarityID.Pink;
            item.shootSpeed = 11;
            item.mana = 5;
            item.value = 600000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.FireSpirit2>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.Fireblossom, 50);
            recipe.AddIngredient(ItemID.SoulofNight, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 45000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
