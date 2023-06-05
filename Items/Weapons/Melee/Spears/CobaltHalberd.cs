using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{

    public class CobaltHalberd : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Cobalt Halberd");
            /* Tooltip.SetDefault("Left-click to stab like a spear, right-click to swing" +
                                "\nSwing strike does 25% more damage"); */

        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.damage = 29;
            Item.width = 76;
            Item.height = 74;
            Item.knockBack = (float)6;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Melee;
            Item.autoReuse = false;
            Item.scale = 1;
            Item.useAnimation = 28;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 28;
            Item.value = PriceByRarity.LightRed_4;
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
                Item.damage = 36;
            }
            else
            {
                Item.damage = 29;
                Item.noMelee = true;
                Item.noUseGraphic = true;
                Item.useStyle = ItemUseStyleID.Shoot;
                Item.shoot = ModContent.ProjectileType<Projectiles.Spears.CobaltHalberdProj>();
            }
            return base.CanUseItem(player);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.CobaltBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}
