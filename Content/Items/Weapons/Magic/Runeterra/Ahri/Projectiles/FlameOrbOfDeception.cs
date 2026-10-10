using Microsoft.Xna.Framework;
using Terraria.ID;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Sounds.OrbOfDeception;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Ahri.Projectiles
{
    public class FlameOrbOfDeception : FlameRuneterraOrb
    {
        public override int MaxDetectRadius => 350;
        public override int ProjectileSpeed => 5;
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OrbOfDeceptionSound)) + "_";
        public override Color LightColor => Color.LightSteelBlue;
        public override int dustID => DustID.BlueTorch;
    }
}