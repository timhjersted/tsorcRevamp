using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class GigantAxe : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("An axe used to kill humans.");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Cyan;
            item.damage = 330;
            item.height = 80;
            item.knockBack = 9;
            item.melee = true;
            item.scale = .9f;
            item.useAnimation = item.useTime = 21;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.value = PriceByRarity.Cyan_9;
            item.width = 84;
        }
        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            //todo add mod NPCs to this list
            if (target.type == ModContent.NPCType<NPCs.Bosses.HeroofLumelia>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Warlock>()
                || target.type == ModContent.NPCType<NPCs.Enemies.TibianAmazon>()
                || target.type == ModContent.NPCType<NPCs.Enemies.TibianValkyrie>()
                || target.type == ModContent.NPCType<NPCs.Enemies.ManHunter>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Necromancer>()
                || target.type == ModContent.NPCType<NPCs.Enemies.RedCloudHunter>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Assassin>()
                || target.type == ModContent.NPCType<NPCs.Enemies.BlackKnight>()
                || target.type == ModContent.NPCType<NPCs.Enemies.Dunlending>()) { 
                damage *= 2;
            }
        }
    }
}
