using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    public class FasterThanSight : ModBuff
    {
        public const float JumpSpeedBoost = 3.2f;

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
        }

        // Gives infinite flight while active. Refills wing/rocket time every tick, so it never runs out.
        // Overwrites any jump boost from buffs that updated earlier this tick.
        // Accessories update after buffs, so their jump boosts are still added on top.
        public override void Update(Player player, ref int buffIndex)
        {
            player.jumpSpeedBoost = JumpSpeedBoost;
            player.wingTime = 60;
            player.rocketTime = 300;
        }
    }
}
