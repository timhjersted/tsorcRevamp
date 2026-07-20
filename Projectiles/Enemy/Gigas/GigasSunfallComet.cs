using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas sunfall comet: enrage-phase light raining from above. Passes through terrain until it
    ///falls below the nearest player's depth, then collides (the IchorShockwave trick), so ceilings
    ///don't neuter the phase underground. Small golden burst on impact.
    ///</summary>
    class GigasSunfallComet : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
            Projectile.light = 0.5f;
        }

        public override void AI()
        {
            if (Projectile.velocity.Y < 13f)
            {
                Projectile.velocity.Y += 0.3f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation();

            //Only start colliding once below the player, so it always reaches their layer
            Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
            Projectile.tileCollide = target != null && Projectile.Center.Y > target.Center.Y;

            //Comet trail
            for (int seg = 0; seg < 2; seg++)
            {
                Vector2 pos = Projectile.Center - Projectile.velocity * (seg * 0.5f);
                int dust = Dust.NewDust(pos - new Vector2(4, 4), 8, 8, DustID.GoldFlame, 0f, 0f, 80, default, 1.4f - seg * 0.3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
            }
            if (Main.rand.NextBool(3))
            {
                int sparkle = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldCoin, 0f, 0f, 0, default, 0.9f);
                Main.dust[sparkle].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.6f, 0.5f, 0.2f);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.4f, Pitch = -0.2f }, Projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, vel.X, vel.Y - 1f, 80, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
