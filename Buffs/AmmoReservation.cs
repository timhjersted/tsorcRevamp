using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs
{
    public class AmmoReservation : GlobalBuff
    {
        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (type == BuffID.AmmoReservation)
            {
                player.GetModPlayer<tsorcRevampPlayer>().AmmoReservationPotion = true;
            }
        }

        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.AmmoReservation)
            {
                tip += "\n" + LanguageUtils.GetTextValue("CommonItemTooltip.IncreasedRangedCriticalDamage", tsorcRevampPlayer.AmmoReservationRangedCritDamage);
            }
        }
    }
}
