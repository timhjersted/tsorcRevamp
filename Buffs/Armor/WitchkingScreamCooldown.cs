using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs.Armor
{
    public class WitchkingScreamCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;

        public override void LastTickSoundsSettings(out float soundVolume)
        {
            PlaysVanillaSound = true;
            VanillaSoundID = SoundID.Zombie83;
            soundVolume = 2f;
        }
    }
}