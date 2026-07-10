using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas heavenly spear: materializes in the air near the player as a converging sparkle cluster
    ///(harmless telegraph), then lances at the player's position. ai[0] = hover ticks before the dive.
    ///The dive locks its direction at launch — repositioning during the hover dodges it.
    ///</summary>
    class GigasHeavenlySpear : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float DiveSpeed = 15f;

        int HoverTicks => (int)Projectile.ai[0];
        bool Diving => Projectile.localAI[0] > HoverTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 600;
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
                Projectile.velocity = Vector2.Zero;
                //Converging sparkles: light drawn inward to the forming spear
                float progress = Projectile.localAI[0] / (float)HoverTicks;
                for (int i = 0; i < 2; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float radius = MathHelper.Lerp(38f, 6f, progress) + Main.rand.NextFloat(8f);
                    Vector2 pos = Projectile.Center + angle.ToRotationVector2() * radius;
                    int dust = Dust.NewDust(pos, 4, 4, DustID.GoldCoin, 0f, 0f, 0, default, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (Projectile.Center - pos) * 0.12f;
                }
                Lighting.AddLight(Projectile.Center, 0.5f * progress, 0.45f * progress, 0.2f * progress);

                //Launch: lock onto the player's position right now
                if (Projectile.localAI[0] >= HoverTicks)
                {
                    Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
                    if (target != null && !target.dead)
                    {
                        Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * DiveSpeed;
                    }
                    else
                    {
                        Projectile.velocity = Vector2.UnitY * DiveSpeed;
                    }
                    Projectile.tileCollide = true;
                    Projectile.timeLeft = 240;
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.6f, Pitch = 0.4f }, Projectile.Center);
                }
                return;
            }

            //Diving: draw the lance as a short dust streak behind the head
            Projectile.rotation = Projectile.velocity.ToRotation();
            for (int seg = 0; seg < 3; seg++)
            {
                Vector2 pos = Projectile.Center - Projectile.velocity * (seg * 0.6f);
                int dust = Dust.NewDust(pos - new Vector2(4, 4), 8, 8, DustID.GoldFlame, 0f, 0f, 100, default, 1.5f - seg * 0.3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.1f;
            }
            Lighting.AddLight(Projectile.Center, 0.8f, 0.7f, 0.3f);
        }

        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);
            for (int i = 0; i < 14; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3.5f, 3.5f);
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.GoldFlame, vel.X, vel.Y, 80, default, 1.4f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
