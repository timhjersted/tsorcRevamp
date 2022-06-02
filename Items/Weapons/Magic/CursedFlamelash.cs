using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class CursedFlamelash : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 19;
            Item.useTime = 19;
            Item.channel = true;
            Item.damage = 47;
            Item.knockBack = 4;
            Item.UseSound = SoundID.Item20;
            Item.rare = ItemRarityID.LightRed;
            Item.crit = 4;
            Item.mana = 17;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.CursedFlamelash>();
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Flamelash, 1);
            recipe.AddIngredient(ItemID.CursedFlame, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 15000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
