using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class TomeOfSlograAndGaibon : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Tome of Slogra and Gaibon");
            Tooltip.SetDefault("Summons the nocturnal beast known as Slogra, a reptilian creature who fights alongside the demon \n" +
                "known as Gaibon. Not much is known of Gaibon, though it is legend that Slogra was once a man,  \n" +
                "whose rings he wore finally consumed him until the man was gone and only his lost soul remained");
        }

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.maxStack = 1;
            Item.consumable = false;
            Item.rare = ItemRarityID.LightRed;
            Item.consumable = false;
        }


        public override bool? UseItem(Player player) {
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Slogra>()) || NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Gaibon>()))
                {
                return false;
            }
            if (Main.dayTime) {
                Main.NewText("Slogra only awakens at night.", 175, 75, 255);
            }
            else {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, -1, -1, 0);
                NPC.NewNPC((int)player.position.X + 1000, (int)player.position.Y, ModContent.NPCType<NPCs.Bosses.Gaibon>(), 0);
                NPC.NewNPC((int)player.position.X - 1000, (int)player.position.Y - 200, ModContent.NPCType<NPCs.Bosses.Slogra>(), 0);
            }
            return true;
        }

        public override void AddRecipes() {
            if (!ModContent.GetInstance<tsorcRevampConfig>().AdventureModeItems)
            {
                Recipe recipe = CreateRecipe();
                recipe.AddIngredient(ItemID.SpellTome, 1);
                recipe.AddIngredient(ItemID.ShadowScale, 1);
                recipe.AddIngredient(ItemID.MeteoriteBar, 12);
                recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 100);
                recipe.AddTile(TileID.DemonAltar);
                
                recipe.Register();
            }
        }
    }
}
