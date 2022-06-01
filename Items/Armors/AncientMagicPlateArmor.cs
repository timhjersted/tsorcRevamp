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
            Item.width = 18;
            Item.height = 18;
            Item.defense = 12;
            Item.value = 9000000;
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetAttackSpeed(DamageClass.Melee) += 0.15f;
            player.manaCost -= 0.15f;
            player.ammoCost80 = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.CobaltBreastplate, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
