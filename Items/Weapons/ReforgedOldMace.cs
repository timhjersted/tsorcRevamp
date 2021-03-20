using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons
{
    class ReforgedOldMace : ModItem
    {

        public override void SetDefaults()
        {
            item.damage = 15; //buffed
            item.width = 36;
            item.height = 36;
            item.knockBack = 6.5f;
            item.maxStack = 1;
            item.melee = true;
            item.scale = .9f;
            item.useAnimation = 22;
            item.rare = ItemRarityID.White;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 8000;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldMace"));
            //recipe.AddTile(mod.GetTile("SweatyCyclopsForge"));
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
