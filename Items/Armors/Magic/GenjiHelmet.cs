using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Head)]
    public class GenjiHelmet : ModItem
    {
        public static float Dmg = 28f;
        public static float CritChance = 26f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, CritChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 9;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Magic) += Dmg / 100f;
            player.GetCritChance(DamageClass.Magic) += CritChance;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteHeadgear);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AdamantiteHeadgear);
            recipe2.AddIngredient(ItemID.TitaniumHeadgear);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
