using Microsoft.Xna.Framework;
using Terraria.ID;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Sounds.OrbOfDeception;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Projectiles
{
    public class FlameOrbOfFlame : FlameRuneterraOrb
    {
        public override int MaxDetectRadius => 500;
        public override int ProjectileSpeed => 6;
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OrbOfDeceptionSound)) + "_"; //no flame orb sounds unfortunately
        public override Color LightColor => Color.Firebrick;
        public override int dustID => DustID.Torch;
    }
}