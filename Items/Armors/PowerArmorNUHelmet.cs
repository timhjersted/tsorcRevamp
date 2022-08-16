using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Head)]
    public class PowerArmorNUHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("20% Increased Melee Damage" +
                "\n+17% crit chance for all classes" +
                "\nLonger Breath");
            DisplayName.SetDefault("Power Armor NU Helmet");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 9;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Melee) += 0.2f;
            player.GetCritChance(DamageClass.Generic) += 17;
            player.breath = 10800;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShadowHelmet, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
