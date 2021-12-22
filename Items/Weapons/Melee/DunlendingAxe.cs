using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class DunlendingAxe : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A cruel hill-man's axe fashioned to kill men" +
                 "\nDeals massive damage to humans");
        }
        public override void SetDefaults() {
            item.damage = 5;
            item.width = 48;
            item.height = 44;
            item.knockBack = 5;
            item.melee = true;
            item.useAnimation = item.useTime = 11;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = 3500;
        }

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {

            if (target.type == ModContent.NPCType<NPCs.Enemies.HeroofLumelia>() 
                || target.type == ModContent.NPCType<NPCs.Enemies.Warlock>()
                || target.type == ModContent.NPCType<NPCs.Enemies.TibianAmazon>()
                || target.type == ModContent.NPCType<NPCs.Enemies.TibianValkyrie>()
                || target.type == ModContent.NPCType<NPCs.Enemies.ManHunter>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Necromancer>()
                || target.type == ModContent.NPCType<NPCs.Enemies.RedCloudHunter>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Assassin>()
                ) {
                damage *= 4; // *2 > *4, lets make it actually useful shall we
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.BlackKnight>()) {
                damage *= 6;
            }
            if (target.type == ModContent.NPCType<NPCs.Enemies.Dunlending>()) {
                damage *= 4;
            }
        }

    }
}
