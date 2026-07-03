using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Cursed Dragon's thrown knife.  Arcs out, sticks into the first tile it hits, swells with
    /// black/purple dust for ~1.5 s, then bursts into a lingering plague cloud (the same
    /// <see cref="PlagueTeleportCloud"/> the Black Knight's plague teleport uses) that applies Curse
    /// buildup, and slaps 10 s of Darkness on anyone caught in the bloom.
    /// </summary>
    public class CursedDragonKnife : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.ThrowingKnife;

        private const int StuckLifetime = 90;   // 1.5 s embedded before the burst
        private const float CloudRadius = 100f;
        private const int DarknessTicks = 600;  // 10 s

        // ai[0]: 0 = flying, 1 = embedded.  ai[1]: counts up while embedded.
        private ref float Stuck => ref Projectile.ai[0];
        private ref float StuckTimer => ref Projectile.ai[1];

        // When the knife embeds in a PLAYER instead of a tile, it rides along with them
        // (offset frozen at the moment of impact) until it bursts.  Not synced via ai[] since
        // it's a purely cosmetic follow — harmless if it desyncs a frame in multiplayer.
        private int _stuckPlayerIndex = -1;
        private Vector2 _stuckPlayerOffset;

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600; // a knife that never sticks eventually despawns
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            if (Stuck == 0f)
            {
                // Flying: light gravity arc so it sinks into the terrain near the player.
                Projectile.velocity.Y += 0.18f;
                if (Projectile.velocity.Y > 16f) Projectile.velocity.Y = 16f;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
                EmitDust(0.6f);
            }
            else
            {
                // Stuck in a player: ride along at the offset frozen at the moment of impact.
                if (_stuckPlayerIndex >= 0)
                {
                    Player rider = Main.player[_stuckPlayerIndex];
                    if (rider.active && !rider.dead)
                        Projectile.Center = rider.Center + _stuckPlayerOffset;
                    else
                        _stuckPlayerIndex = -1; // player gone — just hover at the last spot
                }

                // Embedded: dust swells to max size quickly (quadratic ease-out over the first
                // quarter of the fuse), then holds at max size for the remainder — a clean, readable
                // "building cloud" instead of a slow randomness-heavy ramp for the whole fuse.
                StuckTimer++;
                const float riseFrac = 0.25f;
                float riseTicks = StuckLifetime * riseFrac;
                float rise = riseTicks > 0f ? MathHelper.Clamp(StuckTimer / riseTicks, 0f, 1f) : 1f;
                rise = 1f - (1f - rise) * (1f - rise); // quadratic ease-out
                EmitDust(0.5f + rise * 1.8f);

                // Light fire-spark dust while the fuse burns, like a lit fuse — gravity stays on
                // (default) so the sparks flicker up then fall, rather than hanging in a cloud.
                if (!Main.dedServ && Main.rand.NextBool(4))
                {
                    Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), DustID.Torch,
                        new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-1.6f, -0.4f)),
                        0, default, Main.rand.NextFloat(0.8f, 1.2f));
                }

                Lighting.AddLight(Projectile.Center, 0.25f * (0.4f + rise), 0.05f, 0.35f * (0.4f + rise));
                if (StuckTimer >= StuckLifetime)
                {
                    Burst();
                    Projectile.Kill();
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Stuck == 0f)
            {
                Stuck = 1f;
                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                Projectile.netUpdate = true;
                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.NPCHit11 with { Volume = 0.5f, PitchVariance = 0.1f }, Projectile.Center);
            }
            return false; // stick instead of dying
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Stuck != 0f) return; // already embedded — ignore any further contact
            Stuck = 1f;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.netUpdate = true;
            _stuckPlayerIndex = target.whoAmI;
            _stuckPlayerOffset = Projectile.Center - target.Center;
            if (!Main.dedServ)
                SoundEngine.PlaySound(SoundID.NPCHit11 with { Volume = 0.5f, PitchVariance = 0.1f }, Projectile.Center);
        }

        private void EmitDust(float intensity)
        {
            if (Main.dedServ) return;
            int count = 1 + (int)(intensity * 2f);
            for (int i = 0; i < count; i++)
            {
                bool purple = Main.rand.NextBool();
                int type = purple ? DustID.PurpleTorch : DustID.Smoke;
                Color color = purple ? new Color(120, 45, 170) : new Color(14, 8, 20);
                Dust d = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), type,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), purple ? 180 : 200, color, Main.rand.NextFloat(0.8f, 1.4f) * intensity);
                d.noGravity = true;
            }
        }

        private void Burst()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item109 with { Volume = 0.5f, Pitch = -0.1f, PitchVariance = 0.08f }, Projectile.Center);

                // Standard bomb-style smoke cloud (vanilla's own bomb explosion gore IDs).
                for (int i = 0; i < 3; i++)
                {
                    Gore.NewGore(Projectile.GetSource_Death(), Projectile.position, Vector2.Zero, Main.rand.Next(61, 64));
                }

                // Flame sparks at the outset of the burst only — gravity stays ON so they arc
                // outward and fall like embers, rather than hanging in the air like the plague cloud.
                for (int i = 0; i < 10; i++)
                {
                    Dust.NewDustPerfect(Projectile.Center, DustID.Torch,
                        Main.rand.NextVector2Circular(4.5f, 4.5f) + new Vector2(0f, -1.5f),
                        0, default, Main.rand.NextFloat(1.2f, 1.8f));
                }
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Plague cloud — identical to the plague teleport (ai0=1 → curse buildup, ai1 = radius).
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<PlagueTeleportCloud>(), 0, 0f, Main.myPlayer, 1f, CloudRadius);

            // 10 s Darkness to anyone caught in the bloom at the moment it bursts.
            float rSq = CloudRadius * CloudRadius;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player pl = Main.player[i];
                if (pl.active && !pl.dead && Vector2.DistanceSquared(pl.Center, Projectile.Center) <= rSq)
                {
                    pl.AddBuff(BuffID.Darkness, DarknessTicks);
                }
            }
        }
    }
}
