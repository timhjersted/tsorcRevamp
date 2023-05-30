using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class MarilithHold : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
