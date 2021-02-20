using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class Battlefront : ModBuff {
        public override void SetDefaults() {
            DisplayName.SetDefault("Battlefront");
            Description.SetDefault("You feel ready for battle");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.statDefense += 8;
            player.allDamage += 0.2f;
            player.magicCrit += 5;
            player.meleeCrit += 5;
            player.rangedCrit += 5;
            player.meleeSpeed += 0.2f;
            player.pickSpeed += 0.2f;
            player.thorns += 1f;
            player.enemySpawns = true;
        }
    }
}
