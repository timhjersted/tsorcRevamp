using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Melee
{
    [LegacyName("HelmetOfArtorias")]
    [AutoloadEquip(EquipType.Head)]
    public class ArtoriasHelmet : ModItem
    {
        public const int SoulCost = 70000;
        public static float CritChanceMult = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChanceMult);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 28;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += CritChanceMult;
            player.GetCritChance(DamageClass.Melee) *= 1f + CritChanceMult / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BeetleHelmet);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), SoulCost);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
