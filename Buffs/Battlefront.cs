using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class Battlefront : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(DamageClass.Generic) += 0.15f;
            player.GetCritChance(DamageClass.Generic) += 15;
            player.thorns += 2f;
            player.enemySpawns = true;
        }
    }
}
