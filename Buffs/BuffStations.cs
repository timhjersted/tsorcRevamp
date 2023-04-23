using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class BuffStations : GlobalBuff
    {

        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (type == BuffID.Sharpened)
            {
                player.GetArmorPenetration(DamageClass.Melee) -= 12f;
                player.GetModPlayer<tsorcRevampPlayer>().Sharpened = true;
            }
            if (type == BuffID.AmmoBox)
            {
                player.GetModPlayer<tsorcRevampPlayer>().AmmoBox = true;
            }
        }

        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.Sharpened)
            {
                tip = "Increases melee armor penetration by 50%";
            }
            if (type == BuffID.AmmoBox)
            {
                tip = "Increases ammo crit chance by the ammo's base damage and reduces ammo consumption by 20%";
            }
        }

    }
}
