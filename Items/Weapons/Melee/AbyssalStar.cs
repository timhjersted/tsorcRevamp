using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class AbyssalStar : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 58;
            Item.height = 58;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.autoReuse = true;
            Item.crit = 10;
            Item.damage = 500;
            Item.knockBack = 2;
            Item.UseSound = SoundID.Item1;
            Item.shootSpeed = 15;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.MeleeNoSpeed; 
            Item.shoot = ModContent.ProjectileType<Projectiles.Melee.AbyssalStarProj>();

        }
        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Melee.AbyssalStarProj>()] < 5;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<FlameOfTheAbyss>(), 8);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 15);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 90000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
