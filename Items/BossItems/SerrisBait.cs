using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class SerrisBait : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons Serris\n" + "Must be used within the Temple of Serris.");
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
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>()))
                {
                return false;
            }
            else {
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>());
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>());
            }
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SharkFin, 99);
            recipe.AddIngredient(ItemID.Goldfish, 99);
            recipe.AddIngredient(ItemID.ShadowScale, 99);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
