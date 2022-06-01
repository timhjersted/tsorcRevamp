﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class OldStuddedLeatherGreaves : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 3;
            Item.value = 650;
            Item.rare = ItemRarityID.White;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("OldLeatherGreaves").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 500);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}

