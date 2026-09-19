using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Melee
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 250);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
