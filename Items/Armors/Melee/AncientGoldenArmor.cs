using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [LegacyName("AncientDwarvenArmor")]
    [AutoloadEquip(EquipType.Body)]
    public class AncientGoldenArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A lost prince's armor.\nIncreases melee damage by 3 flat\nSet bonus: +10% melee speed and damage");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 7;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee).Flat += 3;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GoldChainmail, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 250);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
