using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class CobaltGreatWarhammer : ModItem
    {
	public override void SetStaticDefaults()
	{
		DisplayName.SetDefault("Cobalt Great Warhammer");
            Tooltip.SetDefault("");

	}

        public override void SetDefaults()
        {
            item.autoReuse = true;
            item.rare = ItemRarityID.LightRed;
            item.damage = 37;
            item.width = 46;
            item.height = 46;
            item.knockBack = 11f;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1;
            item.useAnimation = 38;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 38;
            item.value = 75900;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.CobaltBar, 11);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

        public override void ModifyWeaponDamage(Player player, ref float add, ref float mult, ref float flat)
        {
            player.armorPenetration += 10;
        }
    }
}
