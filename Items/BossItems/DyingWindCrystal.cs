using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class DyingWindCrystal : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The fading Crystal of Wind. \n" + "Will summon Chaos.");
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
            //NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Chaos>());
            return true;
        }
        public override bool CanUseItem(Player player) {
            return !(NPC.AnyNPCs(NPCID.CorruptBunny));
            //todo fix placeholder
            //return (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Chaos>()));
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Topaz, 300);
            recipe.AddIngredient(mod.GetItem("DyingWindShard"), 100);
            recipe.AddIngredient(mod.GetItem("CursedSoul"), 35);
            recipe.AddIngredient(mod.GetItem("SoulOfAttraidies"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
