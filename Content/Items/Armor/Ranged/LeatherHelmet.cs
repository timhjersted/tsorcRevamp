using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Ranged
{
    [LegacyName("OldStuddedLeatherHelmet")]
    [LegacyName("OldLeatherHelmet")]
    [AutoloadEquip(EquipType.Head)]
    public class LeatherHelmet : ModItem
    {
        public static float CritChance = 8f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.defense = 2;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Ranged) += CritChance;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Leather, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 150);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
