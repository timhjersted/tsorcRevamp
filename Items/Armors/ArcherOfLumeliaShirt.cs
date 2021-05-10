using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class ArcherOfLumeliaShirt : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Archer of Lumelia Shirt");
            Tooltip.SetDefault("Gifted with lethal archery abilities\n25% chance not to consume ammo.\nSet Bonus: +23% Ranged Crit, +15% Ranged Damage, Archery Skill (arrow speed & dmg +20%)");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 20;
            item.value = 5000;
            item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.ammoCost75 = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.AdamantiteBreastplate, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 4000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
