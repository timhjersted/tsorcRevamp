using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class TurboboostUniversalCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = "Sounds/Runeterra/Summon/CenterOfTheUniverse/BoostActivation";
            soundVolume = CenterOfTheUniverse.SoundVolume * 2f;
        }
    }
}
