using Microsoft.Xna.Framework;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{
    public class FlameOrbOfDeception : FlameRuneterraOrb
    {
        public override int MaxDetectRadius => 350;
        public override int ProjectileSpeed => 5;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Magic/OrbOfDeception/";
        public override Color LightColor => Color.LightSteelBlue;
        public override int dustID => DustID.BlueTorch;
    }
}