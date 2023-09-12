using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.Flamethrowers
{
    class Meltdown : ModItem
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
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.damage = 66;
            Item.knockBack = 2;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item34;
            Item.rare = ItemRarityID.LightPurple;
            Item.shootSpeed = 9;
            Item.useAmmo = AmmoID.Gel;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightPurple_6;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<Projectiles.Ranged.MeltdownFirestorm>();
            Item.channel = true;
        }

        //Only one allowed at a time
        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Ranged.MeltdownFirestorm>()] == 0)
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
            recipe.AddIngredient(ModContent.ItemType<Items.Materials.DamagedMechanicalScrap>(), 1);
            recipe.AddIngredient(ItemID.SoulofFright, 15);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
