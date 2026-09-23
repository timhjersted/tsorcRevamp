using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Chaos
{
    ///<summary>
    ///Cataclysm Dive's ground wave: dark fire that crawls along the floor away from Chaos's impact point.
    ///Invisible projectile sold entirely by dust geysers, ported from GigasShockwave with a purple palette
    ///and a longer run (the Chaos arena is wider). ai[0] = direction (-1/1), ai[1] = arming delay in ticks,
    ///so the landing dust ring shows the radius before anything can hurt the player.
    ///Hugs the terrain (steps up/down small ledges); dies against walls taller than 3 tiles.
    ///</summary>
    class ChaosShockwave : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Projectiles.InvisibleNothingProj));

        const float WaveSpeed = 7f;
        const int WaveTravelTicks = 60; //~26 tiles of travel after arming — the arena is wide
        const int CloudInterval = 6;    //~10 trailing cloud gores per wave over those 60 ticks

        int Direction => (int)Projectile.ai[0] >= 0 ? 1 : -1;
        int ArmDelay => (int)Projectile.ai[1];
        bool Armed => Projectile.localAI[0] > ArmDelay;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
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
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Shadowflame, 0f, -1f, 100, default, 1.1f);
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

            //The wave itself: dark fire geysers erupting from the ground as it passes
            for (int i = 0; i < 3; i++)
            {
                Vector2 dustPosition = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Projectile.height - 6f);
                int dust = Dust.NewDust(dustPosition, 4, 4, DustID.Shadowflame, Direction * 0.5f, Main.rand.NextFloat(-6f, -3f), 80, default, Main.rand.NextFloat(1.4f, 2f));
                Main.dust[dust].noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                int ember = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.DemonTorch, 0f, -2f, 0, default, 1f);
                Main.dust[ember].noGravity = true;
            }

            //A cloud every few ticks as the wave crawls, so roughly ten per side trail the dust rather than all
            //appearing at the impact point. Client-side only — gores are pure decoration and never sync.
            if (!Main.dedServ && Projectile.localAI[0] % CloudInterval == 0)
            {
                int variant = Main.rand.Next(1, 4);
                Vector2 cloudVelocity = new Vector2(Direction * Main.rand.NextFloat(0.5f, 2f), Main.rand.NextFloat(-3f, -1f));

                Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.Center + new Vector2(0f, -6f), cloudVelocity,
                    ModContent.Find<ModGore>($"tsorcRevamp/ChaosImpactCloud{variant}").Type, Main.rand.NextFloat(0.7f, 1.1f));
            }

            Lighting.AddLight(Projectile.Center, 0.5f, 0.15f, 0.7f);
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
