using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class EphemeralThrowingAxe2 : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("An enchanted melee weapon that can be thrown through walls.\n" + "It does double damage against mages and other magic users.");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.consumable = false;
            Item.damage = 60;
            Item.width = 34;
            Item.height = 58;
            Item.knockBack = 7;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.shootSpeed = 13;
            Item.useAnimation = 24;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 24;
            Item.value = 150000;
            Item.shoot = ModContent.ProjectileType<Projectiles.EphemeralThrowingAxeProj2>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<EphemeralThrowingAxe>());
            //recipe.AddIngredient(ItemID.SoulofNight, 8);
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 12000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
