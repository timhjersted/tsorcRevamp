using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Sounds.Nightbringer;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Yasuo.Buffs
{
    public class NightbringerDashCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(NightbringerSound)) + "_DashReady";
            soundVolume = 2f;
        }
    }
}
