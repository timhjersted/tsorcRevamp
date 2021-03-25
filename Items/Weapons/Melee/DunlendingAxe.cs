using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class DunlendingAxe : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A cruel hill-man's axe fashioned to kill men.");
        }
        public override void SetDefaults() {
            item.damage = 9;
            item.width = 48;
            item.height = 44;
            item.knockBack = 5;
            item.melee = true;
            item.useAnimation = 24;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 3500;
        }

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            //todo add mod NPCs to this list
            if (target.type == NPCID.BoundWizard //placeholder
                //|| target.type == ModContent.NPCType<HeroOfLumelia>()
                //|| target.type == ModContent.NPCType<Warlock>()
                //|| target.type == ModContent.NPCType<TibianAmazon>()
                //|| target.type == ModContent.NPCType<TibianValkyrie>()
                //|| target.type == ModContent.NPCType<ManHunter>()
                //|| target.type == ModContent.NPCType<Dunlending>()
                //|| target.type == ModContent.NPCType<Necromancer>()
                //|| target.type == ModContent.NPCType<RedCloudAssassin>()
                ) {
                damage *= 2;
            }
            /*if (target.type == ModContent.NPCType<BlackKnight>()) {
                damage *= 6;
            }*/
            /*if (target.type == ModContent.NPCType<Dunlending>()) {
                damage *= 4;
            }*/
        }

    }
}
