using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class StoneOfSeath : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Stone of Seath");
            Tooltip.SetDefault("Summons Seath the Scaleless, a great dragon granted the title of Duke by Lord Gwyn for his \n" +
                "assistance in defeating the Everlasting Dragons and given a fragment of a Lord Soul. Seath \n" +
                "was driven insane during his research on the Scales of Immortality, which he could never \n" +
                "obtain. Ironically, he is now an immortal himself, a true Undead by means of his research \n" +
                "into the Primordial Crystal, which he stole from the dragons when he defected.");
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
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>()))
            {
                return false;
            }
            if (Main.dayTime) {
                Main.NewText("Nothing happens... Retry at night.", 175, 75, 255);
                return false;
            }
            else {
                Main.NewText("Thy death will only fuel my immortality, Red... ", 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>());
            }
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("BlueTitanite"), 10);
            recipe.AddIngredient(mod.GetItem("DragonScale"), 15);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
