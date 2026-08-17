using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class TurboboostCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void CustomSetStaticDefaults()
        {
            LastTickSoundPath = "Runeterra/Summon/InterstellarVessel/BoostActivation";
            LastTickSoundVolume = InterstellarVesselGauntlet.SoundVolume * 2f;
        }
    }
}
