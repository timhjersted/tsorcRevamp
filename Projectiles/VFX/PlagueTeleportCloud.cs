using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Projectiles.VFX
{
    public class PlagueTeleportCloud : ModProjectile
    {
        public const int LifetimeTicks = 180;
        public const float MaxCloudRadius = 100f;
        public const int CurseBuildupPerTick = 2;
        private const int BuffRefreshTicks = 120;
        private const int ExpansionTicks = 90;
        private const int DissipateTicks = 45;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        private bool AppliesCurse => Projectile.ai[0] == 1f;
        private float TargetRadius => Projectile.ai[1] > 0f ? Projectile.ai[1] : MaxCloudRadius;

        public override void SetDefaults()
        {
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            float radius = CurrentRadius();
            if (!Main.dedServ)
            {
                SpawnPlagueDust(radius);
            }

            if (AppliesCurse && Main.netMode != NetmodeID.MultiplayerClient)
            {
                ApplyCurseBuildup(radius);
            }
        }

        private float CurrentRadius()
        {
            float progress = MathHelper.Clamp(Projectile.localAI[0] / ExpansionTicks, 0f, 1f);
            progress = progress * progress * (3f - 2f * progress);
            return MathHelper.Lerp(16f, TargetRadius, progress);
        }

        private void SpawnPlagueDust(float radius)
        {
            float dissipate = MathHelper.Clamp(Projectile.timeLeft / (float)DissipateTicks, 0f, 1f);
            int dustCount = (int)MathHelper.Clamp(22f * dissipate, 6f, 22f);

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                bool outerEdge = Main.rand.NextBool(3);
                float distance = outerEdge ? Main.rand.NextFloat(radius * 0.55f, radius) : Main.rand.NextFloat(radius * 0.1f, radius * 0.8f);
                Vector2 jitter = Main.rand.NextVector2Circular(14f, 14f);
                Vector2 position = Projectile.Center + direction * distance + jitter;
                Vector2 velocity = direction * Main.rand.NextFloat(0.05f, 0.38f) + Main.rand.NextVector2Circular(0.18f, 0.18f);

                int dustType = Main.rand.NextBool(2) ? DustID.Smoke : (Main.rand.NextBool(3) ? DustID.Wraith : DustID.ShadowbeamStaff);
                Color color = dustType == DustID.Smoke
                    ? new Color(12, 8, 18)
                    : (Main.rand.NextBool() ? new Color(70, 24, 112) : new Color(18, 10, 30));
                float scale = dustType == DustID.Smoke ? Main.rand.NextFloat(2.2f, 3.6f) : Main.rand.NextFloat(1.3f, 2.1f);
                Dust dust = Dust.NewDustPerfect(position, dustType, velocity, 190, color, scale * dissipate);
                dust.noGravity = true;
                dust.fadeIn = dustType == DustID.Smoke ? 0.9f : 0.45f;

                if (Main.rand.NextBool(3))
                {
                    Dust purple = Dust.NewDustPerfect(position - direction * Main.rand.NextFloat(4f, 18f), DustID.PurpleTorch, velocity * 0.4f, 210, new Color(120, 45, 170), Main.rand.NextFloat(0.8f, 1.35f) * dissipate);
                    purple.noGravity = true;
                }
            }
        }

        private void ApplyCurseBuildup(float radius)
        {
            float radiusSquared = radius * radius;
            int curseBuff = ModContent.BuffType<CurseBuildup>();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead || !PlayerTouchesCloud(player, radiusSquared))
                {
                    continue;
                }

                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                modPlayer.CurseLevel += CurseBuildupPerTick;

                int buffIndex = player.FindBuffIndex(curseBuff);
                if (buffIndex == -1)
                {
                    player.AddBuff(curseBuff, BuffRefreshTicks, false);
                }
                else if (player.buffTime[buffIndex] < BuffRefreshTicks)
                {
                    player.buffTime[buffIndex] = BuffRefreshTicks;
                }

                if (modPlayer.CurseLevel >= 100)
                {
                    CurseBuildup.TriggerCurse(player);
                }
            }
        }

        private bool PlayerTouchesCloud(Player player, float radiusSquared)
        {
            Rectangle hitbox = player.Hitbox;
            float closestX = MathHelper.Clamp(Projectile.Center.X, hitbox.Left, hitbox.Right);
            float closestY = MathHelper.Clamp(Projectile.Center.Y, hitbox.Top, hitbox.Bottom);

            return Vector2.DistanceSquared(Projectile.Center, new Vector2(closestX, closestY)) <= radiusSquared;
        }

        public override bool PreDraw(ref Color lightColor) => false;
        public override bool? CanDamage() => false;
    }
}
