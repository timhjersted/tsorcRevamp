using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.VFX
{
    // Great Black Knight's teleport cloud. Both departure and destination are Blight hazards.
    public class BlightTeleportCloud : ModProjectile
    {
        public const int LifetimeTicks = 180;
        public const float MaxCloudRadius = 100f;
        public const int BuildupIntervalTicks = 2;
        private const int ExpansionTicks = 90;
        private const int DissipateTicks = 45;

        public override string Texture => "Terraria/Images/Projectile_0";
        private float TargetRadius => Projectile.ai[1] > 0f ? Projectile.ai[1] : MaxCloudRadius;
        private bool IsSpearImpact => Projectile.ai[2] == 1f;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            float radius = CurrentRadius();
            if (!Main.dedServ) SpawnDust(radius);
            // No entry bonus: one point on ticks 2, 4, 6... while overlapping either cloud.
            if (Main.netMode != NetmodeID.MultiplayerClient
                && (int)Projectile.localAI[0] % BuildupIntervalTicks == 0)
                ApplyBuildup(radius);
        }

        private float CurrentRadius()
        {
            float progress = MathHelper.Clamp(Projectile.localAI[0] / ExpansionTicks, 0f, 1f);
            progress = progress * progress * (3f - 2f * progress);
            return MathHelper.Lerp(16f, TargetRadius, progress);
        }

        private void ApplyBuildup(float radius)
        {
            float radiusSquared = radius * radius;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || !PlayerTouchesCloud(player, radiusSquared)) continue;
                BlightBuildup.Apply(player, 1);
            }
        }

        private bool PlayerTouchesCloud(Player player, float radiusSquared)
        {
            Rectangle hitbox = player.Hitbox;
            float closestX = MathHelper.Clamp(Projectile.Center.X, hitbox.Left, hitbox.Right);
            float closestY = MathHelper.Clamp(Projectile.Center.Y, hitbox.Top, hitbox.Bottom);
            return Vector2.DistanceSquared(Projectile.Center, new Vector2(closestX, closestY)) <= radiusSquared;
        }

        private void SpawnDust(float radius)
        {
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)DissipateTicks, 0f, 1f);
            int count = (int)MathHelper.Lerp(5f, 19f, fade);
            for (int i = 0; i < count; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                float distance = Main.rand.NextFloat(0.08f, 1f) * radius;
                Vector2 position = Projectile.Center + direction * distance + Main.rand.NextVector2Circular(8f, 8f);
                Vector2 velocity = direction * Main.rand.NextFloat(0.08f, 0.4f)
                    + Main.rand.NextVector2Circular(0.18f, 0.18f);
                bool smoke = i % 5 == 0;
                Dust dust = Dust.NewDustPerfect(position, smoke ? DustID.Smoke : DustID.Wraith,
                    velocity, smoke ? 175 : 130, smoke ? new Color(28, 26, 30) : new Color(65, 61, 70),
                    Main.rand.NextFloat(smoke ? 1.2f : 1.05f, smoke ? 1.9f : 1.75f) * fade);
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(4))
            {
                Vector2 point = Projectile.Center + Main.rand.NextVector2Circular(radius, radius);
                Dust firefly = Dust.NewDustPerfect(point, IsSpearImpact ? DustID.YellowTorch : DustID.Firefly,
                    Main.rand.NextVector2Circular(0.3f, 0.3f), 100,
                    new Color(255, 210, 95), Main.rand.NextFloat(0.65f, 1f) * fade);
                firefly.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < 14; i++)
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(TargetRadius, TargetRadius),
                    DustID.Wraith, Main.rand.NextVector2Circular(0.4f, 0.4f), 190,
                    new Color(65, 61, 70), Main.rand.NextFloat(0.45f, 0.85f)).noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor) => false;
        public override bool? CanDamage() => false;
    }
}
