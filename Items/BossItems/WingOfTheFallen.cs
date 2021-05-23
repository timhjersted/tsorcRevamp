using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class WingOfTheFallen : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Wing of the Fallen");
            Tooltip.SetDefault("Summons the Wyvern Mage, a powerful demon adept with the forces of magic and lightning\n" +
                "The Wyvern Mage is known to command the loyalty of a fiery Wyvern with white scales as strong as steel\n" +
                "If you cannot defeat the Wyvern Mage after several tries, it would be wise\n" +
                "to return later when you've become stronger and possess greater abilities.");
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
            //if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.WyvernMage>()))
                {
                return false;
            }
            if (Main.dayTime) {
                Main.NewText("The Wyvern Mage is not present in this dimension... Retry at night.", 175, 75, 255);
            }
            else {
                Main.NewText("It was a mistake to come here, Red... ", 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, NPCID.CorruptBunny);
                //NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.WyvernMage>());
                //NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.MechaDragonHead>());
            }
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofFlight, 15);
            recipe.AddIngredient(ItemID.Feather, 99);
            recipe.AddIngredient(ItemID.ShadowScale, 99);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 100);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
