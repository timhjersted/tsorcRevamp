using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Hostile crossbow bolt fired by invader NPCs.  Shares the BoltProjectile sprite but is
    /// hostile=true so it damages the player rather than enemies.
    /// Behaviour mirrors the friendly BoltProjectile: straight flight, tileCollide, dig sound on hit.
    /// </summary>
    public class InvaderCrossbowBolt : ModProjectile
    {
        // Reuse the existing bolt sprite — no separate .png file needed.
        public override string Texture => "tsorcRevamp/Projectiles/Ranged/Ammo/BoltProjectile";

        public override void SetDefaults()
        {
            // No aiStyle — we provide a custom AI() that halves vanilla arrow gravity
            // so the bolt travels roughly twice as far before significant arc appears.
            Projectile.friendly    = false;
            Projectile.hostile     = true;
            Projectile.width       = 10;
            Projectile.height      = 20;
            Projectile.scale       = 1.1f;
            Projectile.penetrate   = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft    = 300;
        }

        public override void AI()
        {
            // Vanilla wooden-arrow AI uses gravity += 0.1f per tick and adds PiOver2 to the rotation
            // because bolt sprites are drawn pointing UP at rotation 0.
            // We use 0.02f gravity (1/5 of vanilla) so the bolt travels much farther before arcing.
            Projectile.velocity.Y += 0.02f;
            if (Projectile.velocity.Y > 16f)
                Projectile.velocity.Y = 16f;

            // Add PiOver2 so the sprite faces the direction of travel.
            // (The BoltProjectile sprite is oriented vertically; without this offset it renders
            // perpendicular to the flight path — i.e. "sideways".)
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity,
                               Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);

            for (int i = 0; i < 8; i++)
            {
                Dust.NewDust(Projectile.position, Projectile.width, Projectile.height,
                             DustID.WoodFurniture,
                             Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f);
            }
        }
    }
}
