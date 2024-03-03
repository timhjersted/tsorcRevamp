using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.Flamethrowers
{
    class Freezethrower : ModItem
    {

        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 60;
            Item.height = 22;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 30;
            Item.useTime = 5;
            Item.damage = 22;
            Item.knockBack = 2;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item34;
            Item.rare = ItemRarityID.LightPurple;
            Item.shootSpeed = 9;
            Item.useAmmo = AmmoID.Gel;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightPurple_6;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<Projectiles.Freezethrower>();
            Item.channel = true;
        }

        //Only one allowed at a time
        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Freezethrower>()] == 0)
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
            recipe.AddIngredient(ItemID.SlimeGun, 1);
            recipe.AddIngredient(ItemID.FrostDaggerfish, 5);
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 20);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.SlimeGun, 1);
            recipe2.AddIngredient(ModContent.ItemType<EphemeralDust>(), 20);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();
        }
    }
}
