using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.Triad;

namespace tsorcRevamp.Content.Projectiles.VFX
{
    public class PlagueTeleportCloud : ModProjectile
    {
        public const int LifetimeTicks = 180;
        public const int SpearImpactLifetimeTicks = 60;
        public const float MaxCloudRadius = 100f;
        public const float SpearImpactCloudRadius = 24f;
        public const int CurseBuildupPerTick = 1;
        private const int BuffRefreshTicks = 120;
        private const int ExpansionTicks = 90;
        private const int DissipateTicks = 45;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(HomingStarStar));

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
            // Capture the spawn lifetime on tick 1 so shorter clouds (like the 60-tick spear impact cloud)
            // scale their expansion and fade phases proportionally instead of spending 45 of 60 ticks fading.
            if (Projectile.localAI[1] == 0f)
            {
                Projectile.localAI[1] = Projectile.timeLeft;
            }

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
            float initialLifetime = Projectile.localAI[1] > 0f ? Projectile.localAI[1] : LifetimeTicks;
            float expansionTicks = System.Math.Min((float)ExpansionTicks, initialLifetime * 0.35f);
            float progress = MathHelper.Clamp(Projectile.localAI[0] / expansionTicks, 0f, 1f);
            progress = progress * progress * (3f - 2f * progress);
            float startRadius = System.Math.Min(16f, TargetRadius * 0.5f);
            return MathHelper.Lerp(startRadius, TargetRadius, progress);
        }

        private void SpawnPlagueDust(float radius)
        {
            float initialLifetime = Projectile.localAI[1] > 0f ? Projectile.localAI[1] : LifetimeTicks;
            float dissipateTicks = System.Math.Min((float)DissipateTicks, initialLifetime * 0.35f);
            float dissipate = MathHelper.Clamp(Projectile.timeLeft / dissipateTicks, 0f, 1f);
            int dustCount = (int)MathHelper.Clamp(22f * dissipate, 6f, 22f);
            float jitterRadius = System.Math.Min(14f, radius * 0.25f);

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                bool outerEdge = Main.rand.NextBool(3);
                float distance = outerEdge ? Main.rand.NextFloat(radius * 0.55f, radius) : Main.rand.NextFloat(radius * 0.1f, radius * 0.8f);
                Vector2 jitter = Main.rand.NextVector2Circular(jitterRadius, jitterRadius);
                Vector2 position = Projectile.Center + direction * distance + jitter;
                Vector2 velocity = direction * Main.rand.NextFloat(0.05f, 0.38f) + Main.rand.NextVector2Circular(0.18f, 0.18f);

                int dustType = DustID.ShadowbeamStaff;
                if (Main.rand.NextBool(2))
                {
                    dustType = DustID.Smoke;
                }
                else if (Main.rand.NextBool(3))
                {
                    dustType = DustID.Wraith;
                }

                Color color = new Color(18, 10, 30);
                if (dustType == DustID.Smoke)
                {
                    color = new Color(12, 8, 18);
                }
                else if (Main.rand.NextBool())
                {
                    color = new Color(70, 24, 112);
                }

                float scale = dustType == DustID.Smoke ? Main.rand.NextFloat(2.2f, 3.6f) : Main.rand.NextFloat(1.3f, 2.1f);
                Dust dust = Dust.NewDustPerfect(position, dustType, velocity, 190, color, scale * dissipate);
                dust.noGravity = true;
                dust.fadeIn = dustType == DustID.Smoke ? 0.9f : 0.45f;

                if (Main.rand.NextBool(3))
                {
                    float pullBack = System.Math.Min(18f, radius * 0.5f);
                    Dust purple = Dust.NewDustPerfect(position - direction * Main.rand.NextFloat(2f, pullBack), DustID.PurpleTorch, velocity * 0.4f, 210, new Color(120, 45, 170), Main.rand.NextFloat(0.8f, 1.35f) * dissipate);
                    purple.noGravity = true;
                }

                // Curse accent, only on clouds that build curse (ai[0] == 1): sparse pink flames inside the smoke.
                if (AppliesCurse && Main.rand.NextBool(6))
                {
                    CurseBuildup.SpawnAccentDust(position, 0, 0, velocity, Main.rand.NextFloat(1.1f, 1.6f) * dissipate);
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

                int buffIndex = player.FindBuffIndex(curseBuff);
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                if (buffIndex == -1 && modPlayer.CurseLevel == 1)
                {
                    modPlayer.CurseLevel = 0;
                }
                modPlayer.CurseLevel += CurseBuildupPerTick;

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
