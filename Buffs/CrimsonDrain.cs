using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class CrimsonDrain : ModBuff {
        public override void SetDefaults() {
            DisplayName.SetDefault("Crimson Drain");
            Description.SetDefault(!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode ? "Enemies within a ten tile radius receive Crimson Burn." : "Enemies within a ten tile radius taking damage.");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.GetModPlayer<tsorcRevampPlayer>().CrimsonDrain = true;
        }
       
    }
}
