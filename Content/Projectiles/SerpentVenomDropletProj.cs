using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles
{
    public class SerpentVenomDropletProj : ModProjectile
    {
        // ai[0]: 0 = SummonMeleeSpeed, 1 = Ranged, 2 = Melee, 3 = Magic.
        // ai[1]: >= 1 inflicts Acid Venom (Venom) instead of Poisoned (+3 milestone perk).
        public const float SummonDamageTag = 0f;
        public const float RangedDamageTag = 1f;
        public const float MeleeDamageTag = 2f;
        public const float MagicDamageTag = 3f;

        private bool InflictsAcidVenom => Projectile.ai[1] >= 1f;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
        }

        public override void OnSpawn(IEntitySource source)
        {
            int damageTag = (int)Projectile.ai[0];
            if (damageTag == (int)RangedDamageTag)
            {
                Projectile.DamageType = DamageClass.Ranged;
            }
            else if (damageTag == (int)MeleeDamageTag)
            {
                Projectile.DamageType = DamageClass.Melee;
            }
            else if (damageTag == (int)MagicDamageTag)
            {
                Projectile.DamageType = DamageClass.Magic;
            }
            else
            {
                Projectile.DamageType = DamageClass.SummonMeleeSpeed;
            }
        }

        public override void AI()
        {
            Projectile.velocity.Y += 0.26f;
            if (Projectile.velocity.Y > 14f)
            {
                Projectile.velocity.Y = 14f;
            }

            if (Projectile.velocity.LengthSquared() > 0.01f)
            {
                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            }

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, 0.3f, 0.1f, 0.5f);

            // Interpolate 2 trail motes along the tick step so droplets leave a tight, continuous venom arc.
            for (int i = 0; i < 2; i++)
            {
                float stepFraction = i * 0.5f;
                Vector2 samplePos = Vector2.Lerp(Projectile.oldPosition, Projectile.position, stepFraction) + Projectile.Size * 0.5f;
                Vector2 jitter = Main.rand.NextVector2Circular(2f, 2f);
                Dust venom = Dust.NewDustPerfect(samplePos + jitter, DustID.Venom, -Projectile.velocity * 0.12f, 45, default, Main.rand.NextFloat(0.8f, 1.05f));
                venom.noGravity = true;
            }

            if (Main.GameUpdateCount % 2 == 0)
            {
                Dust glow = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, -Projectile.velocity * 0.1f, 55, new Color(180, 75, 255), 0.85f);
                glow.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (InflictsAcidVenom)
            {
                target.AddBuff(BuffID.Venom, 4 * 60);
            }
            else
            {
                target.AddBuff(BuffID.Poisoned, 4 * 60);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.35f, Pitch = 0.25f }, Projectile.Center);

            for (int i = 0; i < 6; i++)
            {
                Vector2 burstVel = Main.rand.NextVector2Circular(3.2f, 3.2f) + new Vector2(0f, -0.8f);
                Dust splash = Dust.NewDustPerfect(Projectile.Center, DustID.Venom, burstVel, 45, default, Main.rand.NextFloat(0.9f, 1.25f));
                splash.noGravity = true;
            }

            for (int i = 0; i < 4; i++)
            {
                Vector2 glowVel = Main.rand.NextVector2Circular(2.4f, 2.4f);
                Dust glow = Dust.NewDustPerfect(Projectile.Center, DustID.AncientLight, glowVel, 55, new Color(185, 80, 255), Main.rand.NextFloat(0.85f, 1.15f));
                glow.noGravity = true;
            }
        }
    }
}
