using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class Shockwave : ModBuff {
        public override void SetDefaults() {
            DisplayName.SetDefault("Shockwave");
            Description.SetDefault(!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode ? "Release a damaging shockwave when you land while holding DOWN." : "Enemies take damage when you land.");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.GetModPlayer<tsorcRevampPlayer>().Shockwave = true;
		}
    }
}
