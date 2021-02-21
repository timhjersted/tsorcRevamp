using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class AncientMagicPlateArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Fueled by a magical gem in the chest.\n+15% melee speed, -15% mana cost, 20% chance not to consume ammo\n Set bonus: +6% damage all types, +40 mana");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 12;
            item.value = 9000000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.meleeSpeed += 0.15f;
            player.manaCost -= 0.15f;
            player.ammoCost80 = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CobaltBreastplate, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
