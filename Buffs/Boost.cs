using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class Boost : ModBuff {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Boost");
            Description.SetDefault("Increased critical strike chance");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.GetCritChance(DamageClass.Magic) += 5;
            player.GetCritChance(DamageClass.Melee) += 5;
            player.GetCritChance(DamageClass.Ranged) += 5;
        }
    }
}
