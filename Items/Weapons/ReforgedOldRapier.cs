using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    class ReforgedOldRapier : ModItem
    {

        public override void SetDefaults()
        {
            item.damage = 11;
            item.width = 40;
            item.height = 40;
            item.knockBack = 3;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1;
            item.useAnimation = 12;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.Stabbing;
            item.useTime = 15;
            item.value = 200;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldRapier"));
            recipe.AddTile(mod.GetTile("SweatyCyclopsForge"));
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
