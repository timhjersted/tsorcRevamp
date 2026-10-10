using Microsoft.Xna.Framework;
using Terraria.ID;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Sounds.OrbOfDeception;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Projectiles
{

    public class ThrownOrbOfFlame : ThrownRuneterraOrb
    {
        public override int Width => 64;
        public override int Height => 54;
        public override int FrameCount => 8;
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OrbOfDeceptionSound)) + "_"; //there are no flame orb sounds unfortunately
        public override int NotFilledDustID => DustID.Torch;
        public override int FilledDustID => DustID.RedTorch;
        public override int Tier => 2;
        public override Color NotFilledColor => Color.Firebrick;
    }
}