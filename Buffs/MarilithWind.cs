using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class MarilithWind : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
