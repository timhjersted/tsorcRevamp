using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Ranged;

class ScoutsBoost2Cooldown : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Scouts Boost II Cooldown");
        Description.SetDefault("You can't trigger the movement speed bonus again until this runs out");
        Main.debuff[Type] = true;
        Main.buffNoTimeDisplay[Type] = false;
    }
}
