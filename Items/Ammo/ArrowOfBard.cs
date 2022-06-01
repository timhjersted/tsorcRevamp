using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Ammo {
    public class ArrowOfBard : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Arrow of Bard");
            Tooltip.SetDefault("\"The arrow which slew Smaug.\"");

        }

        public override void SetDefaults() {

            Item.stack = 1;
            Item.consumable = true;
            Item.ammo = AmmoID.Arrow;
            Item.shoot = ModContent.ProjectileType<Projectiles.ArrowOfBard>();
            Item.damage = 500;
            Item.height = 28;
            Item.knockBack = (float)4;
            Item.maxStack = 2000;
            Item.DamageType = DamageClass.Ranged;
            Item.scale = (float)1;
            Item.shootSpeed = (float)3.5f;
            Item.useAnimation = 100;
            Item.useTime = 100;
            Item.value = 500000;
            Item.width = 10;
            Item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.MythrilBar, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 2000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 10);
            recipe.Register();
        }


    }
}
