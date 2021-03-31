using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class JungleFeather : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons the Jungle Wyvern \n" + "An ancient beast that once guarded an advanced civilization, \n" + "long since forgotten. To this day, it watches over the lost \n" + "city, ripping to shreds any traveler who should discover it.");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useAnimation = 45;
            item.useTime = 45;
            item.maxStack = 1;
            item.consumable = false;
            item.rare = ItemRarityID.LightRed;
            item.consumable = false;
        }


        public override bool UseItem(Player player) {
            if (NPC.AnyNPCs(NPCID.CorruptBunny))
                //(NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.JungleWyvernHead>()));
                {
                return false;
            }
            if (Main.dayTime) {
                Main.NewText("The ancient Jungle Wyvern remains deep in slumber... Retry at night.", 175, 75, 255);
            }
            else if (!player.ZoneRockLayerHeight) {
                Main.NewText("The ancient Jungle Wyvern must be summoned underground.", 175, 75, 255);
            }
            else {
                Main.NewText("A rumbling thunder shakes the ground below you... ", 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.CorruptBunny);
                //NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.JungleWyvernHead>());
            }
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Emerald, 300);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
