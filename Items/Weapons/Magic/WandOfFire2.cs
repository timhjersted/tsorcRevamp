using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class WandOfFire2 : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Magic/WandOfFire";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Wand of Fire II");
            Item.staff[item.type] = true;
        }
        public override void SetDefaults() {
            item.autoReuse = true;
            item.width = 12;
            item.height = 17;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 19;
            item.useTime = 19;
            item.maxStack = 1;
            item.damage = 23;
            item.knockBack = 1;
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.mana = 8;
            if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.mana = 4;
            item.UseSound = SoundID.Item20;
            item.shootSpeed = 14;
            item.noMelee = true;
            item.value = 24000;
            item.magic = true;
            item.rare = ItemRarityID.Orange;
            item.shoot = ModContent.ProjectileType<Projectiles.FireBall>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Stinger, 3);
            recipe.AddIngredient(mod.GetItem("WandOfFire"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2300);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
