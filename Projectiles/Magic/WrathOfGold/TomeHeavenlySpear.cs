using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{
    ///<summary>
    ///Wrath of Gold left tap: a golden spear that materializes over the cursor as a converging
    ///sparkle cluster, then lances down onto the strike point. Spawn velocity = the locked dive
    ///direction (unit length); ai[0] = hover ticks. Player mirror of the boss's Heavenly Spears.
    ///</summary>
    class TomeHeavenlySpear : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float DiveSpeed = 16f;

        int HoverTicks => (int)Projectile.ai[0];
        bool Diving => Projectile.localAI[0] > HoverTicks;

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.4f;
        }

        public override bool? CanDamage()
        {
            return Diving;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (!Diving)
            {
                //Converging sparkles forming the spear; the dive direction is locked in velocity (unit length)
                float progress = Projectile.localAI[0] / (float)HoverTicks;
                for (int i = 0; i < 2; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = MathHelper.Lerp(34f, 6f, progress) + Main.rand.NextFloat(8f);
                    Vector2 pos = Projectile.Center + angle.ToRotationVector2() * radius;
                    int dust = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, 0f, 0, default, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (Projectile.Center - pos) * 0.12f;
                }
                if (Projectile.localAI[0] >= HoverTicks)
                {
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * DiveSpeed;
                    Projectile.tileCollide = true;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.6f, Pitch = 0.4f }, Projectile.Center);
                }
                return;
            }

            //Diving: dust lance behind the head
            Projectile.rotation = Projectile.velocity.ToRotation();
            for (int seg = 0; seg < 3; seg++)
            {
                Vector2 pos = Projectile.Center - Projectile.velocity * (seg * 0.6f);
                int dust = Dust.NewDust(pos - new Vector2(4, 4), 8, 8, DustID.GoldFlame, 0f, 0f, 100, default, 1.4f - seg * 0.3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
            }
            Lighting.AddLight(Projectile.Center, 0.7f, 0.6f, 0.25f);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = -0.2f }, Projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, vel.X, vel.Y, 80, default, 1.3f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
