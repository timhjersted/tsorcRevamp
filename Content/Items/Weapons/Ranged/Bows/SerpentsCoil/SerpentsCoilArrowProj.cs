using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;

namespace tsorcRevamp.Content.Items.Weapons.Ranged.Bows.SerpentsCoil
{
    public class SerpentsCoilArrowProj : ModProjectile
    {
        // ai[1] carries reinforcement perk tier: 0 = base, 1 = +3 Acid Venom & 4 droplets, 2 = +6 60% droplets.
        private int PerkTier => (int)Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.arrow = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            if (Projectile.ai[0] > 15f)
            {
                Projectile.velocity.Y += 0.09f;
                if (Projectile.velocity.Y > 16f)
                {
                    Projectile.velocity.Y = 16f;
                }
            }

            if (Projectile.velocity.LengthSquared() > 0.01f)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, 0.42f, 0.16f, 0.72f);

            // Interpolate 2 motes per tick along the arrow's fast step so the venom trail stays unbroken.
            for (int i = 0; i < 2; i++)
            {
                float stepFraction = i * 0.5f;
                Vector2 samplePos = Vector2.Lerp(Projectile.oldPosition, Projectile.position, stepFraction) + Projectile.Size * 0.5f;
                Dust venom = Dust.NewDustPerfect(
                    samplePos + Main.rand.NextVector2Circular(2f, 2f),
                    DustID.Venom,
                    -Projectile.velocity * 0.08f,
                    45,
                    default,
                    Main.rand.NextFloat(0.85f, 1.05f));
                venom.noGravity = true;
            }

            Dust glow = Dust.NewDustPerfect(
                Projectile.Center,
                DustID.AncientLight,
                -Projectile.velocity * 0.06f,
                55,
                new Color(185, 80, 255),
                0.85f);
            glow.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (PerkTier >= 1)
            {
                target.AddBuff(BuffID.Venom, 4 * 60);
            }
            else
            {
                target.AddBuff(BuffID.Poisoned, 4 * 60);
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 sparkVel = Main.rand.NextVector2Circular(3.2f, 3.2f);
                    Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, sparkVel, 45, default, 1.05f);
                    spark.noGravity = true;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);

                for (int i = 0; i < 10; i++)
                {
                    Vector2 burstVel = Main.rand.NextVector2Circular(3.8f, 3.8f);
                    Dust burst = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, burstVel, 45, default, Main.rand.NextFloat(0.95f, 1.25f));
                    burst.noGravity = true;
                }

                for (int i = 0; i < 5; i++)
                {
                    Vector2 glowVel = Main.rand.NextVector2Circular(3.0f, 3.0f);
                    Dust glow = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, glowVel, 55, new Color(190, 85, 255), 1.0f);
                    glow.noGravity = true;
                }
            }

            if (Main.myPlayer != Projectile.owner)
            {
                return;
            }

            float dropletScale = PerkTier >= 2 ? SerpentsCoilItem.Tier6DropletDamagePercent / 100f : 0.45f;
            int dropletDamage = (int)(Projectile.damage * dropletScale);
            if (dropletDamage < 1)
            {
                dropletDamage = 1;
            }

            int dropletCount = PerkTier >= 1 ? SerpentsCoilItem.Tier3SplashDropletCount : SerpentsCoilItem.SplashDropletCount;
            float acidVenomFlag = PerkTier >= 1 ? 1f : 0f;

            // Burst into arcing venom droplets fanned upward/outward from the impact point.
            for (int i = 0; i < dropletCount; i++)
            {
                float fraction = dropletCount > 1 ? i / (float)(dropletCount - 1) : 0.5f;
                float horizontalSpread = MathHelper.Lerp(-3.6f, 3.6f, fraction);
                Vector2 dropletVel = new Vector2(horizontalSpread + Main.rand.NextFloat(-0.7f, 0.7f), -Main.rand.NextFloat(3.5f, 5.4f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    dropletVel,
                    ModContent.ProjectileType<SerpentVenomDropletProj>(),
                    dropletDamage,
                    1.5f,
                    Projectile.owner,
                    SerpentVenomDropletProj.RangedDamageTag,
                    acidVenomFlag);
            }
        }
    }
}
