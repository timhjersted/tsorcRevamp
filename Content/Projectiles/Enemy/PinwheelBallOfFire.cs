using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // Hostile copy of the Flower of Fire ball for Pinwheel's Volcanic Eruption. Vanilla BallofFire is friendly, and flipping
    // hostile/friendly after NewProjectile never reaches MP clients (those flags aren't synced), so it did no damage there.
    public class PinwheelBallOfFire : ModProjectile
    {
        public override string Texture => UsefulFunctions.VanillaTextureFilepath(ProjectileID.BallofFire, "Projectile");

        public override void SetDefaults()
        {
            // aiStyle 8 brings the bounce + gravity-after-20-ticks arc; vanilla's bounce cap is 6 for type 15, 5 for anything else
            Projectile.CloneDefaults(ProjectileID.BallofFire);
            AIType = ProjectileID.BallofFire;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Default;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Vanilla gives BallofFire its burn by type ID, so a clone has to apply it itself: 50% for 5 seconds
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.OnFire, 300);
            }
        }
    }
}
