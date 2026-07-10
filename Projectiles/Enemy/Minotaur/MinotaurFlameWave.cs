using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Minotaur Mage's staff-slam wave: a short fire wave crawling along the ground away from the
    ///slam (small cousin of GigasShockwave). ai[0] = direction (-1/1), ai[1] = arming delay ticks.
    ///Hugs the terrain; dies against walls taller than 2 tiles. ~8 tiles of reach.
    ///</summary>
    class MinotaurFlameWave : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float WaveSpeed = 6f;
        const int WaveTravelTicks = 22; //~8 tiles after arming

        int Direction => (int)Projectile.ai[0] >= 0 ? 1 : -1;
        int ArmDelay => (int)Projectile.ai[1];
        bool Armed => Projectile.localAI[0] > ArmDelay;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 24;
            Projectile.height = 32;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 120;
            Projectile.light = 0.35f;
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
                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Torch, 0f, -1f, 100, default, 1.1f);
                    Main.dust[dust].noGravity = true;
                }
                return;
            }

            Projectile.position.X += Direction * WaveSpeed;
            if (!SnapToGround())
            {
                Projectile.Kill();
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Projectile.height - 6f);
                int dust = Dust.NewDust(pos, 4, 4, DustID.Torch, Direction * 0.5f, Main.rand.NextFloat(-4f, -2f), 80, default, Main.rand.NextFloat(1.3f, 1.8f));
                Main.dust[dust].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.5f, 0.3f, 0.1f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 2 * 60);
        }

        ///<summary>Aligns the wave bottom with the ground; false = blocked by a wall or open pit.</summary>
        bool SnapToGround()
        {
            int tileX = (int)((Projectile.Center.X + Direction * Projectile.width / 2f) / 16f);
            int tileY = (int)((Projectile.position.Y + Projectile.height - 8f) / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5 || tileY > Main.maxTilesY - 10)
            {
                return false;
            }
            int climb = 0;
            while (IsSolid(tileX, tileY) && climb <= 2)
            {
                tileY--;
                climb++;
            }
            if (climb > 2)
            {
                return false;
            }
            int drop = 0;
            while (!IsSolid(tileX, tileY + 1) && drop <= 4)
            {
                tileY++;
                drop++;
            }
            if (drop > 4)
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
