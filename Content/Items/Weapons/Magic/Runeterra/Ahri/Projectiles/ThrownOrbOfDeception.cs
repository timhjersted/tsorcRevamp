using Microsoft.Xna.Framework;
using Terraria.ID;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Sounds.OrbOfDeception;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Projectiles
{

    public class ThrownOrbOfDeception : ThrownRuneterraOrb
    {
        public override int Width => 50;
        public override int Height => 50;
        public override int FrameCount => 4;
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OrbOfDeceptionSound)) + "_";
        public override int NotFilledDustID => DustID.MagicMirror;
        public override int FilledDustID => DustID.PoisonStaff;
        public override int Tier => 1;
        public override Color NotFilledColor => Color.LightSteelBlue;
    }
}