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
    /// Large custom flaming scythe used by Death's weak attack 2. Only the vanilla
    /// FlamingScythe texture is borrowed; movement, collision and glow are custom.
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

        MovementMode Mode => (MovementMode)(int)Projectile.ai[2];

        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.FlamingScythe;
        // PLACEHOLDER sprite (copy of vanilla Flaming Scythe projectile).

        public override void SetStaticDefaults()
        {
            // The visual is much larger than the reduced hitbox, so keep it drawing near screen edges.
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 500;
        }

        public override void SetDefaults()
        {
            Projectile.scale = 3f;
            Texture2D texture = ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            Projectile.width = (int)(texture.Width * 0.8f);
            Projectile.height = (int)(texture.Height * 0.8f);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = OutboundTicks + ReturnTicks + 4;
            Projectile.light = 1f;
            Projectile.DamageType = DamageClass.Melee;
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
            if (Mode == MovementMode.Straight)
            {
                TickStraight(age);
                return;
            }

            Vector2 origin = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (age <= OutboundTicks)
            {
                float t = age / (float)OutboundTicks;
                float eased = 1f - (1f - t) * (1f - t);
                Projectile.Center = origin + direction * ThrowDistance * eased;
            }
            else if (age <= OutboundTicks + ReturnTicks)
            {
                float t = (age - OutboundTicks) / (float)ReturnTicks;
                float eased = t * t;
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
            Texture2D texture = TextureAssets.Projectile[ProjectileID.FlamingScythe].Value;
            Vector2 origin = texture.Size() / 2f;
            Vector2 position = Projectile.Center - Main.screenPosition;
            Color glowColor = new Color(255, 120, 35);
            float opacity = 1f - Projectile.alpha / 255f;

            for (int i = 3; i >= 1; i--)
            {
                float scale = Projectile.scale * (1f + i * 0.18f);
                Main.EntitySpriteDraw(
                    texture,
                    position,
                    null,
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
                null,
                lightColor * opacity,
                Projectile.rotation,
                origin,
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
