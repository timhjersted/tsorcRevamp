using Microsoft.Xna.Framework;
using Terraria.ID;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Bases;
using tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Sounds.OrbOfSpirituality;
using tsorcRevamp.Content.Projectiles.Magic.Runeterra;

namespace tsorcRevamp.Content.Items.Weapons.Magic.Runeterra.Projectiles
{
    public class FlameSpiritRush : FlameRuneterraOrb
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(FlameOrbOfSpirituality));
        public override int MaxDetectRadius => 650;
        public override int ProjectileSpeed => 7;
        public override string SoundPath => UsefulFunctions.RefactorableFilepath(typeof(OrbOfSpiritualitySound)) + "_";
        public override Color LightColor => Color.Pink;
        public override int dustID => DustID.VenomStaff;
        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.scale = 1.5f;
        }

        public override void AI()
        {
            base.AI();
        }
    }
}