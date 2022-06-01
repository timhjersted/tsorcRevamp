using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.BossItems {
    class BloodySkull : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A powerful weapon which destroys all enemies when used.");
        }
        public override void SetDefaults() {
            Item.width = 18;
            Item.height = 18;
            Item.consumable = false;
            Item.maxStack = 1;
            Item.value = 100000;
            Item.rare = ItemRarityID.Pink;
            Item.useTime = 45;
            Item.useAnimation = 45;
            Item.scale = 1f;
            Item.useStyle = ItemUseStyleID.HoldUp;
        }
        public override bool? UseItem(Player player) {
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
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofNight, 7);
            recipe.AddIngredient(ItemID.Bone, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 500);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}