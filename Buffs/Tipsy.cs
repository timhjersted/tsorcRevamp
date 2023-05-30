using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Buffs
{
    public class Tipsy : GlobalBuff
    {
        public override void Update(int type, Player player, ref int buffIndex)
        {
            if (type == BuffID.Tipsy)
            {
                player.GetDamage(DamageClass.SummonMeleeSpeed) += 0.1f;
            }
        }

        public override void ModifyBuffText(int type, ref string buffName, ref string tip, ref int rare)
        {
            if (type == BuffID.Tipsy)
            {
                tip += "\n" + LanguageUtils.GetTextValue("CommonItemTooltip.IncreasedWhipDamage", 10);
            }
        }
    }
}
