using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Armor.Melee.AncientGoldenSet
{
    [AutoloadEquip(EquipType.Body)]
    public class AncientGoldenArmor : ModItem
    {
        public const float AtkSpeed = 11f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AtkSpeed);
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 6;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += AtkSpeed / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GoldChainmail, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 250);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
