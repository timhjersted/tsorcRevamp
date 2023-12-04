using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Projectiles.Throwing;

namespace tsorcRevamp.Projectiles.Summon.Runeterra.Dragons.GrandComet
{
    public class GrandComet : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Projectile.type] = true;
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 500;
            Projectile.height = 500;
            Projectile.friendly = true;
            Projectile.timeLeft = 6000;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 100;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
        }
        public float DistanceToZone;
        public Vector2 CometZone;
        public override void OnSpawn(IEntitySource source)
        {
            CometZone = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            DistanceToZone = Projectile.Center.Distance(CometZone);
        }
        public override void AI()
        {
            int DistanceDivisor = 2000;
            UsefulFunctions.SmoothHoming(Projectile, CometZone, 0.3f * DistanceToZone / DistanceDivisor, 10 * DistanceToZone / DistanceDivisor, null, false);

            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver4;

            int frameSpeed = 3;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            Rectangle CometTopLeftHitbox = new Rectangle(Projectile.Hitbox.X, Projectile.Hitbox.Y, 100, 100);

            if (CometTopLeftHitbox.Intersects(Main.projectile[(int)Projectile.ai[2]].Hitbox))
            {
                Projectile.Kill();
            }
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/CenterOfTheUniverse/CometHit") with { Volume = CenterOfTheUniverse.SoundVolume * 0.5f });
            Dust CometHit = Dust.NewDustDirect(Projectile.Center, 100, 100, DustID.CosmicCarKeys, 0f, 0f, 250, Color.Navy, 2.5f);
            CometHit.noGravity = true;
            Main.projectile[(int)Projectile.ai[2]].ai[0] = 1;

            // Dust spawn
            for (int i = 0; i < 200; i++)
            {
                Dust dust = Dust.NewDustDirect(Projectile.Center, Projectile.width, Projectile.height, DustID.CosmicEmber, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 100, default, 1f);
                dust.noGravity = true;
            }

            int MaxVel = 15;
            int ChunkDmg = Projectile.damage;
            float ChunkKnockback = Projectile.knockBack;
            Vector2 RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<GrandCometChunk01>(), ChunkDmg, ChunkKnockback, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<GrandCometChunk02>(), ChunkDmg, ChunkKnockback, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<GrandCometChunk03>(), ChunkDmg, ChunkKnockback, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<GrandCometChunk04>(), ChunkDmg, ChunkKnockback, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<GrandCometChunk05>(), ChunkDmg, ChunkKnockback, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<GrandCometChunk06>(), ChunkDmg, ChunkKnockback, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<GrandCometChunk07>(), ChunkDmg, ChunkKnockback, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<GrandCometChunk08>(), ChunkDmg, ChunkKnockback, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<GrandCometChunk09>(), ChunkDmg, ChunkKnockback, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<GrandCometChunk10>(), ChunkDmg, ChunkKnockback, Projectile.owner);
            RandomVelocity = Main.rand.NextVector2Circular(MaxVel, MaxVel);
            Projectile.NewProjectileDirect(Projectile.GetSource_None(), Projectile.Center + (RandomVelocity / 2), RandomVelocity, ModContent.ProjectileType<GrandCometChunk11>(), ChunkDmg, ChunkKnockback, Projectile.owner);
        }
    }
}