using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.Flamethrowers
{
    class Ichorthrower : ModItem
    {

        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 54;
            Item.height = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.damage = 120;
            Item.knockBack = 2;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item34;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.shootSpeed = 10;
            Item.useAmmo = AmmoID.Gel;
            Item.noMelee = true;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<Projectiles.Ranged.Ichorstorm>();
            Item.channel = true;
        }

        //Only one allowed at a time
        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Ranged.Ichorstorm>()] == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Items.Materials.LichBone>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Items.Materials.WhiteTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 40000);
            recipe.AddTile(TileID.DemonAltar);

            //recipe.Register();
        }
    }
}
