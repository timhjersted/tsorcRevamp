using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class Murassame : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("A sword crafted for magic users" +
                               "\nDeals +1 damage for every 10 mana the user has over 200"); */
        }

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 48;
            Item.useAnimation = 16;
            Item.useTime = 16;
            Item.damage = 18;
            Item.knockBack = 5;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightRed;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 5;
            Item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            Item.shootSpeed = 12f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddIngredient(ModContent.ItemType<Muramassa>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.statManaMax2 >= 200)
            {
                damage.Flat += (player.statManaMax2 - 200) / 10;
            }
        }
    }
}
