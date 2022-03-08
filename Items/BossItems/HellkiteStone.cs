using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class HellkiteStone : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons a Hellkite Dragon from the sky...");
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
            
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>());
            return true;
        }

        public override bool CanUseItem(Player player)
        {
            if (Main.dayTime)
            {
                Main.NewText("Nothing happens... Retry at night.", 175, 75, 255);
                return false;
            }
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()))
            {
                return false;
            }
            return true;
        }

        public override void AddRecipes() {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            { 
                ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("RedTitanite"), 3);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
            }
        }
    }
}
