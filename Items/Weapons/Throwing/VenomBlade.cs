using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Throwing
{
    class VenomBlade : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.consumable = true;
            Item.damage = 54;
            Item.height = 66;
            Item.knockBack = 5;
            Item.maxStack = Item.CommonMaxStack;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.Ranged;
            Item.scale = 1f;
            Item.shootSpeed = 15;
            Item.useAnimation = 22;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 22;
            Item.value = 100;
            Item.width = 10;
            Item.shoot = ModContent.ProjectileType<Projectiles.Throwing.VenomBlade>();
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(150);
            recipe.AddIngredient(ItemID.PoisonedKnife, 150); 
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(ItemID.CursedFlame, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 950);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
