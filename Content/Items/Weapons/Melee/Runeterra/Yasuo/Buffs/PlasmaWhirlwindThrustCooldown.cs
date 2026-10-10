using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Sounds.PlasmaWhirlwind;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Buffs
{
    public class PlasmaWhirlwindThrustCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(PlasmaWhirlwindSound)) + "_ThrustReady";
            soundVolume = 1f;
        }
    }
}
