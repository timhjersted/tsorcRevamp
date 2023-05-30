using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class FasterThanSight : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
        }

        // Gives infinite flight while active
        // Will be applied by getting near Cataluminance's dash trails or the trails from its stars, giving the player a way to extend their flight
        public override void Update(Player player, ref int buffIndex)
        {
            player.jumpSpeedBoost = 3.2f;
            player.wingTime = 60;
            player.rocketTime = 300;
        }
    }
}
