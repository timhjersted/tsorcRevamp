using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class DemonDrug : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetDamage(DamageClass.Generic) *= 1.22f;
            player.statDefense -= 15;
        }
    }
}
