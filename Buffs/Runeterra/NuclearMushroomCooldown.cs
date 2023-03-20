using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs.Runeterra
{
    class NuclearMushroomCooldown : ModBuff
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Nuclear Mushroom Cooldown");
            Description.SetDefault("You can't deploy another Nuclear Mushroom until this runs out");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }
}
