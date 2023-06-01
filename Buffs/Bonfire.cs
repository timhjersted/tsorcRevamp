using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class Bonfire : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
        }
    }
}
