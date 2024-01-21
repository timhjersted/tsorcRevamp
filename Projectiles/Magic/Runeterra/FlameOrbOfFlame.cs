using Microsoft.Xna.Framework;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{
    public class FlameOrbOfFlame : FlameRuneterraOrb
    {
        public override int MaxDetectRadius => 500;
        public override int ProjectileSpeed => 6;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Magic/OrbOfDeception/"; //no flame orb sounds unfortunately
        public override Color LightColor => Color.Firebrick;
        public override int dustID => DustID.Torch;
    }
}