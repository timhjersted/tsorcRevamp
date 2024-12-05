using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Ammo;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.Specialist
{
    class HeavyCrossbow : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = 20;
            Item.knockBack = 4;
            Item.crit = 16;
            Item.shootSpeed = 11;
            Item.useTime = 22; 
            Item.useAnimation = 22;
            Item.width = 56;
            Item.height = 24;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.useAmmo = ModContent.ItemType<Bolt>();
            Item.UseSound = SoundID.Item98;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
            Item.autoReuse = true; 
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Crossbow>(), 1);
            recipe.AddIngredient(ItemID.TissueSample, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override Vector2? HoldoutOffset()
        {
			return new Vector2(2f, -2f);
		}
    }
}
