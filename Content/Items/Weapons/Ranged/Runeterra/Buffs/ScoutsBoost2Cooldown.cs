using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.ToxicShot;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs
{
    public class ScoutsBoost2Cooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(ToxicShotSound)) + "_SuperBuffReady";
            soundVolume = 1.4f;
        }
    }
}
