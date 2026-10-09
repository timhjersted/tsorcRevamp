using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Aatrox.Sounds;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Runeterra.Aatrox.Buffs
{
    public class WorldEnderCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(AatroxSound)) + "_Ready";
            soundVolume = WorldEnderItem.SoundVolume;
        }
    }
}
