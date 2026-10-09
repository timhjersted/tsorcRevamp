using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Sounds.OrbOfFlame;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Buffs
{
    public class OrbOfFlameFireballCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(OrbOfFlameSound)) + "_CharmReady";
            soundVolume = OrbOfDeception.OrbSoundVolume * 2;
        }
    }
}
