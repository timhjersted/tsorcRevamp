using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///One puff of the Ice Gigas frost breath. The NPC exhales a stream of these along a sweeping
    ///angle; each is short-lived, decelerates and billows out, so the cone is many small ticks of
    ///damage rather than one chunk. Applies Chilled.
    ///</summary>
    class GigasFrostBreathPuff : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 45;
            Projectile.light = 0.2f;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.965f; //billow out and die down

            //Growing frost cloud
            float age = 1f - Projectile.timeLeft / 45f;
            for (int i = 0; i < 2; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Frost, Projectile.velocity.X * 0.4f, Projectile.velocity.Y * 0.4f, (int)(80 + age * 120f), default, 1.3f + age * 0.5f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = Projectile.velocity * 0.5f + Main.rand.NextVector2Circular(0.8f, 0.8f);
            }
            if (Main.rand.NextBool(4))
            {
                int sparkle = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.IceTorch, 0f, 0f, 100, default, 1f);
                Main.dust[sparkle].noGravity = true;
                Main.dust[sparkle].velocity *= 0.3f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Chilled, 2 * 60);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //Breath washes along walls instead of vanishing at the first tile
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
