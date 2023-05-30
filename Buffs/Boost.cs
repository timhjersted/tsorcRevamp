using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class Boost : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.moveSpeed *= 1.2f;
        }
    }
}
