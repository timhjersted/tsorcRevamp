using Terraria.Audio;
using Terraria.ID;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs.Armor
{
    public class ArtoriasAbysswalkerPoiseCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;

        public override void CustomSetStaticDefaults()
        {
            PlaysVanillaSound = true;
            VanillaSoundID = SoundID.Item8;
            LastTickSoundVolume = 0.7f;
        }
    }
}