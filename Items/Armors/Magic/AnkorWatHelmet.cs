using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Magic
{
    [AutoloadEquip(EquipType.Head)]
    public class AnkorWatHelmet : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("14% Less Mana Usage.\n+14 Defense when health is less than 120");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 14;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.manaCost -= 0.14f;

            if (player.statLife <= 120)
            {
                player.statDefense += 14;
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedHeadgear, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
