using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra
{
    class NightbringerDashCooldown : ModBuff
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dashing Cooldown");
            // Description.SetDefault("You can't dash until this runs out");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
