using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {

    public class CobaltHalberd : ModItem
    {
        public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Cobalt Halberd");
        Tooltip.SetDefault("Left-click to stab like a spear, right-click to swing" +
                            "\nSwing strike does 25% more damage");

	}

        public override void SetDefaults()
        {
            item.rare = ItemRarityID.LightRed;
            item.damage = 29;
            item.width = 76;
            item.height = 74;
            item.knockBack = (float)6;
            item.maxStack = 1;
            item.melee = true;
            item.autoReuse = false;
            item.scale = 1;
            item.useAnimation = 28;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 28;
            item.value = PriceByRarity.LightRed_4;
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
                item.damage = 36;
            }
            else
            {
                item.damage = 29;
                item.noMelee = true;
                item.noUseGraphic = true;
                item.useStyle = ItemUseStyleID.HoldingOut;
                item.shoot = ModContent.ProjectileType<Projectiles.CobaltHalberd>();
                item.shootSpeed = 2.7f;
            }
            return base.CanUseItem(player);
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.CobaltBar, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}
