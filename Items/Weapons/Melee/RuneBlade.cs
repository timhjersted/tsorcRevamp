using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class RuneBlade : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A sword used to kill magic users." +
                                "\nDoes up to 8x damage to some enemies.");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Pink;
            item.damage = 19;
            item.height = 36;
            item.knockBack = 5;
            item.maxStack = 1;
            item.melee = true;
            item.scale = .9f;
            item.useAnimation = 23;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 140000;
            item.width = 36;
        }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            //todo add mod NPCs to this list
            if (target.type == NPCID.DarkCaster
                || target.type == NPCID.GoblinSorcerer
                //|| target.type == ModContent.NPCType<UndeadCaster>()
                //|| target.type == ModContent.NPCType<MindflayerServant>()
                ) {
                damage *= 8;
            }
            if (target.type == NPCID.Tim
                //|| target.type == ModContent.NPCType<DungeonMage>()
                //|| target.type == ModContent.NPCType<DemonSpirit>()
                //|| target.type == ModContent.NPCType<ShadowMage>()
                //|| target.type == ModContent.NPCType<AttraidiesIllusion>()
                //|| target.type == ModContent.NPCType<AttraidiesManifestation>()
                //|| target.type == ModContent.NPCType<BrokenOkiku>()
                //|| target.type == ModContent.NPCType<WyvernMage>()
                ) {
                damage *= 4;
            }
            if (target.type == NPCID.BoundWizard //placeholder. remove when adding mod NPCs
                                                 //|| target.type == ModContent.NPCType<CrazedDemonSpirit>()
                                                 //|| target.type == ModContent.NPCType<MindflayerKing>()
                                                 //|| target.type == ModContent.NPCType<DarkShogunMask>()
                                                 //|| target.type == ModContent.NPCType<DarkDragonMask>()
                                                 //|| target.type == ModContent.NPCType<Okiku>()
                                                 //|| target.type == ModContent.NPCType<LichKingDisciple>()
                                                 //|| target.type == ModContent.NPCType<Attraidies>()
                ) {
                damage *= 5;
            }
        }
    }
}
