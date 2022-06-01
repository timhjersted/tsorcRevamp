using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class DyingWindCrystal : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The fading Crystal of Wind. \n" + "Will summon Chaos.");
        }

        public override void SetDefaults() {
            Item.rare = ItemRarityID.LightRed;
            Item.width = 12;
            Item.height = 12;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.maxStack = 1;
            Item.consumable = false;
        }

        public override bool? UseItem(Player player) {
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>());
            return true;
        }
        public override bool CanUseItem(Player player) {
            return (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>()));
        }

        
        public override void AddRecipes() {

            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(Mod.Find<ModItem>("DyingWindShard").Type, 100);
                recipe.AddIngredient(Mod.Find<ModItem>("RedTitanite").Type, 5);
                recipe.AddIngredient(Mod.Find<ModItem>("WhiteTitanite").Type, 5);
                recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 1000);
                recipe.AddTile(TileID.DemonAltar);
                
                recipe.Register();
            }
        }
    }
}
