using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.BossItems {
    class BloodySkull : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A powerful weapon which destroys all enemies when used.");
        }
        public override void SetDefaults() {
            item.width = 18;
            item.height = 18;
            item.consumable = false;
            item.maxStack = 1;
            item.value = 100000;
            item.rare = ItemRarityID.Pink;
            item.useTime = 45;
            item.useAnimation = 45;
            item.scale = 1f;
            item.useStyle = ItemUseStyleID.HoldingUp;
        }
        public override bool UseItem(Player player) {
            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Death>()))
            {
                NPC.SpawnOnPlayer(Main.myPlayer, ModContent.NPCType<NPCs.Bosses.Death>()); //placeholder
                return true;
            }
            else {
                return false;
            }
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofNight, 7);
            recipe.AddIngredient(ItemID.Bone, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 500);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}