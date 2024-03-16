using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    public class Dominatrix : ModItem
    {
        public static float SummonTagCrit = 9;
        public static float CritDamage = 50;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SummonTagCrit, CritDamage);
        public override void SetStaticDefaults()
        {
            CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
        }

        public override void SetDefaults()
        {
            Item.height = 66;
            Item.width = 60;

            Item.DamageType = DamageClass.SummonMeleeSpeed;
            Item.damage = 20;
            Item.knockBack = 2;
            Item.rare = ItemRarityID.Green;
            Item.value = Item.buyPrice(0, 6, 0, 0);

            Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.DominatrixProjectile>();
            Item.shootSpeed = 4;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 30; // for some reason a lower use speed gives it increased range....
            Item.useAnimation = 30;
            Item.noMelee = true;
            Item.noUseGraphic = true;
        }
        public override bool MeleePrefix()
        {
            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DemoniteBar, 3);
            recipe.AddIngredient(ItemID.ShadowScale, 3);
            recipe.AddIngredient(ItemID.CrimtaneBar, 3);
            recipe.AddIngredient(ItemID.TissueSample, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.AdventureModeEnabled);
            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.CrimtaneBar, 3);
            recipe2.AddIngredient(ItemID.TissueSample, 6);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.AddCondition(tsorcRevampWorld.AdventureModeDisabled);
            recipe2.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ItemID.DemoniteBar, 3);
            recipe3.AddIngredient(ItemID.ShadowScale, 6);
            recipe3.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe3.AddTile(TileID.DemonAltar);
            recipe3.AddCondition(tsorcRevampWorld.AdventureModeDisabled);
            recipe3.Register();
        }
    }
}