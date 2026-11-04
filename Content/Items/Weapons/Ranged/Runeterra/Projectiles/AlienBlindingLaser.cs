using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.AlienGun;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Projectiles
{
    public class AlienBlindingLaser : RuneterraBlindingLaser
    {
        public override int CooldownType => ModContent.BuffType<AlienBlindingLaserCooldown>();
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(AlienGunSound));
        public override int DebuffType => ModContent.BuffType<ElectrifiedDebuff>();
    }
}