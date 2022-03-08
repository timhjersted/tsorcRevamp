using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class WateryEgg : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons The Sorrow \n" + "Must be used near the ocean");
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


        public override bool UseItem(Player player) {
            bool zoneJ = (player.position.X < 250 * 16 || player.position.X > (Main.maxTilesX - 250) * 16);
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) {
                return false;
            }
            else if (!zoneJ) {
                Main.NewText("You can only use this in the Ocean.");
            }
            else {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.TheSorrow>());
            }
            return true;
        }

        public override void AddRecipes() {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                ModRecipe recipe = new ModRecipe(mod);
                
                recipe.AddIngredient(ItemID.MythrilOre, 30);
                recipe.AddIngredient(ItemID.Coral, 1);
                recipe.AddIngredient(ItemID.ShadowScale, 1);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this, 1);
                recipe.AddRecipe();
            }
        }
    }
}
