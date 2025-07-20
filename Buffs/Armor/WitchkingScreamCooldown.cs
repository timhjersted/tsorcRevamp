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

        public override void CustomSetStaticDefaults()
        {
            PlaysVanillaSound = true;
            VanillaSoundID = SoundID.Zombie83;
            LastTickSoundVolume = 2f;
        }
    }
}