using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Chaos
{
    ///<summary>
    ///Times the gale ring's collapse detonation. ChaosGaleRing.TriggerCollapseExplosion spawns the centre
    ///burst immediately and then this: three more in a triangle at TriangleRadius 15 ticks later, then six in
    ///a circle at CircleRadius 15 ticks after that — ten hits total, each independently damaging
    ///(ChaosCollapseBurst). This projectile is invisible and only exists to hold the timing.
    ///</summary>
    class ChaosCollapseExplosionController : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Projectiles.InvisibleNothingProj));

        const int WaveTwoTick = 15;
        const int WaveThreeTick = 30;
        const float TriangleRadius = 300f;
        const float CircleRadius = 600f;
        const float BurstScale = 2.1f; //matches the sprite's own single-instance calibration

        public override void SetDefaults()
        {
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = WaveThreeTick + 1;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            if (Projectile.localAI[0] == WaveTwoTick)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 offset = new Vector2(TriangleRadius, 0f).RotatedBy(MathHelper.TwoPi * i / 3f);
                    SpawnBurst(offset);
                }
            }

            if (Projectile.localAI[0] == WaveThreeTick)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 offset = new Vector2(CircleRadius, 0f).RotatedBy(MathHelper.TwoPi * i / 6f);
                    SpawnBurst(offset);
                }

                Projectile.Kill();
            }

            Projectile.localAI[0]++;
        }

        void SpawnBurst(Vector2 offset)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 spawnPosition = Projectile.Center + offset + Main.rand.NextVector2Circular(15f, 15f);
            int burst = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, Vector2.Zero,
                ModContent.ProjectileType<ChaosCollapseBurst>(), (int)Projectile.ai[0], 0f, Main.myPlayer,
                ai0: Projectile.ai[1]);
            Main.projectile[burst].scale = BurstScale;
            Main.projectile[burst].rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }
    }
}
