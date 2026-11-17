using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    public class DragonMeteorExplosion : ModProjectile
    {
        internal const float DrakeImpact = 1f;
        private bool FromDrake => Projectile.ai[0] == DrakeImpact;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30; // 5 frames * 6 ticks/frame
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool? CanDamage()
        {
            if (FromDrake && Projectile.timeLeft <= 20)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            // Animation: 6 ticks per frame across 5 frames
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 6)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= 5)
                {
                    Projectile.frame = 4;
                }
            }

            // Impact effects on spawn: brown dirt + red fire dusts shooting upward from ground
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;

                if (!Main.dedServ)
                {
                    SoundStyle impact = SoundID.Item14 with { Volume = 0.9f, Pitch = -0.1f };
                    if (FromDrake)
                    {
                        impact = SoundID.DD2_MonkStaffGroundImpact with { Volume = 0.46f, Pitch = -0.28f, PitchVariance = 0.16f };
                    }
                    SoundEngine.PlaySound(impact, Projectile.Center);

                    // Brown/dirt dusts shooting up from impact point
                    for (int i = 0; i < 26; i++)
                    {
                        Vector2 vel = new Vector2(Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-14f, -4f));
                        Dust d = Dust.NewDustPerfect(Projectile.Center + new Vector2(Main.rand.NextFloat(-24f, 24f), 10f),
                            DustID.Dirt, vel, 80, default, Main.rand.NextFloat(1.3f, 2.2f));
                        d.noGravity = false;
                    }

                    // Red torch / fire dusts shooting up
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 vel = new Vector2(Main.rand.NextFloat(-7f, 7f), Main.rand.NextFloat(-16f, -5f));
                        Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 15f),
                            DustID.Torch, vel, 50, new Color(255, 40, 20), Main.rand.NextFloat(1.6f, 2.8f));
                        d.noGravity = true;
                    }
                }
            }

            // Additional red fire dusts during explosion
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 vel = new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-6f, -1f));
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f),
                    DustID.Torch, vel, 60, new Color(255, 30, 10), Main.rand.NextFloat(1.2f, 2.2f));
                d.noGravity = true;
            }

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, 1.2f, 0.4f, 0.1f);
            }
        }
    }
}
