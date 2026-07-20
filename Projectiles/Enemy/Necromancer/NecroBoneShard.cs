using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Necromancer bone shard (vanilla skeleton-bone texture): the fan's fragments. Arcs under light
    ///gravity and ricochets ONCE off tiles before shattering — corners aren't safe. ai[0] = gravity.
    ///</summary>
    class NecroBoneShard : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.SkeletonBone;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 240;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Projectile.ai[0];
            if (Projectile.velocity.Y > 12f)
            {
                Projectile.velocity.Y = 12f;
            }
            Projectile.rotation += 0.3f * (Projectile.velocity.X >= 0f ? 1f : -1f);

            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Bone, 0f, 0f, 80, default, 0.9f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.3f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //One ricochet, then shatter
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] > 1)
            {
                return true;
            }
            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = -oldVelocity.X * 0.9f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = -oldVelocity.Y * 0.8f;
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item2 with { Volume = 0.4f, Pitch = 0.3f }, Projectile.Center);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item2 with { Volume = 0.4f, Pitch = -0.2f }, Projectile.Center);
            for (int i = 0; i < 6; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(2.5f, 2.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Bone, vel.X, vel.Y, 60, default, 1.1f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
