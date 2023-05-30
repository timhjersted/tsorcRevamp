using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class FadingShadow : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
