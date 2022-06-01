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
            Tooltip.SetDefault("A powerful magic/low defense set chosen by skilled Paladins with a taste for high risk/reward combat\nSet bonus: 20% magic crit, +30% magic damage, +60 mana, -17% mana cost + Darkmoon Cloak skill\nDarkmoon Cloak activates rapid mana regen, Star Cloak & Doubles magic crit and damage when life falls below 100");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 8;
            Item.value = 1200000;
            Item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilChainmail, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3500);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}
