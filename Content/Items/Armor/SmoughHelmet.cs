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
    public class SmoughHelmet : ModItem
    {
        public static float Dmg = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Dmg);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 4;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Generic) += Dmg / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CopperHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 1500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
