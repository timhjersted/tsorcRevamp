using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Items;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Sounds.OrbOfFlame;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Buffs
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
