using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Magic;
using tsorcRevamp.Items.Weapons.Melee.Axes;

namespace tsorcRevamp.Items.Debug
{
    class BetterGodmode : ModItem
    {

        public override string Texture => "tsorcRevamp/Items/Potions/HolyWarElixir";

        public override void SetDefaults()
        {
            Item.width = 1;
            Item.height = 1;
            Item.value = 69; //i am the master of comedy
            Item.accessory = true;
            Item.rare = ItemRarityID.Expert;
        }

        public override void SetStaticDefaults()
        {
        }

        public override void UpdateEquip(Player player)
        {
            player.immune = true;
            player.lifeRegen += 8000;
        }

        public override void AddRecipes()
        {
            var recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<ForgottenRuneAxe>(), 1);
            recipe.AddCondition(tsorcRevampWorld.DebugModeEnabled);
            
            recipe.Register();
        }
    }
}
