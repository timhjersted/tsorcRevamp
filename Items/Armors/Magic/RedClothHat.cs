using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Magic
{
    [LegacyName("RedMageHat")]
    [AutoloadEquip(EquipType.Head)]
    public class RedClothHat : ModItem
    {
        public static float CritChance = 5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance);
        public override void SetStaticDefaults()
        {
            ArmorIDs.Head.Sets.DrawHatHair[Item.headSlot] = true;
        }
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 16;
            Item.defense = 2;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Magic) += CritChance;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Silk, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
