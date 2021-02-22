using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class ArmorOfArtorias : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Armor of Artorias");
            Tooltip.SetDefault("Enchanted armor of Artorias of the Abyss.\nPossesses the same power as the Titan Glove.\nSet bonus gives +37% damage, +50% move speed, 8 life regen, lava immunity and many other abilities.");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 30;
            item.value = 40000;
            item.rare = ItemRarityID.Purple;
        }

        public override void UpdateEquip(Player player)
        {
            player.kbGlove = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("SoulOfArtorias"), 2);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 60000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
