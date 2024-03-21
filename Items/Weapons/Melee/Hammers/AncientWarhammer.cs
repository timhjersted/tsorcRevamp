using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Hammers
{
    class AncientWarhammer : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ancient Warhammer");
            // Tooltip.SetDefault("An old choice for advancing druids");

        }

        public override void SetDefaults()
        {

            Item.rare = ItemRarityID.Green;
            Item.DamageType = DamageClass.Melee;
            Item.damage = 60;
            Item.width = 48;
            Item.height = 48;
            Item.knockBack = 9f;
            Item.autoReuse = true;
            Item.useTurn = false;
            Item.useAnimation = 40;
            Item.useTime = 40;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.value = PriceByRarity.Green_2;
            Item.shoot = ModContent.ProjectileType<Projectiles.Nothing>();
            tsorcInstancedGlobalItem instancedGlobal = Item.GetGlobalItem<tsorcInstancedGlobalItem>();
            instancedGlobal.slashColor = Microsoft.Xna.Framework.Color.DarkGray;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.TheBreaker);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 200);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
