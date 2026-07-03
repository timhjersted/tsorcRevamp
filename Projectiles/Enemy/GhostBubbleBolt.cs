using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    // Ghost of the Drowned's long-range poke: the vanilla Bubble spray drags/curves and dies out
    // within a few tiles, so it's useless once the fight opens up. This travels far and ignores
    // gravity/tiles, but still wobbles with a gentle sideways drift so it doesn't look laser-straight
    // and unnatural — echoing the old Bubble's random wander.
    class GhostBubbleBolt : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bubble;

        // ai[0]: this bolt's own signed drift strength, rolled once at spawn (see GhostOfTheDrowned's
        // NewProjectile call) so every bolt curves a little differently — some barely wander, some arc
        // hard to one side — instead of every shot drifting identically.
        public float DriftStrength
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 100;
            Projectile.alpha = 60;
            Projectile.penetrate = 1;
        }

        public override void AI()
        {
            // Capture launch speed once so the sideways wobble can be bled back out each tick instead
            // of compounding into an ever-accelerating curve.
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = Projectile.velocity.Length();
            }

            Vector2 perpendicular = new Vector2(-Projectile.velocity.Y, Projectile.velocity.X).SafeNormalize(Vector2.Zero);
            float wobble = (float)Math.Sin(Projectile.localAI[1] * 0.08f) * DriftStrength;
            Projectile.velocity += perpendicular * wobble * 0.15f;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * Projectile.localAI[0];
            Projectile.localAI[1] += 1f;

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Main.rand.NextBool(3))
            {
                int dust = Dust.NewDust(Projectile.Center, 4, 4, DustID.UltraBrightTorch, 0f, 0f, 100, default(Color), 0.6f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= 0.2f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            SoundEngine.PlaySound(SoundID.Drown, target.Center);
        }
    }
}
