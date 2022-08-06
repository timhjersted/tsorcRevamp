using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Body)]
    public class FighterBreastplate : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Adept at close combat\nSet Bonus: +25% Melee damage, +17% Melee Crit\n+20% Melee Speed");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 24;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += 0.2f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteBreastplate, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
