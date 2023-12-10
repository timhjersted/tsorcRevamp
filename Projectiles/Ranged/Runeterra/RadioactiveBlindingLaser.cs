using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
    public class RadioactiveBlindingLaser : RuneterraBlindingLaser
    {
        public override int CooldownType => ModContent.BuffType<RadioactiveBlindingLaserCooldown>();
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Ranged/OmegaSqaudRifle/";
        public override int DebuffType => ModContent.BuffType<IrradiatedDebuff>();
    }
}