using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class WingOfTheGhostWyvern : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Wing of the Ghost Wyvern");
            Tooltip.SetDefault("Frees the Wyvern Mage's Shadow from its Glass Prison.\n" +
                "The Wyvern Mage once created a shadow form of himself, cursed by the powers of the Abyss\n" +
                "It was so hideous that the Mage imprisoned his shadow self in a massive glass cage, enchanted by dark magic\n");
               
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
           if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()))
            {
                return false;
            }
            if (Main.dayTime) {
                Main.NewText("The Ghost Wyvern is not present in this dimension... Retry at night.", 175, 75, 255);
                return false;
            }
            else {
                Main.NewText("You think death is the end? You haven't begun to understand my powers, Red... ", 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>());
            }
            return true;
        }

        public override void AddRecipes() {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
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
}
