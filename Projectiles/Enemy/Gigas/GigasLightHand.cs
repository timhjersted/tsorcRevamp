using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Gigas grasping light: two tall slabs of golden light rise from the ground at the player's
    ///flanks and converge, clapping together at the point where the player was standing. Damage is
    ///only in the seam during the clap window — walk/roll out sideways before they meet.
    ///ai[0] = telegraph (converge) ticks. One projectile manages both hands as dust sculptures.
    ///</summary>
    class GigasLightHand : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float HandStartOffset = 100f;
        const float HandHeight = 130f;
        const int ClapTicks = 10;

        int TelegraphTicks => (int)Projectile.ai[0] > 0 ? (int)Projectile.ai[0] : 50;
        bool Clapping => Projectile.localAI[0] > TelegraphTicks;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 70;
            Projectile.height = 130;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 300;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TelegraphTicks + ClapTicks;
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.5f }, Projectile.Center);
        }

        public override bool? CanDamage()
        {
            return Clapping;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            if (!Clapping)
            {
                //Two converging hand-slabs of light, rising from the ground line
                float progress = Projectile.localAI[0] / (float)TelegraphTicks;
                float offset = MathHelper.Lerp(HandStartOffset, 12f, progress * progress); //accelerating convergence
                float bottom = Projectile.Center.Y + HandHeight / 2f;
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = Projectile.Center.X + side * offset;
                    for (int i = 0; i < 3; i++)
                    {
                        //Rise the slab in with progress: dust only up to the current height
                        float y = bottom - Main.rand.NextFloat(HandHeight * MathHelper.Min(1f, progress * 2f));
                        int dust = Dust.NewDust(new Vector2(x - 5f, y), 10, 4, DustID.GoldFlame, 0f, 0f, 90, default, 1.3f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].velocity = new Vector2(-side * 0.4f, -0.8f);
                    }
                    //"Fingers": brighter sparkles crowning the slab
                    if (Main.rand.NextBool(2))
                    {
                        float y = bottom - HandHeight * MathHelper.Min(1f, progress * 2f);
                        int sparkle = Dust.NewDust(new Vector2(x - 6f, y), 12, 6, DustID.GoldCoin, 0f, -1f, 0, default, 1f);
                        Main.dust[sparkle].noGravity = true;
                    }
                }
                Lighting.AddLight(Projectile.Center, 0.5f * progress, 0.45f * progress, 0.2f * progress);

                if (Projectile.localAI[0] >= TelegraphTicks)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = 0.3f }, Projectile.Center);
                    UsefulFunctions.ScreenShake(Projectile.Center, 5f, 12);
                }
                return;
            }

            //Clap: a single blazing seam where the hands met
            for (int i = 0; i < 10; i++)
            {
                Vector2 pos = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-14f, 14f), Projectile.position.Y + Main.rand.NextFloat(Projectile.height));
                int dust = Dust.NewDust(pos, 4, 4, DustID.GoldFlame, 0f, 0f, 50, default, Main.rand.NextFloat(1.6f, 2.2f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-5f, -2f));
            }
            Lighting.AddLight(Projectile.Center, 1f, 0.9f, 0.4f);
        }
    }
}
