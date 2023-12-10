using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Ranged;

namespace tsorcRevamp.Projectiles.Ranged.Runeterra
{
    public class ToxicShotDart : RuneterraDartsProjectile
    {
        public override int ExtraUpdates => 1;
        public override int DebuffType => ModContent.BuffType<VenomDebuff>();
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Ranged/ToxicShot/";
    }
}