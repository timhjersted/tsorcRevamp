using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.OmegaSquadRifle;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Projectiles
{
    public class RadioactiveBlindingLaser : RuneterraBlindingLaser
    {
        public override int CooldownType => ModContent.BuffType<RadioactiveBlindingLaserCooldown>();
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OmegaSquadRifleSound));
        public override int DebuffType => ModContent.BuffType<IrradiatedDebuff>();
    }
}