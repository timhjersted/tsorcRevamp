using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    [AutoloadEquip(EquipType.Shield)]
    public class AncientDemonShield : ModItem
    {
        public static float DamageReduction = 5f;
        public static float Thorns = 1f;
        public static int SoulCost = 4000;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageReduction, Thorns * 100);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 38;
            Item.defense = 4;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.endurance += DamageReduction / 100f;
            player.noKnockback = true;
            player.fireWalk = true;
            player.thorns += Thorns;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ObsidianShield);
            recipe.AddIngredient(ModContent.ItemType<SpikedIronShield>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
