using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class AncientHornedHelmet : ModItem
    {
        public static float CritChance = 13f;
        public static int MaxMinions = 2;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance, MaxMinions);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 11;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Generic) += CritChance;
            player.maxMinions += MaxMinions;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddRecipeGroup(tsorcRevampSystems.CobaltHelmets, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}