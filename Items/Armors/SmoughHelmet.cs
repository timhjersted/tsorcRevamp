using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class SmoughHelmet : ModItem 
    {
        public static float Dmg = 15f;
        public override LocalizedText Tooltip => base.Tooltip;
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
            player.GetDamage(DamageClass.Generic) += Dmg;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CopperHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
