using Microsoft.Xna.Framework;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{
    public class FlameOrbOfSpirituality : FlameRuneterraOrb
    {
        public override int MaxDetectRadius => 650;
        public override int ProjectileSpeed => 7;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/";
        public override Color LightColor => Color.Pink;
        public override int dustID => DustID.VenomStaff;
    }
}