using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Melee.Claws
{
    class ForgottenTigerFangs : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Long and extremely sharp fighting claws.");
        }

        public override void SetDefaults()
        {
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.rare = ItemRarityID.Cyan;
            Item.damage = 150;
            Item.height = 12;
            Item.knockBack = 3;
            Item.DamageType = DamageClass.Melee;
            Item.useAnimation = 8;
            Item.useTime = 8;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Cyan_9;
            Item.width = 18;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("ForgottenKaiserKnuckles").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("GuardianSoul").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
