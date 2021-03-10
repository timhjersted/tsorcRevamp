using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class CrystalArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Crystal armor vibrates with a mysterious energy\nthat attracts enemies to you in greater numbers\nSet bonus:+40% Melee Speed, +15% Melee Damage,\n-30% Ranged and Magic Damage");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 20;
            item.value = 7000000;
            item.rare = ItemRarityID.Pink;
        }


        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CobaltBreastplate, 1);
            recipe.AddIngredient(ItemID.CrystalShard, 30);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
