using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs.Armor
{
    public class ShunpoBlinkCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void CustomSetStaticDefaults()
        {
            LastTickSoundPath = "Runeterra/ShunpoReady";
            LastTickSoundVolume = 2f;
        }
    }
}
