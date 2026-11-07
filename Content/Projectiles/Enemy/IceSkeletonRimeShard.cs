using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Lance of Rime: the ice spear-head the Ice Skeleton forms on its spear tip and thrusts free.
    /// Sprite: 31x31, cut from the spear's own ice head; the point is the TOP-LEFT corner, so rotation is
    /// velocity + 3pi/4. Straight 12px/t, no gravity or homing. Dies on tiles and players with an ice burst and a
    /// glass-break sound; on timeout it fades over the last 12t. No children.
    /// </summary>
    public class IceSkeletonRimeShard : ModProjectile
    {
        public const float Speed = 12f;
        const int Lifetime = 90;
        const int FadeTicks = 12;
        // The sprite's point is at its top-left corner (-135 degrees in texture space).
        const float PointLeadsOffset = MathHelper.Pi * 0.75f;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = Lifetime;
            Projectile.aiStyle = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + PointLeadsOffset;

            // Fade over the last 12t of a timeout, so a shard that flies off never vanishes at full opacity.
            int remaining = Projectile.timeLeft;
            Projectile.Opacity = 1f;
            if (remaining < FadeTicks)
            {
                Projectile.Opacity = remaining / (float)FadeTicks;
            }

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.25f, 0.45f, 0.6f) * Projectile.Opacity);

            // One white-blue mote every 3t, shed backwards off the tail.
            if (Projectile.timeLeft % 3 == 0)
            {
                Vector2 tail = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 10f;
                Dust mote = Dust.NewDustPerfect(tail, DustID.IceTorch, -Projectile.velocity * 0.08f, 80, default, 1.1f);
                mote.noGravity = true;
            }
        }

        // Hostile shots are not consumed by a player hit on their own; the shared global relays the kill in MP.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // A timed-out shard has already faded; only an impact shatters.
            bool impact = timeLeft > 0;
            if (!impact)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.7f, Pitch = 0.2f }, Projectile.Center);

            for (int i = 0; i < 40; i++)
            {
                int dustType = DustID.IceTorch;
                if (i % 3 == 0)
                {
                    dustType = DustID.BlueCrystalShard;
                }

                Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                Dust shard = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 60, default, Main.rand.NextFloat(0.9f, 1.5f));
                shard.noGravity = dustType == DustID.IceTorch;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            // Ice glows a little in the dark: lift the light toward a cold white.
            Color light = Color.Lerp(lightColor, new Color(200, 230, 255), 0.5f) * Projectile.Opacity;
            Main.EntitySpriteDraw(texture, drawPosition, null, light, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }
}
