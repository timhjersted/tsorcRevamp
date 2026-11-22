using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Sounds.CenterOfTheUniverse;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Buffs
{
    public class TurboboostUniversalCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(CenterOfTheUniverseSound)) + "_BoostActivation";
            soundVolume = CenterOfTheUniverse.SoundVolume * 2f;
        }
    }
}
