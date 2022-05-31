using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class ForgottenThunderBow : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Casts a bolt of lightning from your bow, doing massive damage over time. ");
        }
        public override void SetDefaults() {
            Item.damage = 140;
            Item.height = 58;
            Item.knockBack = 4;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.magic = true;
            Item.rare = ItemRarityID.Red;
            Item.mana = 100;
            Item.shootSpeed = 33;
            Item.useAnimation = 40;
            Item.UseSound = SoundID.Item5;
            Item.useTime = 40;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Red_10;
            Item.width = 16;
            Item.shoot = ModContent.ProjectileType<Projectiles.Bolt4Ball>();
        }
        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("ForgottenThunderBowScroll"), 1);
            recipe.AddIngredient(Mod.GetItem("Bolt4Tome"), 1);
            recipe.AddIngredient(Mod.GetItem("SoulOfArtorias"), 1);
            recipe.AddIngredient(Mod.GetItem("Humanity"), 30);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 200000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
