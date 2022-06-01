using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class Masamune : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The blade almost cannot be seen it cuts so fast, ripping enemies to shreds in seconds." +
                                "\nHas the power to shoot a magical water flame from its blade yet it uses no mana.");
        }

        public override void SetDefaults() {
            Item.width = 48;
            Item.height = 72;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.damage = 140;
            Item.knockBack = 9;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Red;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            Item.shootSpeed = 13f;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            //recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(Mod.GetItem("Murassame"), 1);
            recipe.AddIngredient(Mod.GetItem("GuardianSoul"), 1);
            recipe.AddIngredient(Mod.GetItem("BlueTitanite"), 10);
            recipe.AddIngredient(Mod.GetItem("GhostWyvernSoul"), 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 250000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
