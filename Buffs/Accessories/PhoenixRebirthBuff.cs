using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Accessories
{
    public class PhoenixRebirthBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
