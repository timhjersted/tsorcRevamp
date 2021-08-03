using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class Bolt1Tome : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Bolt 1 Tome");
            Tooltip.SetDefault("A lost beginner's tome" +
                                "\nDrops a small lightning strike upon collision" +
                                "\nCan be upgraded");

        }

        public override void SetDefaults() {
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.damage = 11;
            if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.damage = 9;
            item.height = 10;
            item.knockBack = 0f;
            item.autoReuse = true;
            item.maxStack = 1;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 6f;
            item.magic = true;
            item.noMelee = true;
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.mana = 9;
            if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.mana = 5;
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.useAnimation = 27;
            if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.useAnimation = 15;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.useTime = 27;
            if (ModContent.GetInstance<tsorcRevampConfig>().LegacyMode) item.useTime = 15;
            item.value = 140;
            item.width = 34;
            item.shoot = ModContent.ProjectileType<Projectiles.Bolt1Ball>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
