using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class EarthAlignment : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        // Constantly sets the players wingtime to 60, making it infinite as long as they have the buff.
        public override void Update(Player player, ref int buffIndex)
        {
            player.wingTime = 200;
        }
    }
}
