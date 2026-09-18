using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Soul of Cinder's staff seeker. PhantomFireSeeker.png is an 82x200 vertical sheet:
    /// four 82x50 frames, with the flame head facing right.
    /// </summary>
    class PhantomFireStaffSeeker : ModProjectile
    {
        const int Frames = 4;
        const int FadeTicks = 24;


        public override void SetStaticDefaults() => Main.projFrames[Type] = Frames;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.penetrate = 3;
            Projectile.width = 34;
            Projectile.height = 22;
            Projectile.alpha = 255;
            // Deliberate: this spectral seeker retains its old wall-passing pursuit identity.
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 460;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Frames;
            }

            Projectile.alpha = Math.Max(0, Projectile.alpha - 24);
            int targetIndex = ResolveTarget();
            if (targetIndex >= 0 && Projectile.timeLeft > 100)
            {
                Player target = Main.player[targetIndex];
                Vector2 direct = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Vector2 perpendicular = new Vector2(-direct.Y, direct.X);
                float wave = MathF.Sin(Projectile.localAI[0] * 0.11f + Projectile.ai[1]) * 1.8f;
                float speed = MathHelper.Lerp(7.5f, 9.5f,
                    MathHelper.Clamp(Projectile.localAI[0] / 150f, 0f, 1f));
                Vector2 desired = direct * speed + perpendicular * wave;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.085f);
            }

            if (Projectile.velocity.LengthSquared() > 0.01f)
                Projectile.rotation = Projectile.velocity.ToRotation();

            if (Projectile.timeLeft <= FadeTicks)
            {
                Projectile.damage = 0;
                Projectile.alpha = Math.Min(255, Projectile.alpha + 12);
                Projectile.scale *= 0.94f;
            }

            if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
            {
                Vector2 rear = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                bool gold = ((int)Projectile.localAI[0] / 2) % 2 == 0;
                Dust trail = Dust.NewDustPerfect(Projectile.Center + rear * 13f,
                    gold ? DustID.GoldFlame : DustID.OrangeTorch,
                    rear * Main.rand.NextFloat(0.8f, 2.1f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    90, gold ? Color.Gold : Color.OrangeRed, Main.rand.NextFloat(0.55f, 0.95f));
                trail.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, new Vector3(0.95f, 0.35f, 0.04f) * 0.65f);
        }

        int ResolveTarget()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex >= 0 && targetIndex < Main.maxPlayers
                && Main.player[targetIndex].active && !Main.player[targetIndex].dead)
            {
                return targetIndex;
            }

            int nearest = Player.FindClosest(Projectile.position, Projectile.width, Projectile.height);
            if (nearest >= 0 && nearest < Main.maxPlayers
                && Main.player[nearest].active && !Main.player[nearest].dead)
            {
                Projectile.ai[0] = nearest;
                Projectile.netUpdate = true;
                return nearest;
            }
            return -1;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int expertScaling = Main.expertMode ? 2 : 1;
            target.AddBuff(BuffID.BrokenArmor, 120 / expertScaling, false);
            target.AddBuff(BuffID.OnFire, 600 / expertScaling, false);
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 3600, false);

            if (Main.rand.NextBool(10))
            {
                target.AddBuff(BuffID.Slow, 600 / expertScaling, false);
                target.AddBuff(BuffID.OnFire, 600 / expertScaling, false);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 70; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                bool gold = i % 3 != 0;
                Dust dust = Dust.NewDustPerfect(Projectile.Center,
                    gold ? DustID.GoldFlame : DustID.OrangeTorch,
                    direction * Main.rand.NextFloat(1.5f, 7f), 90,
                    gold ? Color.Gold : Color.OrangeRed, Main.rand.NextFloat(0.55f, 1.2f));
                dust.noGravity = true;
            }
        }
    }
}
