using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class TheBlackenedFlames : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Has a chance to drain enemies' health quickly");

        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 55;
            Item.useTime = 55;
            Item.autoReuse = true;
            Item.damage = 73;
            Item.knockBack = 6;
            Item.scale = 0.9f;
            Item.UseSound = SoundID.Item20;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 10;
            Item.crit = 6;
            Item.mana = 50;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.BlackFire>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.Fireblossom, 3);
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            //recipe.AddIngredient(ItemID.SoulofNight, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 65000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
