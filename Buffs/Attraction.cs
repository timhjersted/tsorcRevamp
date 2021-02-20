using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class Attraction : ModBuff {
        public override void SetDefaults() {
            DisplayName.SetDefault("Attraction");
            Description.SetDefault("You've summoned a blood moon");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            Main.bloodMoon = true;
            player.enemySpawns = true;
        }
    }
}
