using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class FieryEgg : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons The Rage \n" + "You must sacrifice this at the Demon Altar \n" + "within the Cathedral in the Underworld");
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
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.TheRage>())) {
                return false;
            }
            else if (!player.ZoneUnderworldHeight) {
                Main.NewText("You can only use this in the Underworld.");
            }
            else {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.TheRage>());
            }
            return true;
        }

        public override void AddRecipes() {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                ModRecipe recipe = new ModRecipe(mod);
                recipe.AddIngredient(ItemID.CobaltOre, 30);
                recipe.AddIngredient(ItemID.ShadowScale, 1);
                recipe.AddTile(TileID.DemonAltar);
                recipe.SetResult(this, 1);
                recipe.AddRecipe();
            }
        }
    }
}
