using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    class BarrowBlade : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Wrought with spells of a fierce power." +
                                "\nDispels the defensive shields of Artorias and the Witchking");
        }
        public override void SetDefaults() {
            item.rare = ItemRarityID.Green;
            item.damage = 26;
            item.height = 32;
            item.knockBack = 5;
            item.maxStack = 1;
            item.melee = true;
            item.scale = .9f;
            item.useAnimation = 21;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 140000;
            item.width = 32;
        }

        public override void ModifyHitNPC(Player player, NPC target, ref int damage, ref float knockBack, ref bool crit) {
            if (target.type == ModContent.NPCType<NPCs.Bosses.Artorias>() || target.type == ModContent.NPCType<NPCs.Bosses.Witchking>()) {
                target.AddBuff(ModContent.BuffType<Buffs.DispelShadow>(), 36000);
            }
        }
    }
}
