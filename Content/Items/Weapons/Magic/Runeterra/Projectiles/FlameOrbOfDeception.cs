using Microsoft.Xna.Framework;
using Terraria.ID;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Sounds.OrbOfDeception;
using tsorcRevamp.Content.Projectiles.Magic.Runeterra;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Projectiles
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