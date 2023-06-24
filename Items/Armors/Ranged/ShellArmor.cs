using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Body)]
    class ShellArmor : ModItem
    {
        public static float Dmg = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.defense = 9;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.fromItem(Item);
            Item.width = 18;
            Item.height = 18;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.NecroBreastplate);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += Dmg / 100f;
        }
    }
}
