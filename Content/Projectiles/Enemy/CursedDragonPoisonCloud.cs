using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Enemy.GravelordNito;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// The Cursed Dragon's poison-cloud flee-counter payload (PoisonCloudFleeCounter). A large
    /// STATIONARY gas cloud, not a travelling puff — reuses NitoVFX.DrawMiasma (Gravelord Nito's gas
    /// shader) at a much bigger, fixed-in-place scale instead. Debuff-only: no direct hit damage, so
    /// the threat is entirely "don't stand in it" — every player inside gets Poisoned refreshed and
    /// PowerfulCurseBuildup reapplied on an interval, so lingering is what actually punishes.
    /// </summary>
    public class CursedDragonPoisonCloud : ModProjectile
    {
        // Invisible-sprite convention (see PuppetMeleeHitbox): drawn entirely through PreDraw's
        // shader, so this points at the same shared placeholder rather than needing its own art.
        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        private const float FullRadius = 160f; // 10 tiles
        private const int GrowTicks = 30;
        private const int FadeTicks = 50;
        private const int TotalLifetime = 280;
        private const int PoisonRefreshTicks = 30 * 60; // 30 seconds, refreshed every tick while inside
        private const int CurseReapplyInterval = 30;     // reapply the curse-buildup stack every 0.5s inside

        public override void SetDefaults()
        {
            Projectile.width = (int)(FullRadius * 2f);
            Projectile.height = (int)(FullRadius * 2f);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalLifetime;
            Projectile.aiStyle = 0;
            Projectile.alpha = 255;
        }

        // Debuff-only hazard: no direct hit damage, so this never enters the ordinary hit/knockback
        // pipeline - the AI() loop below applies its effects directly to every player in range.
        public override bool? CanDamage() => false;

        private float CurrentRadius()
        {
            int elapsed = TotalLifetime - Projectile.timeLeft;
            if (elapsed < GrowTicks)
            {
                return FullRadius * MathHelper.SmoothStep(0f, 1f, elapsed / (float)GrowTicks);
            }
            if (Projectile.timeLeft < FadeTicks)
            {
                return FullRadius * MathHelper.SmoothStep(0f, 1f, Projectile.timeLeft / (float)FadeTicks);
            }
            return FullRadius;
        }

        public override void AI()
        {
            Projectile.localAI[0] += 1f; // ticks since spawn - drives both the shader's progress read and the curse cadence below
            Projectile.rotation += 0.01f; // slow churn, matches Nito's own miasma drift
            float radius = CurrentRadius();

            if (Main.rand.NextBool(2))
            {
                Vector2 dustPos = Projectile.Center + Main.rand.NextVector2CircularEdge(radius * 0.9f, radius * 0.9f);
                Dust d = Dust.NewDustPerfect(dustPos, DustID.Poisoned, Main.rand.NextVector2Circular(0.6f, 0.6f), 100, default, Main.rand.NextFloat(1.6f, 2.6f));
                d.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.12f, 0.22f, 0.08f);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Debuffs apply per-player independently - every peer is authoritative for its own
            // player, same "force zone" convention as the mod's other lingering hazards.
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }
                if (Vector2.Distance(player.Center, Projectile.Center) > radius)
                {
                    continue;
                }

                player.AddBuff(BuffID.Poisoned, PoisonRefreshTicks);
                if ((int)Projectile.localAI[0] % CurseReapplyInterval == 0)
                {
                    player.AddBuff(ModContent.BuffType<PowerfulCurseBuildup>(), 36000);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float radius = CurrentRadius();
            float progress = MathHelper.Clamp(Projectile.localAI[0] / TotalLifetime, 0f, 1f);
            float opacity = MathHelper.Clamp(radius / FullRadius, 0f, 1f) * 0.9f;
            NitoVFX.DrawMiasma(Projectile.Center, new Vector2(radius * 2f, radius * 2f), Projectile.rotation,
                progress, opacity, Projectile.identity * 0.37f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.4f, Pitch = 0.2f }, Projectile.Center);
        }
    }
}
