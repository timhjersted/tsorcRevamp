using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Armor
{
    public class ShunpoDashCooldown : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
