using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class ChaosTome : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Casts a purple flame that can pass through solid objects.");
        }
        public override void SetDefaults() {
            item.width = 28;
            item.height = 30;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 25;
            item.useTime = 25;
            item.damage = 60;
            item.knockBack = 4f;
            item.mana = 8;
            item.rare = ItemRarityID.Pink;
            item.UseSound = SoundID.Item8;
            item.magic = true;
            item.noMelee = true;
            item.useTurn = true;
            item.autoReuse = true;
            item.value = PriceByRarity.Pink_5;
            item.shootSpeed = 8;
            item.shoot = ModContent.ProjectileType<Projectiles.ChaosBall2>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.MeteoriteBar, 15);
            recipe.AddIngredient(ItemID.SoulofNight, 15);
            recipe.AddIngredient(ItemID.SoulofSight, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 40000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
