using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Weapons.Melee
{
    public class LaevateinnInvincibleCooldown : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
