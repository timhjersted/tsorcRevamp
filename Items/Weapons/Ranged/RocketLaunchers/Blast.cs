using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.RocketLaunchers
{
    class Blast : ModItem
    {
        public override void SetStaticDefaults()
        {
            /*Tooltip.SetDefault("Uses Rockets as ammo" +
                "\nQuite dangerous to use" +
                "\n'The Number 1 Hero'");*/
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }

        public override void SetDefaults()
        {

            Item.damage = 1500; //This has to be in the thousands for the weapon to be viable, yes. 
            Item.width = 46;
            Item.height = 26;
            Item.DamageType = DamageClass.Ranged;
            Item.autoReuse = true;
            Item.knockBack = 20f;
            Item.noMelee = true;
            Item.shoot = ProjectileID.RocketI;
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Rocket;
            Item.useAnimation = 31;
            Item.useTime = 31;
            Item.UseSound = SoundID.Item14;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.RocketLauncher, 1);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>(), 2);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}