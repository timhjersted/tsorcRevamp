using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class CorruptedTooth : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A green ooze dribbles from the tooth, which deals" +
                                "\nextra damage to enemies of a similar nature." +
                                "\nHas a chance to heal the player on hit.");
        }
        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 21;
            item.autoReuse = true;
            item.useTime = 21;
            item.damage = 11;
            item.knockBack = 4f;
            item.UseSound = SoundID.Item1;
            item.value = 21000;
            item.melee = true;
        }
        public override void OnHitNPC(Player player, NPC target, int damage, float knockBack, bool crit) {
            if (target.type == NPCID.EaterofSouls
                || target.type == NPCID.BigEater
                || target.type == NPCID.LittleEater
                || target.type == NPCID.EaterofWorldsHead
                || target.type == NPCID.EaterofWorldsBody
                || target.type == NPCID.EaterofWorldsTail
                || target.type == ModContent.NPCType<NPCs.Enemies.GuardianCorruptor>() //please dont use this weapon on a guardian corruptor...
                ) {
                damage *= 4;
            }
            if (Main.rand.Next(10) == 0) {
                player.statLife += damage;
                player.HealEffect(damage);
            }
        }
    }
}
