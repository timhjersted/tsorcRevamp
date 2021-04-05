using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class GigantAxe : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("An axe used to kill humans.");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Green;
            item.damage = 50;
            item.height = 80;
            item.knockBack = 9;
            item.melee = true;
            item.scale = .9f;
            item.useAnimation = 27;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 45000;
            item.width = 84;
        }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            //todo add mod NPCs to this list
            if (target.type == NPCID.BoundWizard //placeholder
                //|| target.type == ModContent.NPCType<HeroOfLumelia>()
                //|| target.type == ModContent.NPCType<Warlock>()
                //|| target.type == ModContent.NPCType<BlackKnight>()
                //|| target.type == ModContent.NPCType<TibianAmazon>()
                //|| target.type == ModContent.NPCType<TibianValkyrie>()
                //|| target.type == ModContent.NPCType<ManHunter>()
                //|| target.type == ModContent.NPCType<RedCloudHunter>()
                //|| target.type == ModContent.NPCType<RedCloudAssassin>()
                //|| target.type == ModContent.NPCType<Dunlending>()
                ) {
                damage *= 2;
            }
        }
    }
}
