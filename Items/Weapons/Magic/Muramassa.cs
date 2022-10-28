using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class Muramassa : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A sword crafted for magic users" +
                               "\nDeals +1 damage for every 20 mana the user has over 200" +
                               "\nCan be upgraded");// with 25,000 Dark Souls & 3 Souls of Light
        }

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 48;
            Item.useAnimation = 18;
            Item.useTime = 18;
            Item.damage = 12;
            Item.knockBack = 3;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Orange;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 5;
            Item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            Item.shootSpeed = 11f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Muramasa, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.statManaMax2 >= 200)
            {
                damage.Flat += (player.statManaMax2 - 200) / 20;
            }
        }
    }
}
