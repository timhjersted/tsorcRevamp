using Microsoft.Xna.Framework;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{

    public class ThrownOrbOfSpirituality : ThrownRuneterraOrb
    {
        public override int Width => 106;
        public override int Height => 62;
        public override int FrameCount => 8;
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Magic/OrbOfSpirituality/"; //there are no flame orb sounds unfortunately
        public override int NotFilledDustID => DustID.VenomStaff;
        public override int FilledDustID => DustID.PoisonStaff;
        public override int Tier => 3;
        public override Color NotFilledColor => Color.Pink;
    }
}