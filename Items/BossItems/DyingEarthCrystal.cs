using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class DyingEarthCrystal : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The fading Crystal of Earth. \n" + "Will summon Lich. \n" + "Item is non-consumable");
        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.width = 12;
            item.height = 12;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useAnimation = 5;
            item.useTime = 5;
            item.maxStack = 1;
            item.consumable = false;
        }


        public override bool UseItem(Player player)
        {
            //Main.NewText("Earth Fiend Lich ascends from the ground", Color.GreenYellow);
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Fiends.EarthFiendLich>());
            return true;
        }
        public override bool CanUseItem(Player player) {

            return (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Fiends.EarthFiendLich>()));
        }

       
        public override void AddRecipes() {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                ModRecipe recipe = new ModRecipe(mod);
                recipe.AddIngredient(mod.GetItem("WhiteTitanite"), 10);
                recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this, 1);
                recipe.AddRecipe();
            }
        }
       
    }
}
