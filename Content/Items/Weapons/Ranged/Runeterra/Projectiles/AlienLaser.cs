using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Buffs;
using tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Sounds.AlienGun;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Runeterra.Projectiles
{
    public class AlienLaser : RuneterraDartsProjectile
    {
        public override int ExtraUpdates => 2;
        public override int DebuffType => ModContent.BuffType<ElectrifiedDebuff>();
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(AlienGunSound));
        public override void CustomSetDefaults()
        {
            Projectile.light = 0.25f;
        }
    }
}