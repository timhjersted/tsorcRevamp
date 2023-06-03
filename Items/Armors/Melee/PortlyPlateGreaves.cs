using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Legs)]
    public class PortlyPlateGreaves : ModItem
    {
        public static float MeleeSpeed = 27f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MeleeSpeed);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 9;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += MeleeSpeed / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GladiatorLeggings);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1650);
            recipe.AddTile(TileID.DemonAltar);

            //recipe.Register();
        }
    }
}

