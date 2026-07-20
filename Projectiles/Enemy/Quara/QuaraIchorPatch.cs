using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///A patch of black ichor left on the ground by a missed glob. INERT (harmless, no damage) until a
    ///Pincher ember lands near it — then it IGNITES into a short-lived fire wall that burns anyone standing
    ///in it (and detonates a doused player). ai[0]: 0 = inert, 1 = burning. Ignited by QuaraEmber on impact.
    ///</summary>
    class QuaraIchorPatch : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public const int PatchWidth = 64; //4 tiles
        public const int PatchHeight = 16;
        public const int InertLifetime = 8 * 60;
        public const int BurnLifetime = 3 * 60;

        bool Ignited => Projectile.ai[0] == 1f;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = PatchWidth;
            Projectile.height = PatchHeight;
            Projectile.tileCollide = false;
            Projectile.aiStyle = 0;
            Projectile.penetrate = -1;
            Projectile.timeLeft = InertLifetime;
        }

        public override bool? CanDamage() => Ignited; //inert goo does nothing; only fire hurts

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            if (!Ignited)
            {
                //Slick, bubbling black ichor — the read that this floor is dangerous IF lit
                if (Main.rand.NextBool(2))
                {
                    Vector2 pos = new(Projectile.position.X + Main.rand.NextFloat(PatchWidth), Projectile.Bottom.Y - Main.rand.NextFloat(6f));
                    int d = Dust.NewDust(pos, 2, 2, DustID.Ichor, 0f, -0.4f, 150, default, 0.9f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.15f;
                }
                return;
            }

            //Burning: a low wall of flame
            Lighting.AddLight(Projectile.Center, 0.6f, 0.35f, 0.1f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new(Projectile.position.X + Main.rand.NextFloat(PatchWidth), Projectile.Bottom.Y - Main.rand.NextFloat(PatchHeight * 0.6f));
                int d = Dust.NewDust(pos, 4, 4, DustID.Torch, 0f, -1.5f, 100, default, 1.4f);
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity.Y = -1.6f;
            }
        }

        ///<summary>Called by a QuaraEmber landing nearby. Deterministic on every client (distance-based).</summary>
        public void Ignite()
        {
            if (Ignited)
            {
                return;
            }
            Projectile.ai[0] = 1f;
            Projectile.timeLeft = BurnLifetime;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 16; i++)
            {
                int d = Dust.NewDust(Projectile.position, PatchWidth, PatchHeight, DustID.Torch, 0f, -2f, 80, default, 1.4f);
                Main.dust[d].noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire3, 3 * 60); //ignites the target -> detonates a doused player
        }
    }
}
