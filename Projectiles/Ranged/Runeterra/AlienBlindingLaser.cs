using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
    public class AlienBlindingLaser : RuneterraBlindingLaser
    {
        public override int CooldownType => ModContent.BuffType<AlienBlindingLaserCooldown>();
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Ranged/AlienGun/";
        public override int DebuffType => ModContent.BuffType<ElectrifiedDebuff>();
    }
}