using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class ReforgedOldHalberd : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/OldHalberd";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Left-click to stab like a spear, right-click to swing" +
                                "\nSwing strike does 25% more damage");

        }

        public override void SetDefaults()
        {
            Item.damage = 14;
            Item.width = 68;
            Item.height = 68;
            Item.knockBack = 6;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.scale = 1f;
            Item.useAnimation = 32;
            Item.rare = ItemRarityID.White;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 32;
            Item.value = 7000;
            Item.shootSpeed = 2.7f;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {

                Item.useStyle = ItemUseStyleID.Swing;
                Item.shoot = ProjectileID.None;
                Item.noMelee = false;
                Item.noUseGraphic = false;
                Item.damage = 18;
            }
            else
            {
                Item.noMelee = true;
                Item.noUseGraphic = true;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ModContent.ProjectileType<Projectiles.OldHalberd>();
                Item.damage = 14;

            }
            return base.CanUseItem(player);
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("OldHalberd").Type);
            recipe.AddTile(ModContent.TileType<Tiles.SweatyCyclopsForge>());

            recipe.Register();

        }
    }
}