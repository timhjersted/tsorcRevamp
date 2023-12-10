using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
    public class AlienLaser : RuneterraDartsProjectile
    {
        public override int ExtraUpdates => 2;
        public override int DebuffType => ModContent.BuffType<ElectrifiedDebuff>();
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Ranged/AlienGun/";
        public override void CustomSetDefaults()
        {
            Projectile.light = 0.25f;
        }
    }
}