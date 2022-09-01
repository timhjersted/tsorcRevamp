
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class CelestialLance : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Celestial Lance");
            Tooltip.SetDefault("Celestial lance fabled to hold sway over the world.\nGains 50% attack damage while falling. \nAlso has random chance to cast a healing spell on each strike.");
        }


        public override void SetDefaults()
        {
            Item.damage = 200;
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
            Item.shoot = ModContent.ProjectileType<Projectiles.CelestialLance>();

        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.gravDir == 1f && player.velocity.Y > 0 || player.gravDir == -1f && player.velocity.Y < 0)
            {
                damage += 1.5f;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("Longinus").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("WhiteTitanite").Type, 20);
            recipe.AddIngredient(Mod.Find<ModItem>("CursedSoul").Type, 30);
            recipe.AddIngredient(ItemID.FallenStar, 20);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 240000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
