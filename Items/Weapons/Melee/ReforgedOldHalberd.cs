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
            item.damage = 14;
            item.width = 68;
            item.height = 68;
            item.knockBack = 6;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1f;
            item.useAnimation = 28;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 28;
            item.value = 7000;
        }

        public override bool AltFunctionUse(Player player)
        {
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {

                item.useStyle = ItemUseStyleID.SwingThrow;
                item.shoot = ProjectileID.None;
                item.noMelee = false;
                item.noUseGraphic = false;
                item.damage = 18;
            }
            else
            {
                item.noMelee = true;
                item.noUseGraphic = true;
                item.useStyle = ItemUseStyleID.HoldingOut;
                item.shoot = ModContent.ProjectileType<Projectiles.OldHalberd>();
                item.shootSpeed = 2.7f;
                item.damage = 14;

            }
            return base.CanUseItem(player);
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldHalberd"));
            recipe.AddTile(mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();

        }
    }
}