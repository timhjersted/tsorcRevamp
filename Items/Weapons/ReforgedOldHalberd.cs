using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    class ReforgedOldHalberd : ModItem
    {
        public override void SetDefaults()
        {
            item.damage = 18;
            item.width = 60;
            item.height = 60;
            item.knockBack = 6;
            item.maxStack = 1;
            item.melee = true;
            item.scale = 1f;
            item.useAnimation = 28;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 7000;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldHalberd"));
            //recipe.AddTile(mod.GetTile("SweatyCyclopsForge"));
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}