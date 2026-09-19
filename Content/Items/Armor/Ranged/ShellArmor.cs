using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Armor.Ranged
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += Dmg / 100f;
        }
    }
}
