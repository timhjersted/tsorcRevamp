using Microsoft.Xna.Framework;
using Terraria.ID;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Sounds.OrbOfSpirituality;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Projectiles
{
    public class FlameOrbOfSpirituality : FlameRuneterraOrb
    {
        public override int MaxDetectRadius => 650;
        public override int ProjectileSpeed => 7;
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OrbOfSpiritualitySound)) + "_";
        public override Color LightColor => Color.Pink;
        public override int dustID => DustID.VenomStaff;
    }
}