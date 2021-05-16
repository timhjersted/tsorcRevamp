using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.Okiku.FirstForm;
using tsorcRevamp.NPCs.Bosses.Okiku.SecondForm;
using tsorcRevamp.NPCs.Bosses.Okiku.ThirdForm;


namespace tsorcRevamp.Items.BossItems {
    class MindCube : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons the Mindflayer King \n" +
                "This is it. The final battle. \n" +
                "Item is not consumed on use");
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
            NPC.NewNPC((int)player.position.X, (int)player.position.Y - 64, ModContent.NPCType<DarkShogunMask>());
            Main.NewText("You are a fool, Red. You think you can defeat me?...", 175, 75, 255);;
            return true;
        }
        public override bool CanUseItem(Player player) {
            if (NPC.AnyNPCs(ModContent.NPCType<DarkShogunMask>())
                || NPC.AnyNPCs(ModContent.NPCType<DarkDragonMask>())
                || NPC.AnyNPCs(ModContent.NPCType<Okiku>())
                || NPC.AnyNPCs(ModContent.NPCType<BrokenOkiku>())
                ) {
                return false;
            }
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.LightShard, 99);
            recipe.AddIngredient(ItemID.DarkShard, 99);
            recipe.AddIngredient(mod.GetItem("CrestOfSky"), 1);
            recipe.AddIngredient(mod.GetItem("CrestOfFire"), 1);
            recipe.AddIngredient(mod.GetItem("CrestOfWater"), 1);
            recipe.AddIngredient(mod.GetItem("CrestOfEarth"), 1);
            recipe.AddIngredient(mod.GetItem("CrestOfCorruption"), 1);
            recipe.AddIngredient(mod.GetItem("CrestOfSteel"), 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
