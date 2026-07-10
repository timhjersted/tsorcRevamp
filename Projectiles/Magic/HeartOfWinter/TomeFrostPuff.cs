using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    ///<summary>
    ///Heart of Winter left hold: one puff of the channeled frost breath. Billows out and dies down,
    ///pierces everything with a local hit cooldown so the stream reads as continuous ticks.
    ///</summary>
    class TomeFrostPuff : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 40;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.2f;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.965f;

            float age = 1f - Projectile.timeLeft / 40f;
            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Frost, Projectile.velocity.X * 0.4f, Projectile.velocity.Y * 0.4f, (int)(80 + age * 120f), default, 1.2f + age * 0.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Projectile.velocity * 0.5f + Main.rand.NextVector2Circular(0.8f, 0.8f);
            }
            if (Main.rand.NextBool(4))
            {
                int sparkle = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch, 0f, 0f, 100, default, 0.9f);
                Main.dust[sparkle].noGravity = true;
                Main.dust[sparkle].velocity *= 0.3f;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Frostburn, 3 * 60);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //Wash along walls instead of vanishing
            if (Projectile.velocity.X != oldVelocity.X)
            {
                Projectile.velocity.X = 0f;
            }
            if (Projectile.velocity.Y != oldVelocity.Y)
            {
                Projectile.velocity.Y = 0f;
            }
            return false;
        }
    }
}
