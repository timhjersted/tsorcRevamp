using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Magic
{
    [LegacyName("AncientDragonScaleHelmet")]
    [AutoloadEquip(EquipType.Head)]
    public class DragonScaleHelmet : ModItem
    {
        public static int MaxMana = 80;
        public static float CritChance = 22f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxMana, CritChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 20;
            Item.defense = 3;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += MaxMana;
            player.GetCritChance(DamageClass.Magic) += CritChance;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilHood);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.MythrilHood);
            recipe2.AddIngredient(ItemID.OrichalcumHeadgear);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
