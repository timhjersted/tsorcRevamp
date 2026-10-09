using Microsoft.Xna.Framework;
using Terraria.ID;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Sounds.OrbOfSpirituality;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Projectiles
{

    public class ThrownOrbOfSpirituality : ThrownRuneterraOrb
    {
        public override int Width => 106;
        public override int Height => 62;
        public override int FrameCount => 8;
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OrbOfSpiritualitySound)) + "_";
        public override int NotFilledDustID => DustID.VenomStaff;
        public override int FilledDustID => DustID.PoisonStaff;
        public override int Tier => 3;
        public override Color NotFilledColor => Color.Pink;
    }
}