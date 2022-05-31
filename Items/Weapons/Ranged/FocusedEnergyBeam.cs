using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    public class FocusedEnergyBeam : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Uses Tesla Bolts for ammo\n" +
                        "Fires a single high damage beam that obliterates weaker enemies on contact...\n" +
                        "A weapon from a civilization not of this world...");

        }

        public override void SetDefaults() {
            Item.ranged = true;
            Item.shoot = ProjectileID.PurificationPowder;

            Item.damage = 750;
            Item.width = 52;
            Item.height = 22;
            Item.knockBack = 9.0f;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Red;
            Item.useAmmo = Mod.Find<ModItem>("TeslaBolt").Type;
            Item.shootSpeed = 30;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.UseSound = SoundID.Item12;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Red_10;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 3);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 3);
            recipe.AddIngredient(Mod.GetItem("CompactFrame"));
            recipe.AddIngredient(Mod.GetItem("DestructionElement"));
            recipe.AddIngredient(ItemID.SpaceGun, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 90000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override Vector2? HoldoutOffset() {
            return new Vector2(-6, 0);
        }
    }
}
