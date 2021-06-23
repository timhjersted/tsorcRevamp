using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Items.BossItems {
    class BlightStone : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons The Blight, one of six guardians of The Abyss." +
                                "\nYou must fight this battle on the surface." +
                                "\nThe Blight cannot be fought with the Covenant of Artorias ring equipped.");
        }
        public override void SetDefaults() {
            item.width = 30;
            item.height = 30;
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
            if (player.ZoneOverworldHeight && !Main.bloodMoon && !NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Blight>()))
            {
                NPC.SpawnOnPlayer(Main.myPlayer, ModContent.NPCType<NPCs.Bosses.Blight>());
                Main.NewText("\"You will be destroyed\"", 255, 50, 50);
                return true;
            }
            else {
                return false;
            }
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.StoneBlock, 1);
            recipe.AddIngredient(mod.GetItem("Humanity"), 15);
            recipe.AddIngredient(mod.GetItem("CursedSoul"), 50);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe.AddIngredient(mod.GetItem("SoulOfAttraidies"));
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}