using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class EnchantedThrowingSpear : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Magic throwing spear that passes through walls and is created with mana on each throw");
        }
        public override void SetDefaults()
        {
            Item.shootSpeed = 12f;
            Item.damage = 39;
            Item.knockBack = 9f;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.mana = 10;
            Item.width = 64;
            Item.height = 64;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.LightRed;

            Item.consumable = false;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.magic = true;

            Item.UseSound = SoundID.Item1;
            Item.value = PriceByRarity.LightRed_4;
            Item.shoot = ModContent.ProjectileType<Projectiles.EnchantedThrowingSpear>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("ThrowingSpear"), 1);
            recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(Mod.GetItem("EphemeralDust"), 30);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 5000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}
