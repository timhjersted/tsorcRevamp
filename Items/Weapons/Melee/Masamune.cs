using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class Masamune : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The blade almost cannot be seen it cuts so fast, ripping enemies to shreds in seconds." +
                                "\nHas the power to shoot a magical water flame from its blade yet it uses no mana.");
        }

        public override void SetDefaults() {
            item.width = 48;
            item.height = 72;
            item.useAnimation = 15;
            item.useTime = 15;
            item.damage = 140;
            item.knockBack = 9;
            item.autoReuse = true;
            item.useTurn = true;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Red;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = PriceByRarity.Red_10;
            item.melee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            item.shootSpeed = 13f;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(mod.GetItem("Murassame"), 1);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"), 3);
            recipe.AddIngredient(mod.GetItem("BlueTitanite"), 10);
            recipe.AddIngredient(mod.GetItem("GhostWyvernSoul"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 250000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
