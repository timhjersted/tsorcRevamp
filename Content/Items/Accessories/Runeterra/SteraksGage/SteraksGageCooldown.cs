using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.SteraksGage
{
    public class SteraksGageCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(SteraksGageCooldown)) + "_Ready";
            soundVolume = 1.2f;
        }
    }
}
