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

        //Lifetime and decay solved together (alongside the 7.5->9 launch speed bump in
        //RunFrostBreath) for ~2x the old ~171px travel (now ~340px) over a correspondingly longer
        //haul — was 45t/~0.75s, now 80t/~1.3s. A bigger initial velocity alone would read as a
        //fast squirt instead of a breath that takes a moment to reach you.
        const int Lifetime = 80;
        const float Decay = 0.978f;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = Lifetime;
            Projectile.light = 0.2f;
        }

        public override void AI()
        {
            Projectile.velocity *= Decay; //billow out and die down

            //Growing frost cloud
            float age = 1f - Projectile.timeLeft / (float)Lifetime;
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

            //Chunky white flecks riding over the shader cone, matching Seath's frozen breath
            //(FrozenDragonsBreath), at half Seath's per-puff rate — a quarter chance per tick
            //instead of half — so the cloud reads as accented rather than as thick as the boss ice
            //dragon's own breath.
            if (Main.rand.NextBool(4))
            {
                int snow = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Snow,
                    Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1f);
                //Two thirds hang weightless and large; the rest keep gravity and stay small, so the
                //cloud gets both a suspended body and a little settling grit.
                if (!Main.rand.NextBool(3))
                {
                    Main.dust[snow].noGravity = true;
                    Main.dust[snow].scale *= 2f;
                    Main.dust[snow].velocity *= 2f;
                }
                Main.dust[snow].scale *= 1.5f;
                Main.dust[snow].velocity *= 1.2f;
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
