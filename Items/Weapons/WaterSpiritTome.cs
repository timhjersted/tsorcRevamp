using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class WaterSpiritTome : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons frost spirits that can pass through walls." +
                                "\nDamage grows as the spirits grow larger");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 30;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useAnimation = 6;
            item.useTime = 6;
            item.damage = 70;
            item.knockBack = 6;
            item.autoReuse = true;
            item.noMelee = true;
            item.UseSound = SoundID.Item9;
            item.rare = ItemRarityID.Pink;
            item.shootSpeed = 9;
            item.mana = 4;
            item.value = 14000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.IceSpirit4>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.CrystalShard, 100);
            recipe.AddIngredient(ItemID.SoulofNight, 40);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 60000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
