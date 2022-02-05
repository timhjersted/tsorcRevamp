using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class DemonDrug : ModBuff {
        public override void SetDefaults() {
            DisplayName.SetDefault("Demon Drug");
            Description.SetDefault("Damage is increased by 20%, defense is lowered by 20");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.allDamage += 0.2f;
            player.statDefense -= 20;
        }
    }
}
