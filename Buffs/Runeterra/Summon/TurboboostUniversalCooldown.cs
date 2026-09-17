using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class TurboboostUniversalCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void CustomSetStaticDefaults()
        {
            LastTickSoundPath = "Runeterra/Summon/CenterOfTheUniverse/BoostActivation";
            LastTickSoundVolume = CenterOfTheUniverse.SoundVolume * 2f;
        }
    }
}
