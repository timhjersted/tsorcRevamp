using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class CelestriadManaSickness : GlobalBuff
    {
        public int sicknessTime = 0;
        public override bool ReApply(int type, Player player, int time, int buffIndex) //currently doesn't work at all
        {
            if (type == BuffID.ManaSickness & player.GetModPlayer<tsorcRevampPlayer>().Celestriad)
            {
                time += 300;
                sicknessTime = time / 12;
                player.endurance -= sicknessTime;
                return true;
            } else
            return false;
        }

        public override void ModifyBuffTip(int type, ref string tip, ref int rare)
        {
            if (type == BuffID.ManaSickness && sicknessTime != 0) //This sets the tooltip to be always the same, that won't work
            {
                tip = "Damage taken increased by " + sicknessTime + "%";
            } else if (type == BuffID.ManaSickness)
            {
            }
        }

    }
}