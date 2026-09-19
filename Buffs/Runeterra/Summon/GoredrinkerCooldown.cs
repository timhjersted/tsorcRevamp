using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Ranged;
using tsorcRevamp.Content.Items.Accessories.Summon.Goredrinker;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class GoredrinkerCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = "Sounds/Runeterra/Summon/GoredrinkerHit";
            soundVolume = .35f;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            base.Update(player, ref buffIndex);
            player.GetModPlayer<GoredrinkerPlayer>().Hits = 0;
        }
    }
}