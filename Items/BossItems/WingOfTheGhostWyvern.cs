using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class WingOfTheGhostWyvern : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Wing of the Ghost Wyvern");
            Tooltip.SetDefault("Summons the Wyvern Mage Shadow from The Abyss.\n" +
                "The Wyvern Mage, once killed at your hands, has only grown more powerful since being\n" +
                "transformed into his undead shadow form when he escaped to The Abyss.\n" +
                "If you cannot defeat the Wyvern Mage Shadow after several tries, it would\n" +
                "be wise to try later when you've become stronger and possess greater abilities.");
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
            //if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.WyvernMageShadow>()))
                {
                return false;
            }
            if (Main.dayTime) {
                Main.NewText("The Ghost Wyvern is not present in this dimension... Retry at night.", 175, 75, 255);
            }
            else {
                Main.NewText("You think death is the end? You haven't begun to understand my powers, Red... ", 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.CorruptBunny);
                //NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.WyvernMageShadow>());
            }
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("WingOfTheFallen"), 1);
            recipe.AddIngredient(mod.GetItem("FlameOfTheAbyss"), 20);
            recipe.AddIngredient(mod.GetItem("SoulOfAttraidies"), 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
