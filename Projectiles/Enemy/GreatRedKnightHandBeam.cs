using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class GreatRedKnightHandBeam : WyvernMage.RedLightning
    {
        // RedLightning's `Texture => base.Texture` resolves off the RUNTIME type (GreatRedKnightHandBeam),
        // not the declaring class, so simply inheriting it isn't enough — this class needs its own
        // override. Intentional permanent reuse of RedLightning's laser texture (same visual, no bespoke
        // art needed), matching the CLAUDE.md Texture Override Rule.
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/WyvernMage/RedLightning";

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 85; // 60t telegraph + 25t firing
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;

            FollowHost = true;
            ProjectileSource = true;
            TelegraphTime = 60;
            FiringDuration = 25;
            MaxCharge = 60;
            LaserLength = 1600;
        }

        public override void AI()
        {
            int hostProjIndex = (int)Projectile.ai[0];
            if (hostProjIndex >= 0 && hostProjIndex < Main.maxProjectiles && Main.projectile[hostProjIndex].active)
            {
                HostIdentifier = hostProjIndex;
                LaserOrigin = Main.projectile[hostProjIndex].Center;
            }
            base.AI();
        }

        public override void OnKill(int timeLeft)
        {
            // Spawn no-gravity red dust trail along the laser line for kill effect (fade away)
            Vector2 start = LaserOrigin;
            Vector2 end = start + Projectile.velocity.SafeNormalize(Vector2.UnitX) * LaserLength;
            float dist = Vector2.Distance(start, end);
            int dustCount = (int)(dist / 14f);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 pos = Vector2.Lerp(start, end, (float)i / dustCount);
                int d = Dust.NewDust(pos, 0, 0, DustID.RedTorch, 0f, 0f, 100, default, 1.2f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = Main.rand.NextVector2Circular(0.6f, 0.6f);
            }
            base.OnKill(timeLeft);
        }
    }
}
