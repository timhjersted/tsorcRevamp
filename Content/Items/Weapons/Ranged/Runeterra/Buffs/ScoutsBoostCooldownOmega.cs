using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.OmegaSquadRifle;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs
{
    public class ScoutsBoostCooldownOmega : CooldownDebuff
    {
        public override bool PlaysSoundOnLastTick => true;
        public override void LastTickSoundsSettings(out float soundVolume)
        {
            LastTickSoundPath = UsefulFunctions.RefactorableFilepath(typeof(OmegaSquadRifleSound)) + "_BuffRegained";
            soundVolume = 1f;
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
