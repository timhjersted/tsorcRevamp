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

        public override void Update(Player player, ref int buffIndex)
        {
            player.statDefense += 17;
            player.allDamage += 0.3f;
            player.magicCrit += 6;
            player.meleeCrit += 6;
            player.rangedCrit += 6;
            player.meleeSpeed += 0.2f;
            player.pickSpeed += 0.2f;
            player.thorns += 2f;
            player.enemySpawns = true;
        }
    }
}
