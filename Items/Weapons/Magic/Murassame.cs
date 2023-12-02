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
        }

        public override void SetDefaults()
        {
            Item.width = 48;
            Item.height = 48;
            Item.useAnimation = 16;
            Item.useTime = 16;
            Item.damage = 20;
            Item.knockBack = 5;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightRed;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 20;
            Item.shoot = ModContent.ProjectileType<Projectiles.HealingWater>();
            Item.shootSpeed = 12f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CobaltBar, 1);
            recipe.AddIngredient(ItemID.MythrilBar, 3);
            recipe.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe.AddIngredient(ModContent.ItemType<Muramassa>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 7000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            if (player.statManaMax2 >= 200)
            {
                damage.Flat += (player.statManaMax2 - 200) / 8;
            }
        }
    }
}
