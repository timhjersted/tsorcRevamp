using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.OmegaSquadRifle;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs
{
    public class ScoutsBoost2CooldownOmega : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(OmegaSquadRifleSound)) + "_SuperBuffReady";
            soundVolume = 1f;
        }
    }
}
