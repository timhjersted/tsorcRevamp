using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Body)]
    public class TarantulaCarapace : ModItem
    {
        public static float Dmg = 20f;
        public static int MinionSlots = 1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg, MinionSlots);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 40;
            Item.height = 30;
            Item.defense = 11;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += Dmg / 100f;
            player.maxMinions += MinionSlots;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpiderBreastplate);
            recipe.AddIngredient(ItemID.MythrilBar);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8800);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.SpiderBreastplate);
            recipe2.AddIngredient(ItemID.OrichalcumBreastplate);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}