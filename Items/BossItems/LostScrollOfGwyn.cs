using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class LostScrollOfGwyn : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Lost Scroll of Gwyn");
            Tooltip.SetDefault("Used to summon Gwyn, Lord of Cinder. \n" +
                "You must enter The Abyss with the Covenant of Artorias equipped to summon Lord Gwyn.  \n" +
                "Upon the scroll it reads: \"Once living, now Undead, and a fitting heir to father Gwyn thou art, \n" +
                "And beseech thee. Succeed Lord Gwyn, and inheriteth the Fire of our world. \n" +
                "Thou shall endeth this eternal twilight, and avert further Undead sacrifices.\"\n" +
                "Only the true warrior of the age will survive this fight. ");
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
            if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>()))
            {
                return false;
            }
            if (!Main.bloodMoon) {
                Main.NewText("Lord Gwyn ignores your call... you must enter The Abyss to summon the Lord of Cinder!", 175, 75, 255);
            }
            else {
                Main.NewText("Defeat me, and you shall inherit the fire of this world... ", 175, 75, 255);
                NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>());
            }
            return true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<SoulOfBlight>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfChaos>(), 1);
            recipe.AddIngredient(ModContent.ItemType<GhostWyvernSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BequeathedSoul>(), 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
