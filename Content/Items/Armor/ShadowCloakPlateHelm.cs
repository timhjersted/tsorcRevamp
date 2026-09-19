using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor
{
    [AutoloadEquip(EquipType.Head)]
    public class ShadowCloakPlateHelm : ModItem
    {
        public static float CritChance = 13f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 6;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Generic) += CritChance;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShadowHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 1400);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AncientShadowHelmet);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 1400);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();
        }
    }
}
