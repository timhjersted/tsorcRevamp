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

            float groundY = FindGroundY(Projectile.Center.X, Projectile.Center.Y);
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
            int tileX = Utils.Clamp((int)(worldX / 16f), 1, Main.maxTilesX - 2);
            int centerTileY = Utils.Clamp((int)(aroundY / 16f), 5, Main.maxTilesY - 10);
            for (int tileY = centerTileY - 5; tileY <= centerTileY + 10; tileY++)
            {
                if (WorldGen.SolidTile(tileX, tileY))
                    return tileY * 16f;
            }
            return aroundY;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
