using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class DyingFireCrystal : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The fading Crystal of Earth. \n" + "Will summon Marilith. \n" + "Item is non-consumable");
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
            NPC.SpawnOnPlayer(player.whoAmI, NPCID.CorruptBunny);
            //NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Marilith>());
            return true;
        }
        public override bool CanUseItem(Player player) {
            return !(NPC.AnyNPCs(NPCID.CorruptBunny));
            //todo fix placeholder
            //return (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Marilith>()));
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Ruby, 300);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
