using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Weapons.Throwing
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 950);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
