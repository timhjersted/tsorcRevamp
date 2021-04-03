using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class ForgottenRuneAxe : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("4 times as effective against magic users.");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Pink;
            item.damage = 105;
            item.height = 46;
            item.knockBack = 5;
            item.autoReuse = true;
            item.melee = true;
            item.scale = 1.1f;
            item.useAnimation = 23;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 1800000;
            item.width = 56;
        }

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            //todo add mod NPCs to this list
            if (target.type == NPCID.Tim
                || target.type == NPCID.DarkCaster
                || target.type == NPCID.GoblinSorcerer
                //|| target.type == ModContent.NPCType<UndeadCaster>()
                //|| target.type == ModContent.NPCType<MindflayerServant>()
                //|| target.type == ModContent.NPCType<DungeonMage>()
                //|| target.type == ModContent.NPCType<DemonSpirit>()
                //|| target.type == ModContent.NPCType<CrazedDemonSpirit>()
                //|| target.type == ModContent.NPCType<ShadowMage>()
                //|| target.type == ModContent.NPCType<AttraidiesIllusion>()
                //|| target.type == ModContent.NPCType<AttraidiesManifestation>()
                //|| target.type == ModContent.NPCType<MindflayerKing>()
                //|| target.type == ModContent.NPCType<DarkShogunMask>()
                //|| target.type == ModContent.NPCType<DarkDragonMask>()
                //|| target.type == ModContent.NPCType<BrokenOkiku>()
                //|| target.type == ModContent.NPCType<Okiku>()
                //|| target.type == ModContent.NPCType<WyvernMage>()
                //|| target.type == ModContent.NPCType<GhostOfTheForgottenKnight>()
                //|| target.type == ModContent.NPCType<GhostOfTheForgottenWarrior>()
                //|| target.type == ModContent.NPCType<BarrowWight>()
                ) {
                damage *= 4;
            }
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldAxe"), 1);
            recipe.AddIngredient(mod.GetItem("GuardianSoul"), 3);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 115000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}
