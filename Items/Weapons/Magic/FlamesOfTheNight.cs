using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class FlamesOfTheNight : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A full sprectral array of green flame will light up the skies");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 20;
            item.useTime = 2;
            item.damage = 40;
            item.knockBack = 1;
            item.autoReuse = true;
            item.UseSound = SoundID.Item20;
            item.rare = ItemRarityID.Pink;
            item.shootSpeed = 21;
            item.mana = 12;
            item.value = PriceByRarity.Pink_5;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.CursedFlames>();
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.CursedFlame, 1);
            recipe.AddIngredient(ItemID.SoulofSight, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 60000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
