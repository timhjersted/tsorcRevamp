using Microsoft.Xna.Framework;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Death
{
    /// <summary>
    /// Cinder Rend: eight 318x252 frames of an open fire wisp, spinning about its frame center.
    /// The native-size texture replaces the old 3x-enlarged art without changing collision or motion.
    /// </summary>
    class DeathFlamingScythe : ModProjectile
    {
        public enum MovementMode
        {
            Boomerang = 0,
            Straight = 1
        }

        const int OutboundTicks = 60;
        const int ReturnTicks = 60;
        const int FadeInTicks = 8;
        const int FadeOutTicks = 10;
        const int StraightFadeInTicks = 20;
        const int StraightFadeOutTicks = 10;
        const int StraightLifetimeTicks = 8 * 60;
        const float ThrowDistance = 800f;
        internal const int AnimationFrames = 8;
        internal const int AnimationFrameTicks = 6;
        internal const float TextureResolutionScale = 3f;

        // Keep the original 80% bounds of the 106 × 84 vanilla sprite available on dedicated servers.
        internal const int HitboxWidth = 84;
        internal const int HitboxHeight = 67;

        MovementMode Mode => (MovementMode)(int)Projectile.ai[2];

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = AnimationFrames;

            // The visual is much larger than the reduced hitbox, so keep it drawing near screen edges.
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }

        public override void SetDefaults()
        {
            Projectile.scale = 3f;
            Projectile.width = HitboxWidth;
            Projectile.height = HitboxHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = OutboundTicks + ReturnTicks + 4;
            Projectile.light = 1f;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override bool? CanDamage()
        {
            if (Mode == MovementMode.Straight)
            {
                return Projectile.localAI[0] >= StraightFadeInTicks
                    && Projectile.timeLeft > StraightFadeOutTicks;
            }

            return Projectile.localAI[0] >= FadeInTicks
                && Projectile.localAI[0] <= OutboundTicks + ReturnTicks - FadeOutTicks;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Projectile.localAI[0] % 30f == 0f)
            {
                Projectile.netUpdate = true;
            }

            int age = (int)Projectile.localAI[0];
            Projectile.frame = age / AnimationFrameTicks % AnimationFrames;

            if (Mode == MovementMode.Straight)
            {
                TickStraight(age);
                return;
            }

            Vector2 origin = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (age <= OutboundTicks)
            {
                float progress = age / (float)OutboundTicks;
                float eased = 1f - (1f - progress) * (1f - progress);
                Projectile.Center = origin + direction * ThrowDistance * eased;
            }
            else if (age <= OutboundTicks + ReturnTicks)
            {
                float progress = (age - OutboundTicks) / (float)ReturnTicks;
                float eased = progress * progress;
                Projectile.Center = origin + direction * ThrowDistance * (1f - eased);
            }
            else
            {
                Projectile.Kill();
                return;
            }

            Projectile.rotation += MathHelper.ToRadians(12f);
            float fadeIn = MathHelper.Clamp(age / (float)FadeInTicks, 0f, 1f);
            float fadeOut = MathHelper.Clamp(
                (OutboundTicks + ReturnTicks - age + 1) / (float)FadeOutTicks,
                0f,
                1f);
            Projectile.alpha = (int)(255f * (1f - fadeIn * fadeOut));
            Lighting.AddLight(Projectile.Center, new Vector3(1.5f, 0.65f, 0.20f));
        }

        void TickStraight(int age)
        {
            if (Projectile.localAI[1] <= 0f)
            {
                Projectile.localAI[1] = 1f;
                Projectile.timeLeft = StraightLifetimeTicks;
            }

            Vector2 velocity = Projectile.velocity;
            if (velocity.LengthSquared() < 0.01f)
            {
                velocity = Vector2.UnitX;
            }

            Projectile.Center += velocity;
            Projectile.rotation += MathHelper.ToRadians(12f);
            float fadeIn = MathHelper.Clamp(age / (float)StraightFadeInTicks, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)StraightFadeOutTicks, 0f, 1f);
            Projectile.alpha = (int)(255f * (1f - fadeIn * fadeOut));
            Lighting.AddLight(Projectile.Center, new Vector3(1.5f, 0.65f, 0.20f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, AnimationFrames, 0, Projectile.frame);
            Vector2 origin = frame.Size() / 2f;
            float drawScale = Projectile.scale / TextureResolutionScale;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Color glowColor = new Color(255, 120, 35);
            float opacity = 1f - Projectile.alpha / 255f;

            for (int i = 3; i >= 1; i--)
            {
                float scale = drawScale * (1f + i * 0.18f);
                Main.EntitySpriteDraw(
                    texture,
                    position,
                    frame,
                    glowColor * (0.10f * i * opacity),
                    Projectile.rotation,
                    origin,
                    scale,
                    SpriteEffects.None,
                    0f);
            }

            Main.EntitySpriteDraw(
                texture,
                position,
                frame,
                lightColor * opacity,
                Projectile.rotation,
                origin,
                drawScale,
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

            for (int i = 0; i < 36; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    i % 3 == 0 ? DustID.Torch : DustID.FireworkFountain_Red,
                    Main.rand.NextVector2Circular(7f, 7f),
                    80,
                    new Color(255, 130, 40),
                    Main.rand.NextFloat(1.2f, 1.8f));
                dust.noGravity = true;
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
