using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas slam shockwave: a golden wave that crawls along the ground away from the impact point.
    ///Invisible projectile sold entirely by dust geysers. ai[0] = direction (-1/1), ai[1] = arming delay
    ///in ticks — the landing dust ring telegraphs the radius first, then the damaging wave follows it.
    ///Hugs the terrain (steps up/down small ledges); dies against walls taller than 3 tiles.
    ///</summary>
    class GigasShockwave : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float WaveSpeed = 7f;
        const int WaveTravelTicks = 32; //~14 tiles of travel after arming

        int Direction => (int)Projectile.ai[0] >= 0 ? 1 : -1;
        int ArmDelay => (int)Projectile.ai[1];
        bool Armed => Projectile.localAI[0] > ArmDelay;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 30;
            Projectile.height = 42;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300; //real lifetime set on spawn from the arm delay
            Projectile.light = 0.4f;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = ArmDelay + WaveTravelTicks;
        }

        public override bool? CanDamage()
        {
            return Armed;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity = Vector2.Zero;

            if (!Armed)
            {
                //Simmer in place while the landing dust ring telegraphs the radius
                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, 0f, -1f, 100, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                }
                return;
            }

            //Sweep outward, snapping to the ground surface
            Projectile.position.X += Direction * WaveSpeed;
            if (!SnapToGround())
            {
                Projectile.Kill();
                return;
            }

            //The wave itself: dense golden geysers erupting from the ground as it passes
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Projectile.height - 6f);
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, Direction * 0.5f, Main.rand.NextFloat(-6f, -3f), 80, default, Main.rand.NextFloat(1.4f, 2f));
                Main.dust[dust].noGravity = true;
            }
            if (Main.rand.NextBool(2))
            {
                int sparkle = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, -2f, 0, default, 1f);
                Main.dust[sparkle].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.7f, 0.6f, 0.2f);
        }

        ///<summary>Aligns the projectile bottom with the ground below/above; false = blocked by a tall wall or open pit.</summary>
        bool SnapToGround()
        {
            int tileX = (int)((Projectile.Center.X + Direction * Projectile.width / 2f) / 16f);
            int tileY = (int)((Projectile.position.Y + Projectile.height - 8f) / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5 || tileY > Main.maxTilesY - 10)
            {
                return false;
            }

            //Climb: if the row at our feet is solid, look up to 3 tiles for open air
            int climb = 0;
            while (IsSolid(tileX, tileY) && climb <= 3)
            {
                tileY--;
                climb++;
            }
            if (climb > 3)
            {
                return false; //wall taller than the wave
            }
            //Descend: find the first solid tile below (up to 5 tiles; farther = pit, stop)
            int drop = 0;
            while (!IsSolid(tileX, tileY + 1) && drop <= 5)
            {
                tileY++;
                drop++;
            }
            if (drop > 5)
            {
                return false;
            }
            Projectile.position.Y = (tileY + 1) * 16f - Projectile.height;
            return true;
        }

        static bool IsSolid(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            return tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
        }
    }
}
