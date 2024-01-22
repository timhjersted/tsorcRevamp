using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs.Accessories
{
    public class BarrierCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void CustomSetStaticDefaults()
        {
            PlaysVanillaSound = true;
            VanillaSoundID = SoundID.Item4;
            LastTickSoundVolume = 2f;
        }
        public override void PlayerCustomUpdate(Player player, ref int buffIndex)
        {
            player.buffTime[buffIndex]++;
        }
    }
}
