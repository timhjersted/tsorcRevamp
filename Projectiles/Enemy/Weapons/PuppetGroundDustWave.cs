using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>Non-damaging landing echo that walks fourteen tiles along the surface and emits
    /// successive dirt bursts. Keeping it visual preserves the axe as the honest damage source.</summary>
    public class PuppetGroundDustWave : ModProjectile
    {
        private const float MaxTravel = 14f * 16f;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 36;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.localAI[0] += System.Math.Abs(Projectile.velocity.X);
            if (Projectile.localAI[0] > MaxTravel)
            {
                Projectile.Kill();
                return;
            }

            if (!TryFindGroundY(Projectile.Center.X, Projectile.Center.Y, out float groundY))
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = new Vector2(Projectile.Center.X, groundY - 2f);

            if (!Main.dedServ)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 velocity = new Vector2(
                        Main.rand.NextFloat(-1.2f, 1.2f),
                        Main.rand.NextFloat(-3.8f, -1.1f));
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + new Vector2(Main.rand.NextFloat(-5f, 5f), 0f),
                        DustID.Dirt,
                        velocity,
                        80,
                        default,
                        Main.rand.NextFloat(0.8f, 1.25f));
                    dust.noGravity = false;
                }
            }
        }

        internal static float FindGroundY(float worldX, float aroundY)
        {
            return TryFindGroundY(worldX, aroundY, out float groundY) ? groundY : aroundY;
        }

        /// <summary>Finds an exposed solid-tile surface near the supplied height. Returning false
        /// is significant: ground-bound effects must stop at unsupported gaps instead of retaining
        /// their old Y and appearing to travel through open air.</summary>
        internal static bool TryFindGroundY(float worldX, float aroundY, out float groundY)
        {
            int tileX = Utils.Clamp((int)(worldX / 16f), 1, Main.maxTilesX - 2);
            int centerTileY = Utils.Clamp((int)(aroundY / 16f), 5, Main.maxTilesY - 10);
            for (int distance = 0; distance <= 14; distance++)
            {
                if (distance <= 8)
                {
                    int upwardTileY = centerTileY - distance;
                    if (WorldGen.SolidTile(tileX, upwardTileY)
                        && !WorldGen.SolidTile(tileX, upwardTileY - 1))
                    {
                        groundY = upwardTileY * 16f;
                        return true;
                    }
                }

                if (distance > 0)
                {
                    int downwardTileY = centerTileY + distance;
                    if (WorldGen.SolidTile(tileX, downwardTileY)
                        && !WorldGen.SolidTile(tileX, downwardTileY - 1))
                    {
                        groundY = downwardTileY * 16f;
                        return true;
                    }
                }
            }

            groundY = aroundY;
            return false;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
