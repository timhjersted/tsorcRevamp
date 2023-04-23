using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class AmmoReservation : GlobalBuff
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
                tip = "Increases ranged critical strike damage by 10% and reduces ammo consumption by 20%";
            }
        }

    }
}
