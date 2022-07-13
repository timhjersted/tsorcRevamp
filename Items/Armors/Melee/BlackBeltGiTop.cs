using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Body)]
    public class BlackBeltGiTop : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Adds Double Jump & Jump Boost Skills\nYou are a master of the zen arts, at one with the Tao\nSet Bonus: +20% Melee damage, +20% Melee Speed, +7% Melee Crit\nZen meditation adds amazing +13 life regen" +
                "\nDefense is capped at 30\nDamage reduction is converted into movement speed");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 2;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.hasJumpOption_Cloud = true;
            player.jumpBoost = true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilChainmail, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
