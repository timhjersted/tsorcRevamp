using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Head)]
    public class NecromancersSkullmask : ModItem
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
            Item.height = 16;
            Item.defense = 19;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += CritChanceMult;
            player.GetCritChance(DamageClass.Magic) *= 1f + CritChanceMult / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpectreHood);
            recipe.AddIngredient(ModContent.ItemType<LichBone>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.SpectreMask);
            recipe2.AddIngredient(ModContent.ItemType<LichBone>());
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
