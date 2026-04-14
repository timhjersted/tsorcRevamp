using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Damage
{
    [AutoloadEquip(EquipType.Shield)]
    public class MythrilBulwark : ModItem
    {
        public static float Vulnerability = 25f;
        public static int VulnerabilityDuration = 10;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Vulnerability, VulnerabilityDuration);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.defense = 6;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().MythrilBulwark = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.MythrilBar, 15);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

