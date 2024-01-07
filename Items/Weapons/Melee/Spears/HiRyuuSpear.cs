using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Melee.Spears;
using Terraria.Localization;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee.Spears
{
    public class HiRyuuSpear : ModItem
    {
        public static float HiRyuuSpearDamageBoost = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(HiRyuuSpearDamageBoost);

        public override void SetDefaults()
        {
            Item.damage = 170; //was 78
            Item.knockBack = 7f;
            Item.crit = 11;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 15;
            Item.useTime = 5;
            Item.shootSpeed = 5;
            //item.shoot = ProjectileID.DarkLance;

            Item.height = 50;
            Item.width = 50;

            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.value = PriceByRarity.LightPurple_6;
            Item.rare = ItemRarityID.LightPurple;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<HiRyuuSpearProj>();

        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MonkStaffT2);
            recipe.AddIngredient(ItemID.SoulofFlight, 5);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.ObsidianSwordfish);
            recipe.AddIngredient(ItemID.SoulofFlight, 5);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
