using Microsoft.Xna.Framework;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public class ThrownOrbOfDeception : ThrownRuneterraOrb
    {
        public override int Width => 50;
        public override int Height => 50;
        public override int FrameCount => 4;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Magic/OrbOfDeception/";
        public override int NotFilledDustID => DustID.MagicMirror;
        public override int FilledDustID => DustID.PoisonStaff;
        public override int Tier => 1;
        public override Color NotFilledColor => Color.LightSteelBlue;
    }
}