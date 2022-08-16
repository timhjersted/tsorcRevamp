using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Head)]
    public class MirkwoodElvenBlondeHairStyle : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Gifted with ranged combat. High defense not necessary" +
                "\nWith eyes of a hunter you can spot enemies in the dark");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 12;
            Item.defense = 1;
            Item.rare = ItemRarityID.LightPurple;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.detectCreature = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilHat, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
