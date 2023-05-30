using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class ThermalRise : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            // Refills wing/rocket boot flight time
            player.wingTime = 60;
            player.rocketTime = 300;
        }
    }
}
