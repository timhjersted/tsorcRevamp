
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class Longinus : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Legendary spear fabled to hold sway over the world.\nIncreases attack damage by 50% when falling.");
        }


        public override void SetDefaults() {
            Item.damage = 200;
            Item.knockBack = 9f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 11;
            Item.useTime = 1;
            Item.shootSpeed = 8;
            //item.shoot = ProjectileID.DarkLance;

            Item.height = 50;
            Item.width = 50;

            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.value = PriceByRarity.Cyan_9;
            Item.rare = ItemRarityID.Cyan;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.Longinus>();

        }

        public override void ModifyWeaponDamage(Player player, ref float add, ref float mult, ref float flat) {
            if (player.gravDir == 1f && player.velocity.Y > 0 || player.gravDir == -1f && player.velocity.Y < 0) {
                mult = 1.5f;
            }

        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.ChlorophytePartisan, 1);
            recipe.AddIngredient(Mod.GetItem("GuardianSoul"), 1);
            recipe.AddIngredient(Mod.GetItem("SoulOfAttraidies"), 5);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 160000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}
