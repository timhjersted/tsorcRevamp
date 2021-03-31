using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class WateryEgg : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons The Sorrow \n" + "Must be sacrificed at a Demon Alter Deep below the Frozen Ocean");
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
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) {
                return false;
            }
            else if (!player.ZoneBeach) {
                Main.NewText("You can only use this in the Ocean.");
            }
            else {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.TheSorrow>());
            }
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Coral, 99);
            recipe.AddIngredient(ItemID.Waterleaf, 99);
            recipe.AddIngredient(ItemID.ShadowScale, 99);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
