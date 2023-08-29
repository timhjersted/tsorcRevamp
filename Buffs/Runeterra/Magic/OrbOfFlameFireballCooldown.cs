using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra.Magic;

class OrbOfFlameFireballCooldown : ModBuff
{
    public override void SetStaticDefaults()
    {
        DisplayName.SetDefault("Fireball Cooldown");
        Description.SetDefault("You can't cast another Fireball until this runs out");
        Main.debuff[Type] = true;
        Main.buffNoTimeDisplay[Type] = false;
    }
}
