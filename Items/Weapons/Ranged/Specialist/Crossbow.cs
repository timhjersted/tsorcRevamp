using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Ammo;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Ranged.Specialist
{
    class Crossbow : ModItem
    {
        public const int BaseDmg = 10;
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.damage = BaseDmg;
            Item.knockBack = 4;
            Item.crit = 16;
            Item.shootSpeed = 10;
            Item.useTime = 35;
            Item.useAnimation = 35;
            Item.width = 42;
            Item.height = 20;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.useAmmo = ModContent.ItemType<Bolt>();
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.White_0;
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (Item.prefix == PrefixID.Awful)
            {
                damage.Flat -= 7;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Wood, 5);
            recipe.AddIngredient(ItemID.StoneBlock, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 50);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
