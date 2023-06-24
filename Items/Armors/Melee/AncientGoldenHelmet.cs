using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Melee
{
    [LegacyName("AncientDwarvenHelmet")]
    [LegacyName("AncientDwarvenHelmet2")]
    [AutoloadEquip(EquipType.Head)]
    public class AncientGoldenHelmet : ModItem
    {
        public static float CritChance = 6f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 4;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Melee) += CritChance;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GoldHelmet, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 250);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
