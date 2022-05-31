using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class Invincible : ModBuff {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Invincible");
            Description.SetDefault("You are invincible!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.immune = true;
        }
    }
}
