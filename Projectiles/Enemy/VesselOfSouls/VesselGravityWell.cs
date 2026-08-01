using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.VesselOfSouls
{
    ///<summary>
    ///The INHALE made physical: a purple singularity at the Vessel's mouth that radially drags every
    ///player toward it. Deals no damage itself — it sets up the follow-up cone (Gravemaw Tug's Black Nip,
    ///the Gravity Well's Black Breath). Resistible by holding away / dodge-rolling, capped so it never
    ///fully locks a player. Modelled on GwynGravityWell.
    ///  ai[0] = parent NPC whoAmI
    ///  ai[1] = duration (ticks)
    ///  ai[2] = pull radius (px; 0 = default 620). Pull STRENGTH scales off the radius: a small
    ///          phase-1 tug (≤500) is gentle, the full channel (≥700) is strong, and the swallow
    ///          (≥900) uses its own four-second escalating curve.
    ///</summary>
    class VesselGravityWell : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float DefaultPullSpeedCap = 6.5f;
        const float InnerDeadzone = 70f;

        int ParentIndex => (int)Projectile.ai[0];
        int Duration => (int)Projectile.ai[1] > 0 ? (int)Projectile.ai[1] : 120;
        float PullRadius => Projectile.ai[2] > 0f ? Projectile.ai[2] : 620f;
        bool IsSwallowWell => PullRadius >= 900f;
        float AgeProgress => MathHelper.Clamp((Duration - Projectile.timeLeft) / (float)Duration, 0f, 1f);

        float PullPerTick
        {
            get
            {
                if (!IsSwallowWell)
                    return PullRadius >= 700f ? 0.30f : 0.18f;

                // The first two seconds are deliberately weak enough to fight. During the following
                // two seconds SmoothStep turns the inhale increasingly forceful.
                if (AgeProgress < 0.5f)
                    return MathHelper.Lerp(0.035f, 0.08f, AgeProgress * 2f);

                float ramp = MathHelper.SmoothStep(0f, 1f, (AgeProgress - 0.5f) * 2f);
                return MathHelper.Lerp(0.08f, 0.62f, ramp);
            }
        }

        float PullSpeedCap
        {
            get
            {
                if (!IsSwallowWell)
                    return DefaultPullSpeedCap;

                if (AgeProgress < 0.5f)
                    return MathHelper.Lerp(2f, 3f, AgeProgress * 2f);

                float ramp = MathHelper.SmoothStep(0f, 1f, (AgeProgress - 0.5f) * 2f);
                return MathHelper.Lerp(3f, 9f, ramp);
            }
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 120;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = Duration;
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = (int)(PullRadius * 2f);
            Projectile.Center = center;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            NPC parent = ParentIndex >= 0 && ParentIndex < Main.maxNPCs ? Main.npc[ParentIndex] : null;
            if (parent == null || !parent.active)
            {
                Projectile.Kill();
                return;
            }
            // Pull toward the mouth (bottom of the sprite), rotated with the body.
            Vector2 mouth = parent.Center + new Vector2(0f, parent.height * 0.32f).RotatedBy(parent.rotation);
            Projectile.Center = mouth;
            Projectile.velocity = Vector2.Zero;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead) continue;
                Vector2 toCenter = mouth - player.Center;
                float dist = toCenter.Length();
                if (dist > PullRadius || dist < InnerDeadzone) continue;
                Vector2 dir = toCenter.SafeNormalize(Vector2.Zero);
                float towardSpeed = Vector2.Dot(player.velocity, dir);
                if (towardSpeed < PullSpeedCap)
                {
                    player.velocity += dir * PullPerTick;
                }
            }

            // Keep the fake punish window visually restrained, then intensify the stream with the pull.
            float streamRamp = IsSwallowWell
                ? MathHelper.SmoothStep(0f, 1f, AgeProgress)
                : 1f;
            int streamCount = Main.dedServ ? 0 : IsSwallowWell ? 1 + (int)(streamRamp * 2f) : 2;
            float streamSpeedMin = IsSwallowWell ? MathHelper.Lerp(2.5f, 6f, streamRamp) : 6f;
            float streamSpeedMax = IsSwallowWell ? MathHelper.Lerp(4f, 10f, streamRamp) : 10f;
            for (int i = 0; i < streamCount; i++)
            {
                float ang = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = mouth + ang.ToRotationVector2() * Main.rand.NextFloat(100f, PullRadius * 0.85f);
                int type = Main.rand.NextBool() ? DustID.PurpleTorch : DustID.Shadowflame;
                int dust = Dust.NewDust(pos, 4, 4, type, 0f, 0f, 100, default, 1.1f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = (mouth - pos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(streamSpeedMin, streamSpeedMax);
                Main.dust[dust].fadeIn = 0.3f;
            }
            Lighting.AddLight(mouth, 0.6f, 0.15f, 0.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            VesselVFX.DrawMaw(Projectile.Center, PullRadius, AgeProgress,
                IsSwallowWell && AgeProgress >= 0.5f, InnerDeadzone);
            return false;
        }
    }
}
