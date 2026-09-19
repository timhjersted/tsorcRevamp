using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class TurboboostCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = "Sounds/Runeterra/Summon/InterstellarVessel/BoostActivation";
            soundVolume = InterstellarVesselGauntlet.SoundVolume * 2f;
        }
    }
}
