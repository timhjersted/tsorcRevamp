using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Magic
{
    [Autoload(false)]
    [AutoloadEquip(EquipType.Head)]
    public class KrakenHead : ModItem
    {
        public static float CritChanceMult = 20f;
        public const int SoulCost = 70000;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChanceMult);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 18;
            Item.defense = 20;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Ranged) += CritChanceMult;
            player.GetCritChance(DamageClass.Ranged) *= 1f + CritChanceMult / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShroomiteHelmet);
            recipe.AddIngredient(ModContent.ItemType<LichBone>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.ShroomiteHeadgear);
            recipe2.AddIngredient(ModContent.ItemType<LichBone>());
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ItemID.ShroomiteMask);
            recipe3.AddIngredient(ModContent.ItemType<LichBone>());
            recipe3.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe3.AddTile(TileID.DemonAltar);

            recipe3.Register();
        }
    }
}
