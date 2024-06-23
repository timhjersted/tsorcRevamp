using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Ranged;

namespace tsorcRevamp.Buffs.Runeterra.Summon
{
    public class GoredrinkerCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void CustomSetStaticDefaults()
        {
            LastTickSoundPath = "Runeterra/Summon/GoredrinkerHit";
            LastTickSoundVolume = .35f;
        }
        public override void PlayerCustomUpdate(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().GoredrinkerHits = 0;
        }
    }
}