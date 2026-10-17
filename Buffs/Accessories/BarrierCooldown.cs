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
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            PlaysVanillaSound = true;
            VanillaSoundID = SoundID.Item4;
            soundVolume = 2f;
        }
        public override void Update(Player player, ref int buffIndex)
        {
            base.Update(player, ref buffIndex);
            // Keep the timer from running down on its own, but don't revive it once the last-tick sound has played.
            if (player.buffTime[buffIndex] > 0)
            {
                player.buffTime[buffIndex]++;
            }
        }
    }
}
