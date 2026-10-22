using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Minotaur
{
    ///<summary>
    ///Minotaur Mage's staff-slam wave: a short fire wave crawling along the ground away from the
    ///slam (small cousin of GigasShockwave). ai[0] = direction (-1/1), ai[1] = arming delay ticks.
    ///Hugs the terrain; dies against walls taller than 2 tiles. 33 tiles of reach (4x original).
    ///ai[2] is the synced age. A harmless 12t extinguish follows the 88 moving ticks.
    ///</summary>
    class MinotaurFlameWave : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        const float WaveSpeed = 6f;
        const int WaveTravelTicks = 88; //528px: exactly four times the original 132px
        const int FadeTicks = 12;

        int Direction => (int)Projectile.ai[0] >= 0 ? 1 : -1;
        int ArmDelay => (int)Projectile.ai[1];
        int Age => (int)Projectile.ai[2];
        bool Armed => Age > ArmDelay && Age <= ArmDelay + WaveTravelTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 24;
            Projectile.height = 32;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 500;
            Projectile.light = 0.35f;
        }

        public override bool? CanDamage()
        {
            return Armed;
        }

        public override void AI()
        {
            Projectile.ai[2]++;
            Projectile.velocity = Vector2.Zero;
            if (Age > ArmDelay + WaveTravelTicks + FadeTicks)
            {
                Projectile.Kill();
                return;
            }

            if (!Armed)
            {
                if (!Main.dedServ && Age <= ArmDelay && Main.rand.NextBool(2))
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
            if (Main.dedServ) return;

            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Projectile.height - 6f);
                int dust = Dust.NewDust(pos, 4, 4, DustID.Torch, Direction * 0.5f, Main.rand.NextFloat(-4f, -2f), 80, default, Main.rand.NextFloat(1.3f, 1.8f));
                Main.dust[dust].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.5f, 0.3f, 0.1f);
        }

        public override bool PreDraw(ref Color lightColor) => false;
        public override void PostDraw(Color lightColor)
        {
            float opacity = Age <= ArmDelay ? 0.3f
                : MathHelper.Clamp((ArmDelay + WaveTravelTicks + FadeTicks - Age) / (float)FadeTicks, 0f, 1f) * 0.85f;
            Vector2 size = Age <= ArmDelay ? new Vector2(32f, 18f) : new Vector2(32f, 46f);
            MinotaurFireVFX.Draw(Projectile.Bottom - new Vector2(0f, size.Y * 0.44f), size,
                opacity, Projectile.identity * 0.17f);
        }

        public override void OnKill(int timeLeft) => MinotaurFireVFX.Extinguish(Projectile.Center, 25);

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
