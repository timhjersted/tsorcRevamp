using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class HellkiteStone : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons a Hellkite Dragon from the sky...");
        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.width = 38;
            item.height = 34;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useAnimation = 45;
            item.useTime = 45;
            item.maxStack = 1;
            item.consumable = false;
        }


        public override bool UseItem(Player player) {
            if (NPC.AnyNPCs(NPCID.CorruptBunny))
                //(NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.HellkiteDragonHead>()));
                {
                return false;
            }
            if (Main.dayTime) {
                Main.NewText("Nothing happens... Retry at night.", 175, 75, 255);
            }
            else {
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.CorruptBunny);
                //NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.HellkiteDragonHead>());
            }
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("RedTitanite"), 1);
            recipe.AddIngredient(mod.GetItem("SoulOfAttraidies"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
