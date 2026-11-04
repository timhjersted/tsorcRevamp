using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.AlienGun;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs
{
    public class AlienBlindingLaserCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(AlienGunSound)) + "_BlindingLaserReady";
            soundVolume = 0.8f;
        }
    }
}
