
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class CelestialLance : ModItem
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Celestial Lance");
            // Tooltip.SetDefault("Celestial lance fabled to hold sway over the world.\nDoubled damage while falling and has a chance to heal 6 HP on hit.");
        }


        public override void SetDefaults()
        {
            Item.damage = 206;
            Item.knockBack = 10f;

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

            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.Spears.CelestialLanceProj>();

        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.gravDir == 1f && player.velocity.Y > 0 || player.gravDir == -1f && player.velocity.Y < 0)
            {
                damage += 2f;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Longinus>());
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(), 20);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 20);
            recipe.AddIngredient(ItemID.FallenStar, 20);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 240000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
