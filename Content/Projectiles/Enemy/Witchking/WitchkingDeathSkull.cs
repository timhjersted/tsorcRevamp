using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.Witchking
{
    /// <summary>
    /// The Unmaking's skulls: fifty burst out of the Witchking as he dies. Loops the Sudden Death Strike
    /// sprite's 12-frame animation (intentional sprite sharing), travels 50 tiles, then fades out over 15 ticks with a
    /// dust puff. Passes through tiles deliberately: it is a death wave through the cave. Piercing, so a hit never needs a
    /// local Kill (which would only happen on the hit player's machine in MP); post-hit immunity stops repeats.
    /// Each hit adds 20 Blight buildup, on the hit player's machine.
    /// </summary>
    public class WitchkingDeathSkull : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemySpellSuddenDeathStrike));

        public const float Speed = 7f;
        public const int BlightBuildupPerHit = 20;
        private const float TravelDistance = 50f * 16f;
        private const int FadeTicks = 15;
        private const int FrameCount = 12;
        private const int TicksPerFrame = 4;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 44;
            Projectile.height = 40;
            DrawOriginOffsetX = 15;
            DrawOriginOffsetY = 10;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = (int)(TravelDistance / Speed) + FadeTicks;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.5f;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            // Loop the animation instead of dying when it ends (the source projectile's one-shot lifetime).
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            if (Projectile.timeLeft <= FadeTicks)
            {
                Projectile.alpha = (int)MathHelper.Lerp(255f, 0f, Projectile.timeLeft / (float)FadeTicks);
            }

            if (!Main.dedServ)
            {
                Dust wisp = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Wraith,
                    Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.2f);
                wisp.noGravity = true;
                wisp.velocity *= 0.3f;
                Lighting.AddLight(Projectile.Center, 0.3f, 0.1f, 0.45f);
            }
        }

        public override bool? CanDamage() => Projectile.timeLeft > FadeTicks ? null : false;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            BlightBuildup.Apply(target, BlightBuildupPerHit);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            EnemyVFX.DrawBlackKnightDeathTrail(Projectile.Center, Projectile.velocity, new Vector2(92f, 34f),
                0.76f * (1f - Projectile.alpha / 255f));
            return true;
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * (1f - Projectile.alpha / 255f);

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 12; i++)
            {
                Dust puff = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Wraith, 0f, 0f, 80, default, 1.3f);
                puff.velocity = Main.rand.NextVector2Circular(2.5f, 2.5f);
                puff.noGravity = true;
            }
        }
    }
}
