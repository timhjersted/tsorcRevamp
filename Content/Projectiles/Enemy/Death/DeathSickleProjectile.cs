using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Projectiles.Enemy.Death
{
    /// <summary>
    /// Death's phase-one sickle. It keeps the vanilla Death Sickle texture, but all movement
    /// and lifecycle behavior is owned by this class: no vanilla slowdown or homing.
    /// </summary>
    class DeathSickleProjectile : ModProjectile
    {
        const float DefaultTravelSpeed = 5f;
        const int FadeInTicks = 15;
        const int LifetimeTicks = 8 * 60;
        const int FadeOutTicks = 15;

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.DeathSickle;
        // PLACEHOLDER sprite (copy of vanilla Death Sickle projectile).

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false; // Deliberate: the sickle keeps its straight flight path.
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.light = 0.55f;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage()
        {
            return Projectile.localAI[0] >= FadeInTicks && Projectile.timeLeft > FadeOutTicks;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (Projectile.localAI[0] % 30f == 0f)
            {
                Projectile.netUpdate = true;
            }
            Vector2 direction = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (direction.LengthSquared() < 0.01f)
            {
                direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            }
            else
            {
                direction.Normalize();
            }

            float speedRamp = MathHelper.SmoothStep(
                0f,
                1f,
                MathHelper.Clamp(Projectile.localAI[0] / FadeInTicks, 0f, 1f));
            float travelSpeed = Projectile.ai[2] > 0f ? Projectile.ai[2] : DefaultTravelSpeed;
            Projectile.velocity = direction * travelSpeed * speedRamp;
            Projectile.localAI[1] += 0.22f;
            Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2 + Projectile.localAI[1];

            float fadeIn = MathHelper.Clamp(Projectile.localAI[0] / FadeInTicks, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)FadeOutTicks, 0f, 1f);
            Projectile.alpha = (int)(255f * (1f - fadeIn * fadeOut));

            Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.12f, 0.9f));

            if (Main.dedServ || !Main.rand.NextBool(2))
            {
                return;
            }

            bool pale = Main.rand.NextBool(5);
            Dust trail = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                pale ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                -direction * Main.rand.NextFloat(0.4f, 1.2f),
                100,
                pale ? new Color(220, 210, 255) : new Color(118, 36, 176),
                Main.rand.NextFloat(0.8f, 1.15f));
            trail.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[ProjectileID.DeathSickle].Value;
            float opacity = 1f - Projectile.alpha / 255f;
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor * opacity,
                Projectile.rotation,
                texture.Size() / 2f,
                Projectile.scale,
                SpriteEffects.None,
                0f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 12; i++)
            {
                Dust burst = Dust.NewDustPerfect(
                    Projectile.Center,
                    i % 3 == 0 ? DustID.PurpleTorch : DustID.ShadowbeamStaff,
                    Main.rand.NextVector2Circular(2.4f, 2.4f),
                    80,
                    default,
                    Main.rand.NextFloat(0.9f, 1.3f));
                burst.noGravity = true;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(Projectile.localAI[0]);
            writer.Write(Projectile.localAI[1]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            Projectile.localAI[0] = reader.ReadSingle();
            Projectile.localAI[1] = reader.ReadSingle();
        }
    }
}
