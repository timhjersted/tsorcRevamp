using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Sounds.SteelTempest;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Buffs
{
    public class SteelTempestThrustCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(SteelTempestSound)) + "_ThrustReady";
            soundVolume = 0.5f;
        }
    }
}
