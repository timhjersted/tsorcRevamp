using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Head)]
    public class OldChainCoif : ModItem
    {
        public static float AtkSpeed = 9f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AtkSpeed);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 26;
            Item.height = 20;
            Item.defense = 1;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Summon) += AtkSpeed / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverHelmet);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 300);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

    }
}
