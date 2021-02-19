using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class Strength : ModBuff {
        public override void SetDefaults() {
            DisplayName.SetDefault("Protect");
            Description.SetDefault("Defense is increased by 30!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.statDefense += 15;
            player.allDamage += 0.15f;
            player.meleeSpeed += 0.15f;
            player.pickSpeed += 0.15f;
            player.magicCrit += 2;
            player.meleeCrit += 2;
            player.rangedCrit += 2;
        }
    }
}
