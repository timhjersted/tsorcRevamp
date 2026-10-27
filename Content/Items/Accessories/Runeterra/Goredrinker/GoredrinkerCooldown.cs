using Terraria;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Items.Accessories.Runeterra.Goredrinker
{
    public class GoredrinkerCooldown : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(GoredrinkerItem)) + "_Hit";
            soundVolume = .35f;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            base.Update(player, ref buffIndex);
            player.GetModPlayer<GoredrinkerPlayer>().Hits = 0;
        }
    }
}