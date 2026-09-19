using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Buffs.Runeterra.Ranged
{
    public class ScoutsBoostCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = "Sounds/Runeterra/Ranged/ToxicShot/BuffRegained";
            soundVolume = 1.6f;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            base.Update(player, ref buffIndex);
            if (player.HasBuff(ModContent.BuffType<ScoutsBoost2>()))
            {
                player.DelBuff(buffIndex);
            }
        }
    }
}
