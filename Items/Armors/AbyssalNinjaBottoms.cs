using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    class AbyssalNinjaBottoms : ModItem
    {
        public static float MoveSpeed = 36f;
        public static float MeleeDmg = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed, MeleeDmg);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.value = PriceByRarity.Purple_11;
            Item.defense = 7;
        }
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
            player.GetDamage(DamageClass.Melee) += MeleeDmg / 100f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<Melee.ShadowNinjaBottoms>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfOccultist>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfAbyssalInvader>());
            recipe.AddIngredient(ModContent.ItemType<FlameOfTheAbyss>(), 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
