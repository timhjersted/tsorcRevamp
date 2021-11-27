using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class BossZenBuff : ModBuff {
        public override void SetDefaults() {
            DisplayName.SetDefault("Boss Zen");
            Description.SetDefault("The active boss is blocking enemy spawns!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.GetModPlayer<tsorcRevampPlayer>().BossZenBuff = true;
        }

    }
}
