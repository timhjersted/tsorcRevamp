using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Items;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Sounds.OrbOfSpirituality;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Buffs
{
    public class OrbOfSpiritualityCharmCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(OrbOfSpiritualitySound)) + "_CharmReady";
            soundVolume = OrbOfDeception.OrbSoundVolume * 2;
        }
    }
}
