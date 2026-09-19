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
    public class AncientHornedHelmet : ModItem
    {
        public static float CritChance = 13f;
        public static int MaxMinions = 1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritChance, MaxMinions);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 8;
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 1500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}