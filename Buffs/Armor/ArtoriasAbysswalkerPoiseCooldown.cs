using Terraria.Audio;
using Terraria.ID;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs.Armor
{
    public class ArtoriasAbysswalkerPoiseCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;

        public override void LastTickSoundsSettings(out float soundVolume)
        {
            PlaysVanillaSound = true;
            VanillaSoundID = SoundID.Item8;
            soundVolume = 0.7f;
        }
    }
}