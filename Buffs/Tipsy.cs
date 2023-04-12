using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    class Tipsy : GlobalBuff
    {

        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (type == BuffID.Tipsy
                )
            {
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.1f;
            }
        }

        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.Tipsy)
            {
                tip = "Increases melee, whip damage and speed by 10% at the cost of 4 defense";
            }
        }

    }
}
