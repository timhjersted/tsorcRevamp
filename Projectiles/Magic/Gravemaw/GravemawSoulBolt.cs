using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic.Gravemaw
{
    ///<summary>Soulspit's homing soul-bolt (friendly magic). Reuses the PurpleSkull sprite. ai[0]=homing.</summary>
    class GravemawSoulBolt : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/VesselOfSouls/PurpleSkull";

        float Homing => Projectile.ai[0];

        public override void SetStaticDefaults() => Main.projFrames[Type] = 4;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 240;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (++Projectile.frameCounter >= 6) { Projectile.frameCounter = 0; Projectile.frame = (Projectile.frame + 1) % 4; }
            if (Homing > 0f)
            {
                NPC t = FindTarget();
                if (t != null)
                {
                    float speed = Projectile.velocity.Length();
                    Vector2 desired = (t.Center - Projectile.Center).SafeNormalize(Projectile.velocity) * speed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, Homing);
                }
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (Main.rand.NextBool(2))
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch, 0f, 0f, 130, default, 0.9f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity *= 0.2f;
            }
            Lighting.AddLight(Projectile.Center, 0.3f, 0.08f, 0.4f);
        }

        NPC FindTarget()
        {
            NPC closest = null;
            float best = 650f * 650f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(this)) continue;
                float dSq = Vector2.DistanceSquared(npc.Center, Projectile.Center);
                if (dSq < best) { best = dSq; closest = npc; }
            }
            return closest;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 8; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.PurpleTorch, 0f, 0f, 120, default, 1f);
                Main.dust[d].noGravity = true;
            }
        }
    }
}
