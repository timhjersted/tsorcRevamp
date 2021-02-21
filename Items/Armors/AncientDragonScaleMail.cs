using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class AncientDragonScaleMail : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("A powerful magic/low defense set chosen by skilled Paladins with a taste for high risk/reward combat\nSet bonus: 20% magic crit, +30% magic damage, +60 mana, -17% mana cost + Mana Cloak skill\nMana Cloak activates rapid mana regen, Star Cloak & Doubles magic crit and damage when life falls below 100");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 8;
            item.value = 1200000;
            item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MythrilChainmail, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
