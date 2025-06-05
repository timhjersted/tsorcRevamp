using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs.Weapons.Melee
{
    public class PilgrimSpontoonCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;

        public override void CustomSetStaticDefaults()
        {
            PlaysVanillaSound = true;
            VanillaSoundID = SoundID.Item4;
            LastTickSoundVolume = 2f;
        }
    }
}
