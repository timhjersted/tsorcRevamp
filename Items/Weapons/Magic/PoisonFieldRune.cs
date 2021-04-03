using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class PoisonFieldRune : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Creates a poisonous cloud at the point of impact." +
                                "\nHigh duration and can damage many targets.");
        }

        public override void SetDefaults() {
            item.damage = 5;
            item.height = 28;
            item.knockBack = 4;
            item.rare = ItemRarityID.Orange;
            item.shootSpeed = 6;
            item.magic = true;
            item.mana = 20;
            item.noMelee = true;
            item.useAnimation = 45;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 45;
            item.value = 100000;
            item.width = 20;
            item.shoot = ModContent.ProjectileType<Projectiles.PoisonFieldBall>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.Stinger, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
