using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Ornsmough
{
    public enum WardenBoltMode
    {
        Bolt,          // ai1 = lifetime ticks
        Crawler,       // velocity.X = signed speed, ai1 = reach tiles, ai2 = armed-after ticks
        SkyStrike,     // bottom-centre on the floor; ai1 = mark ticks, ai2 = column width in tiles
        Annulus,       // ai1 = max radius, ai2 = start radius * 100 + speed
        Crescent,      // ai1 = range px
        Rock,          // gravity lob; ai1 / ai2 = landing mark x / y
        ChargedRock,   // Rock + a lightning puff on landing
        Lance,         // sticks in a tile, then detonates
        Shock,         // ai1 = radius, ai2 = live ticks
        Constellation, // Center = sky anchor; ai1 = player, ai2 = star count * 1000 + boss whoAmI
        QuakePulse,    // ai1 = boss feet y, ai2 = boss x
        GolemShatter,  // ai1 = dash direction
        Burst,         // ai1 = 1 gold column, 0 gold ring; ai2 = shake strength
    }

    /// <summary>
    /// Every projectile of the Ornstein and Smough fight, one per ai[0] mode. Hitboxes come from Colliding so one fixed
    /// projectile size serves every mode (size and collision flags are not synced). All visuals are dust plus jagged
    /// MagicPixel lightning: the leading edge of a directional bolt is its velocity.
    /// </summary>
    public class WardenLightning : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public const int LanceColumnExpertDamage = 170;
        public const int LanceCrawlerExpertDamage = 130;
        public const int ChargedRockPuffExpertDamage = 130;
        public const float HighPlayerRise = 25f * 16f;

        private const int BoltFadeTicks = 12;
        private const int CrescentFadeTicks = 15;
        private const int StrikeLiveTicks = 6;
        private const int StrikeLingerTicks = 10;
        private const float AnnulusHalfBand = 22f;
        private const float RockGravity = 0.25f;
        private const int LanceFuseTicks = 40;
        private const int QuakeCrippleTicks = 8 * 60;

        // Constellation timing (spec B8): fade 15t, lines to t45, wave k locks at 45 + 14k and fires 10t later.
        private const int StarFadeTicks = 15;
        public const int ConstellationLinesEnd = 45;
        public const int ConstellationWaveGap = 14;
        private const int ConstellationFireDelay = 10;
        private const float StarBoltSpeed = 14f;
        private const int StarBoltLife = 60;

        private static readonly Color GoldCore = new Color(255, 236, 170, 0);
        private static readonly Color GoldEdge = new Color(255, 190, 70, 0);
        private static readonly Color StormBlue = new Color(150, 205, 255, 0);

        private WardenBoltMode Mode => (WardenBoltMode)(int)Projectile.ai[0];
        private ref float Age => ref Projectile.localAI[0];
        private ref float StuckFlag => ref Projectile.localAI[1];

        private Vector2[] lockedStarAims;
        private bool[] waveFizzled;

        /// <summary>Server only. expertDamage 0 makes a visual / utility projectile.</summary>
        public static int Spawn(IEntitySource source, Vector2 position, Vector2 velocity, WardenBoltMode mode, int expertDamage, float ai1 = 0f, float ai2 = 0f)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return -1;
            }

            int damage = 0;
            if (expertDamage > 0)
            {
                damage = EnemyDamage.Projectile(expertDamage);
            }

            return Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<WardenLightning>(), damage, 3f, Main.myPlayer, (float)mode, ai1, ai2);
        }

        /// <summary>Server only: one floor crawler each way from a floor point near startX.</summary>
        public static void SpawnCrawlerPair(IEntitySource source, float startX, float aroundY, float speed, int reachTiles, int armedAfterTicks, int expertDamage)
        {
            for (int direction = -1; direction <= 1; direction += 2)
            {
                float spawnX = startX + direction * 14f;
                if (!PuppetGroundDustWave.TryFindGroundY(spawnX, aroundY, out float groundY))
                {
                    continue;
                }

                Vector2 position = new Vector2(spawnX, groundY - 10f);
                Spawn(source, position, new Vector2(direction * speed, 0f), WardenBoltMode.Crawler, expertDamage, reachTiles, armedAfterTicks);
            }
        }

        /// <summary>Server only: a ground mark that strikes after markTicks, skipped over a pit.</summary>
        public static void SpawnSkyStrike(IEntitySource source, float worldX, float aroundY, int markTicks, int widthTiles, int expertDamage)
        {
            if (PuppetGroundDustWave.TryFindGroundY(worldX, aroundY, out float groundY))
            {
                Spawn(source, new Vector2(worldX, groundY), Vector2.Zero, WardenBoltMode.SkyStrike, expertDamage, markTicks, widthTiles);
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 900;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Age++;

            switch (Mode)
            {
                case WardenBoltMode.Bolt:
                    TickBolt();
                    break;
                case WardenBoltMode.Crawler:
                    TickCrawler();
                    break;
                case WardenBoltMode.SkyStrike:
                    TickSkyStrike();
                    break;
                case WardenBoltMode.Annulus:
                    TickAnnulus();
                    break;
                case WardenBoltMode.Crescent:
                    TickCrescent();
                    break;
                case WardenBoltMode.Rock:
                case WardenBoltMode.ChargedRock:
                    TickRock();
                    break;
                case WardenBoltMode.Lance:
                    TickLance();
                    break;
                case WardenBoltMode.Shock:
                    TickShock();
                    break;
                case WardenBoltMode.Constellation:
                    TickConstellation();
                    break;
                case WardenBoltMode.QuakePulse:
                    TickQuakePulse();
                    break;
                case WardenBoltMode.GolemShatter:
                    TickGolemShatter();
                    break;
                case WardenBoltMode.Burst:
                    TickBurst();
                    break;
            }
        }

        private bool HitsTileHere()
        {
            return Collision.SolidCollision(Projectile.Center - new Vector2(4f), 8, 8);
        }

        private void SpawnTrailDust(int dustType, float chanceDenominator, float scale)
        {
            if (Main.dedServ || !Main.rand.NextBool((int)chanceDenominator))
            {
                return;
            }

            Dust trail = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), dustType, -Projectile.velocity * 0.12f, 60, default, scale);
            trail.noGravity = true;
        }

        private void TickBolt()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.6f, 0.55f, 0.3f);
            SpawnTrailDust(DustID.Electric, 2f, 0.8f);

            float life = Projectile.ai[1];
            if (HitsTileHere() || Age >= life + BoltFadeTicks)
            {
                Projectile.Kill();
            }
        }

        private void TickCrawler()
        {
            int direction = Math.Sign(Projectile.velocity.X);
            float speed = Math.Abs(Projectile.velocity.X);
            float lifeTicks = Projectile.ai[1] * 16f / Math.Max(1f, speed);

            if (!SnapCrawlerToGround(direction) || Age >= lifeTicks)
            {
                Projectile.Kill();
                return;
            }

            Lighting.AddLight(Projectile.Center, 0.5f, 0.45f, 0.2f);
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 dustPoint = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-12f, 12f), -3f);
                Dust spark = Dust.NewDustPerfect(dustPoint, DustID.Electric, new Vector2(direction, Main.rand.NextFloat(-2.2f, -0.6f)), 80, default, 0.85f);
                spark.noGravity = true;
            }
        }

        // Hugs the floor: climbs up to 3 tiles, drops up to 5, otherwise the crawler ends at the wall or pit.
        private bool SnapCrawlerToGround(int direction)
        {
            int tileX = (int)((Projectile.Center.X + direction * 10f) / 16f);
            int tileY = (int)((Projectile.Bottom.Y - 8f) / 16f);
            bool outOfWorld = tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5 || tileY > Main.maxTilesY - 10;
            if (outOfWorld)
            {
                return false;
            }

            int climb = 0;
            while (WorldGen.SolidTile(tileX, tileY) && climb <= 3)
            {
                tileY--;
                climb++;
            }

            if (climb > 3)
            {
                return false;
            }

            int drop = 0;
            while (!WorldGen.SolidTile(tileX, tileY + 1) && drop <= 5)
            {
                tileY++;
                drop++;
            }

            if (drop > 5)
            {
                return false;
            }

            Projectile.position.Y = (tileY + 1) * 16f - Projectile.height;
            return true;
        }

        private int StrikeHeightTiles
        {
            get
            {
                if (Projectile.ai[2] >= 3f)
                {
                    return 14;
                }

                return 12;
            }
        }

        private void TickSkyStrike()
        {
            int markTicks = (int)Projectile.ai[1];
            float halfWidth = Projectile.ai[2] * 8f;

            if (!Main.dedServ && Age < markTicks && (int)Age % 3 == 0)
            {
                Vector2 floorPoint = Projectile.Center + new Vector2(Main.rand.NextFloat(-halfWidth, halfWidth), -2f);
                Dust mark = Dust.NewDustPerfect(floorPoint, DustID.GoldFlame, new Vector2(0f, -1.1f), 80, default, 1f);
                mark.noGravity = true;
            }

            if (Age == markTicks && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.45f, Pitch = 0.2f, MaxInstances = 4 }, Projectile.Center);
                for (int i = 0; i < 22; i++)
                {
                    Vector2 point = Projectile.Center + new Vector2(Main.rand.NextFloat(-halfWidth, halfWidth), -Main.rand.NextFloat(StrikeHeightTiles * 16f));
                    Dust bolt = Dust.NewDustPerfect(point, DustID.Electric, Main.rand.NextVector2Circular(2f, 2f), 40, default, 1.1f);
                    bolt.noGravity = true;
                }
            }

            if (Age >= markTicks + StrikeLiveTicks + StrikeLingerTicks)
            {
                Projectile.Kill();
            }
        }

        private float AnnulusRadius
        {
            get
            {
                float encoded = Projectile.ai[2];
                float startRadius = (float)Math.Floor(encoded / 100f);
                float speed = encoded - startRadius * 100f;
                return startRadius + speed * Age;
            }
        }

        private void TickAnnulus()
        {
            float radius = AnnulusRadius;
            if (!Main.dedServ)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector2 rimPoint = Projectile.Center + Main.rand.NextVector2CircularEdge(radius, radius);
                    Dust rim = Dust.NewDustPerfect(rimPoint, DustID.Electric, Vector2.Zero, 60, default, 1f);
                    rim.noGravity = true;
                }
            }

            if (radius >= Projectile.ai[1])
            {
                Projectile.Kill();
            }
        }

        private void TickCrescent()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            SpawnTrailDust(DustID.Electric, 1f, 0.9f);

            float lifeTicks = Projectile.ai[1] / Math.Max(1f, Projectile.velocity.Length());
            if (Age >= lifeTicks + CrescentFadeTicks)
            {
                Projectile.Kill();
            }
        }

        private void TickRock()
        {
            Projectile.velocity.Y += RockGravity;
            Projectile.rotation += 0.2f * Math.Sign(Projectile.velocity.X);
            SpawnTrailDust(DustID.Stone, 2f, 1f);

            if (!Main.dedServ && (int)Age % 4 == 0)
            {
                Vector2 markPoint = new Vector2(Projectile.ai[1] + Main.rand.NextFloat(-22f, 22f), Projectile.ai[2] - 2f);
                Dust mark = Dust.NewDustPerfect(markPoint, DustID.GoldFlame, new Vector2(0f, -0.8f), 80, default, 0.9f);
                mark.noGravity = true;
            }

            bool landed = Age > 8f && Projectile.velocity.Y > 0f && HitsTileHere();
            if (!landed)
            {
                return;
            }

            if (Mode == WardenBoltMode.ChargedRock)
            {
                Spawn(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, WardenBoltMode.Shock, ChargedRockPuffExpertDamage, 32f, StrikeLiveTicks);
            }

            Projectile.Kill();
        }

        private void TickLance()
        {
            bool stuck = StuckFlag > 0f;
            if (!stuck)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                SpawnTrailDust(DustID.Electric, 1f, 1f);

                if (HitsTileHere())
                {
                    StuckFlag = 1f;
                    Age = 0f;
                    Projectile.velocity = Vector2.Zero;
                    SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.7f, Pitch = 0.3f }, Projectile.Center);
                }

                return;
            }

            // The crackle speeds up across the fuse, then a column and two crawlers replace the lance.
            int crackleInterval = (int)MathHelper.Lerp(8f, 2f, Age / LanceFuseTicks);
            if (!Main.dedServ && (int)Age % Math.Max(1, crackleInterval) == 0)
            {
                Dust crackle = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, Main.rand.NextVector2Circular(3f, 3f), 40, default, 1.1f);
                crackle.noGravity = true;
            }

            if (Age >= LanceFuseTicks)
            {
                IEntitySource source = Projectile.GetSource_FromThis();
                SpawnSkyStrike(source, Projectile.Center.X, Projectile.Center.Y, 0, 3, LanceColumnExpertDamage);
                SpawnCrawlerPair(source, Projectile.Center.X, Projectile.Center.Y, 9f, 18, 10, LanceCrawlerExpertDamage);
                Projectile.Kill();
            }
        }

        private void TickShock()
        {
            if (Age >= Projectile.ai[2] + 6f)
            {
                Projectile.Kill();
            }
        }

        private void TickQuakePulse()
        {
            if (Age == 1f && !Main.dedServ)
            {
                Vector2 quakeCenter = new Vector2(Projectile.ai[2], Projectile.ai[1]);
                UsefulFunctions.ScreenShake(quakeCenter, 16f, 40, distanceFalloff: 3000f);
                SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 1f, Pitch = -0.5f }, quakeCenter);

                // Player-local: every client cripples only its own player, flat 8s in every mode. The fall still hurts.
                Player local = Main.LocalPlayer;
                float rise = Projectile.ai[1] - local.Bottom.Y;
                bool nearby = Math.Abs(local.Center.X - Projectile.ai[2]) <= 100f * 16f;
                if (local.active && !local.dead && nearby && rise >= HighPlayerRise)
                {
                    local.AddBuff(ModContent.BuffType<Crippled>(), QuakeCrippleTicks, false);
                    for (int i = 0; i < 30; i++)
                    {
                        Dust.NewDustPerfect(local.Center + Main.rand.NextVector2Circular(18f, 24f), DustID.Stone, Main.rand.NextVector2Circular(3f, 3f), 0, default, 1.2f);
                    }
                }
            }

            if (Age >= 2f)
            {
                Projectile.Kill();
            }
        }

        private void TickGolemShatter()
        {
            if (Age == 1f && !Main.dedServ)
            {
                int[] pieces = { 360, 361, 362, 363, 364, 366, 367 };
                int[] debris = { 368, 370 };
                float direction = Projectile.ai[1];
                IEntitySource source = Projectile.GetSource_FromThis();

                foreach (int goreType in pieces)
                {
                    SpawnGolemGore(source, goreType, direction);
                }

                for (int i = 0; i < 6; i++)
                {
                    SpawnGolemGore(source, debris[i % debris.Length], direction);
                }

                for (int i = 0; i < 80; i++)
                {
                    int dustType = DustID.Electric;
                    if (i % 2 == 0)
                    {
                        dustType = DustID.OrangeTorch;
                    }

                    Dust burst = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(60f, 60f), dustType, Main.rand.NextVector2Circular(7f, 7f), 0, default, 1.5f);
                    burst.noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.9f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);
                UsefulFunctions.ScreenShake(Projectile.Center, 12f, 26, distanceFalloff: 2400f);
            }

            if (Age >= 2f)
            {
                Projectile.Kill();
            }
        }

        private void SpawnGolemGore(IEntitySource source, int goreType, float direction)
        {
            Vector2 goreVelocity = new Vector2(direction * Main.rand.NextFloat(6f, 10f), Main.rand.NextFloat(-5f, 1f));
            Gore.NewGore(source, Projectile.Center + Main.rand.NextVector2Circular(50f, 50f), goreVelocity, goreType);
        }

        private void TickBurst()
        {
            if (Age == 1f && !Main.dedServ)
            {
                bool column = Projectile.ai[1] > 0f;
                for (int i = 0; i < 70; i++)
                {
                    int dustType = DustID.GoldFlame;
                    if (i % 2 == 0)
                    {
                        dustType = DustID.AncientLight;
                    }

                    Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                    Vector2 point = Projectile.Center;
                    if (column)
                    {
                        point += new Vector2(Main.rand.NextFloat(-22f, 22f), Main.rand.NextFloat(-90f, 30f));
                        velocity = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-5f, -1f));
                    }

                    Dust burst = Dust.NewDustPerfect(point, dustType, velocity, 0, default, 1.5f);
                    burst.noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.9f, Pitch = -0.2f }, Projectile.Center);
                if (Projectile.ai[2] > 0f)
                {
                    UsefulFunctions.ScreenShake(Projectile.Center, Projectile.ai[2], 20, distanceFalloff: 2400f);
                }
            }

            if (Age >= 2f)
            {
                Projectile.Kill();
            }
        }

        // ── Constellation ───────────────────────────────────────────────────────────
        private int StarCount => (int)(Projectile.ai[2] / 1000f);
        private int BossIndex => (int)Projectile.ai[2] % 1000;
        private int WaveCount => StarCount / 4;

        private Vector2 StarPosition(int starIndex)
        {
            int perRing = StarCount / 2;
            bool outer = starIndex < perRing;
            float innerRadius = 300f;
            float outerRadius = 420f;
            if (StarCount > 16)
            {
                innerRadius = 330f;
                outerRadius = 460f;
            }

            float angleStep = MathHelper.TwoPi / perRing;
            if (outer)
            {
                return Projectile.Center + (angleStep * starIndex).ToRotationVector2() * outerRadius;
            }

            float innerAngle = angleStep * (starIndex - perRing) + angleStep * 0.5f;
            return Projectile.Center + innerAngle.ToRotationVector2() * innerRadius;
        }

        private int WaveLockTick(int wave)
        {
            return ConstellationLinesEnd + ConstellationWaveGap * wave;
        }

        private bool ConstellationTargetStillHigh(Player target)
        {
            NPC boss = Main.npc[BossIndex];
            if (!boss.active || !target.active || target.dead)
            {
                return false;
            }

            return boss.Bottom.Y - target.Bottom.Y >= HighPlayerRise;
        }

        private void TickConstellation()
        {
            int starCount = StarCount;
            int waveCount = WaveCount;
            lockedStarAims ??= new Vector2[starCount];
            waveFizzled ??= new bool[waveCount];
            Player target = Main.player[(int)Projectile.ai[1]];

            if (!Main.dedServ && Age <= StarFadeTicks)
            {
                for (int i = 0; i < starCount; i++)
                {
                    Vector2 star = StarPosition(i);
                    Vector2 from = star + Main.rand.NextVector2CircularEdge(24f, 24f);
                    Dust gather = Dust.NewDustPerfect(from, DustID.GoldFlame, (star - from) * 0.12f, 80, default, 0.8f);
                    gather.noGravity = true;
                }
            }

            for (int wave = 0; wave < waveCount; wave++)
            {
                int lockTick = WaveLockTick(wave);
                if (Age == lockTick)
                {
                    LockWave(wave, target);
                }

                if (Age == lockTick + ConstellationFireDelay && !waveFizzled[wave])
                {
                    FireWave(wave);
                }
            }

            if (Age >= WaveLockTick(waveCount) + ConstellationFireDelay + BoltFadeTicks)
            {
                Projectile.Kill();
            }
        }

        // Escape rule: a target no longer high at this wave's lock tick makes the wave fizzle into a puff.
        private void LockWave(int wave, Player target)
        {
            bool stillHigh = ConstellationTargetStillHigh(target);
            waveFizzled[wave] = !stillHigh;

            for (int i = wave; i < StarCount; i += WaveCount)
            {
                Vector2 star = StarPosition(i);
                lockedStarAims[i] = (target.Center - star).SafeNormalize(Vector2.UnitY);

                if (!stillHigh && !Main.dedServ)
                {
                    for (int puff = 0; puff < 8; puff++)
                    {
                        Dust fizzle = Dust.NewDustPerfect(star, DustID.Smoke, Main.rand.NextVector2Circular(1.5f, 1.5f), 120, default, 0.9f);
                        fizzle.noGravity = true;
                    }
                }
            }
        }

        private void FireWave(int wave)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.45f, Pitch = 0.3f, MaxInstances = 4 }, Projectile.Center);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = wave; i < StarCount; i += WaveCount)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), StarPosition(i), lockedStarAims[i] * StarBoltSpeed, Type,
                    Projectile.damage, 3f, Main.myPlayer, (float)WardenBoltMode.Bolt, StarBoltLife);
            }
        }

        // ── Damage ──────────────────────────────────────────────────────────────────
        public override bool? CanDamage()
        {
            switch (Mode)
            {
                case WardenBoltMode.Bolt:
                    return Age < Projectile.ai[1];
                case WardenBoltMode.Crawler:
                    return Age >= Projectile.ai[2];
                case WardenBoltMode.SkyStrike:
                    return Age >= Projectile.ai[1] && Age < Projectile.ai[1] + StrikeLiveTicks;
                case WardenBoltMode.Annulus:
                case WardenBoltMode.Rock:
                case WardenBoltMode.ChargedRock:
                    return true;
                case WardenBoltMode.Crescent:
                    return Age < Projectile.ai[1] / Math.Max(1f, Projectile.velocity.Length());
                case WardenBoltMode.Lance:
                    return StuckFlag <= 0f;
                case WardenBoltMode.Shock:
                    return Age <= Projectile.ai[2];
                default:
                    return false;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            switch (Mode)
            {
                case WardenBoltMode.Crawler:
                {
                    Rectangle crawlerBox = new Rectangle((int)Projectile.Center.X - 14, (int)Projectile.Bottom.Y - 20, 28, 20);
                    return crawlerBox.Intersects(targetHitbox);
                }

                case WardenBoltMode.SkyStrike:
                {
                    int width = (int)(Projectile.ai[2] * 16f);
                    int height = StrikeHeightTiles * 16;
                    Rectangle column = new Rectangle((int)Projectile.Center.X - width / 2, (int)Projectile.Center.Y - height, width, height);
                    return column.Intersects(targetHitbox);
                }

                case WardenBoltMode.Annulus:
                {
                    float radius = AnnulusRadius;
                    float nearest = NearestDistance(Projectile.Center, targetHitbox);
                    float farthest = FarthestDistance(Projectile.Center, targetHitbox);
                    return nearest <= radius + AnnulusHalfBand && farthest >= radius - AnnulusHalfBand;
                }

                case WardenBoltMode.Crescent:
                {
                    Rectangle crescentBox = new Rectangle((int)Projectile.Center.X - 12, (int)Projectile.Center.Y - 20, 24, 40);
                    return crescentBox.Intersects(targetHitbox);
                }

                case WardenBoltMode.Lance:
                {
                    Vector2 halfLance = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 30f;
                    float collisionPoint = 0f;
                    return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - halfLance, Projectile.Center + halfLance, 12f, ref collisionPoint);
                }

                case WardenBoltMode.Shock:
                    return NearestDistance(Projectile.Center, targetHitbox) <= Projectile.ai[1];

                default:
                    return null;
            }
        }

        private static float NearestDistance(Vector2 point, Rectangle box)
        {
            float nearestX = MathHelper.Clamp(point.X, box.Left, box.Right);
            float nearestY = MathHelper.Clamp(point.Y, box.Top, box.Bottom);
            return Vector2.Distance(point, new Vector2(nearestX, nearestY));
        }

        private static float FarthestDistance(Vector2 point, Rectangle box)
        {
            float farX = Math.Max(Math.Abs(point.X - box.Left), Math.Abs(point.X - box.Right));
            float farY = Math.Max(Math.Abs(point.Y - box.Top), Math.Abs(point.Y - box.Bottom));
            return new Vector2(farX, farY).Length();
        }

        // Runs on the hit player's machine; the mod relays the kill to the server (RequestKillHitProjectile).
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            bool diesOnHit = Mode == WardenBoltMode.Bolt || Mode == WardenBoltMode.Lance || Mode == WardenBoltMode.Rock || Mode == WardenBoltMode.ChargedRock;
            if (diesOnHit)
            {
                Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            int dustCount = 0;
            int dustType = DustID.Electric;

            switch (Mode)
            {
                case WardenBoltMode.Bolt:
                case WardenBoltMode.Lance:
                    dustCount = 70;
                    break;
                case WardenBoltMode.Crawler:
                case WardenBoltMode.Crescent:
                    dustCount = 18;
                    break;
                case WardenBoltMode.Rock:
                case WardenBoltMode.ChargedRock:
                    dustCount = 30;
                    dustType = DustID.Stone;
                    SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.55f, MaxInstances = 4 }, Projectile.Center);
                    break;
            }

            for (int i = 0; i < dustCount; i++)
            {
                Dust burst = Dust.NewDustPerfect(Projectile.Center, dustType, Main.rand.NextVector2Circular(5f, 5f), 40, default, 1.15f);
                burst.noGravity = dustType == DustID.Electric;
            }
        }

        // ── Drawing ─────────────────────────────────────────────────────────────────
        private static void DrawBeam(Vector2 from, Vector2 to, Color color, float thickness)
        {
            Vector2 span = to - from;
            float length = span.Length();
            if (length < 0.5f)
            {
                return;
            }

            Rectangle pixel = new Rectangle(0, 0, 1, 1);
            Vector2 origin = new Vector2(0f, 0.5f);
            Vector2 scale = new Vector2(length, thickness);
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, from - Main.screenPosition, pixel, color, span.ToRotation(), origin, scale, SpriteEffects.None, 0);
        }

        // Jagged lightning between two points. The seed changes every 3 ticks, so the arc flickers without per-frame noise.
        private void DrawLightning(Vector2 from, Vector2 to, Color edge, Color core, float thickness, int segments, float jitter, int seedOffset = 0)
        {
            int flickerStep = (int)(Main.GameUpdateCount / 3);
            int seed = Projectile.identity * 31 + flickerStep * 7 + seedOffset;
            DrawJaggedBolt(seed, from, to, edge, core, thickness, segments, jitter);
        }

        /// <summary>Draws one jagged bolt. The same seed gives the same shape, so callers choose how often it flickers.</summary>
        public static void DrawJaggedBolt(int seed, Vector2 from, Vector2 to, Color edge, Color core, float thickness, int segments, float jitter)
        {
            Random random = new Random(seed);
            Vector2 normal = (to - from).SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            Vector2 previous = from;

            for (int i = 1; i <= segments; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, i / (float)segments);
                if (i < segments)
                {
                    float offset = ((float)random.NextDouble() * 2f - 1f) * jitter;
                    point += normal * offset;
                }

                DrawBeam(previous, point, edge, thickness * 2.2f);
                DrawBeam(previous, point, core, thickness);
                previous = point;
            }
        }

        private void DrawStar(Vector2 center, float opacity, float size)
        {
            Color color = GoldCore * opacity;
            DrawBeam(center - new Vector2(size, 0f), center + new Vector2(size, 0f), color, 2f);
            DrawBeam(center - new Vector2(0f, size), center + new Vector2(0f, size), color, 2f);
        }

        private void DrawConstellation()
        {
            int starCount = StarCount;
            int perRing = starCount / 2;
            float starOpacity = MathHelper.Clamp(Age / StarFadeTicks, 0f, 1f);
            float lineProgress = MathHelper.Clamp((Age - StarFadeTicks) / (ConstellationLinesEnd - StarFadeTicks), 0f, 1f);
            Color lineColor = GoldEdge * 0.35f;

            for (int i = 0; i < perRing; i++)
            {
                Vector2 outer = StarPosition(i);
                Vector2 nextOuter = StarPosition((i + 1) % perRing);
                Vector2 inner = StarPosition(perRing + i);
                Vector2 nextInner = StarPosition(perRing + (i + 1) % perRing);
                DrawBeam(outer, Vector2.Lerp(outer, nextOuter, lineProgress), lineColor, 1.5f);
                DrawBeam(inner, Vector2.Lerp(inner, nextInner, lineProgress), lineColor, 1.5f);
                DrawBeam(outer, Vector2.Lerp(outer, inner, lineProgress), lineColor, 1.5f);
            }

            for (int i = 0; i < starCount; i++)
            {
                int wave = i % WaveCount;
                int fireTick = WaveLockTick(wave) + ConstellationFireDelay;
                if (Age >= fireTick)
                {
                    continue;
                }

                Vector2 star = StarPosition(i);
                DrawStar(star, starOpacity, 7f);

                bool aimLocked = lockedStarAims != null && waveFizzled != null && Age >= WaveLockTick(wave) && !waveFizzled[wave];
                if (aimLocked)
                {
                    DrawBeam(star, star + lockedStarAims[i] * 160f, StormBlue * 0.6f, 1.5f);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            switch (Mode)
            {
                case WardenBoltMode.Bolt:
                {
                    float opacity = MathHelper.Clamp((Projectile.ai[1] + BoltFadeTicks - Age) / BoltFadeTicks, 0f, 1f);
                    Vector2 back = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 34f;
                    DrawLightning(back, Projectile.Center, GoldEdge * 0.55f * opacity, GoldCore * opacity, 2f, 4, 5f);
                    break;
                }

                case WardenBoltMode.Crawler:
                {
                    float armed = MathHelper.Clamp(Age / Math.Max(1f, Projectile.ai[2]), 0.3f, 1f);
                    Vector2 bottom = Projectile.Bottom;
                    DrawLightning(bottom + new Vector2(-16f, -2f), bottom + new Vector2(0f, -20f), GoldEdge * 0.5f * armed, GoldCore * armed, 1.5f, 3, 4f);
                    DrawLightning(bottom + new Vector2(16f, -2f), bottom + new Vector2(0f, -20f), GoldEdge * 0.5f * armed, GoldCore * armed, 1.5f, 3, 4f, 1);
                    break;
                }

                case WardenBoltMode.SkyStrike:
                    DrawSkyStrike();
                    break;

                case WardenBoltMode.Annulus:
                    DrawAnnulus();
                    break;

                case WardenBoltMode.Crescent:
                    DrawCrescent();
                    break;

                case WardenBoltMode.Rock:
                case WardenBoltMode.ChargedRock:
                {
                    Vector2 corner = new Vector2(9f).RotatedBy(Projectile.rotation);
                    DrawBeam(Projectile.Center - corner, Projectile.Center + corner, new Color(112, 96, 82), 13f);
                    break;
                }

                case WardenBoltMode.Lance:
                {
                    Vector2 axis = Projectile.rotation.ToRotationVector2() * 30f;
                    DrawLightning(Projectile.Center - axis, Projectile.Center + axis, StormBlue * 0.55f, GoldCore, 3f, 5, 3f);
                    break;
                }

                case WardenBoltMode.Shock:
                {
                    float radius = Projectile.ai[1];
                    DrawLightning(Projectile.Center - new Vector2(radius, 0f), Projectile.Center + new Vector2(radius, 0f), GoldEdge * 0.5f, GoldCore, 2f, 5, 8f);
                    DrawLightning(Projectile.Center - new Vector2(0f, radius), Projectile.Center + new Vector2(0f, radius), GoldEdge * 0.5f, GoldCore, 2f, 5, 8f, 1);
                    break;
                }

                case WardenBoltMode.Constellation:
                    DrawConstellation();
                    break;
            }

            return false;
        }

        private void DrawSkyStrike()
        {
            float markTicks = Projectile.ai[1];
            float halfWidth = Projectile.ai[2] * 8f;
            int height = StrikeHeightTiles * 16;
            Vector2 floor = Projectile.Center;
            Vector2 top = floor - new Vector2(0f, height);

            if (Age < markTicks)
            {
                float charge = Age / Math.Max(1f, markTicks);
                DrawBeam(floor - new Vector2(halfWidth, 1f), floor + new Vector2(halfWidth, -1f), GoldCore * charge, 2f);
                return;
            }

            float fade = 1f - MathHelper.Clamp((Age - markTicks - StrikeLiveTicks) / StrikeLingerTicks, 0f, 1f);
            DrawLightning(top, floor, GoldEdge * 0.6f * fade, GoldCore * fade, halfWidth * 0.3f, 8, halfWidth * 0.6f);
        }

        private void DrawAnnulus()
        {
            const int Segments = 56;
            float radius = AnnulusRadius;
            Vector2 previous = Projectile.Center + new Vector2(radius, 0f);

            for (int i = 1; i <= Segments; i++)
            {
                Vector2 point = Projectile.Center + (MathHelper.TwoPi * i / Segments).ToRotationVector2() * radius;
                DrawBeam(previous, point, GoldEdge * 0.4f, AnnulusHalfBand * 1.2f);
                DrawBeam(previous, point, GoldCore, 3f);
                previous = point;
            }
        }

        private void DrawCrescent()
        {
            float lifeTicks = Projectile.ai[1] / Math.Max(1f, Projectile.velocity.Length());
            float opacity = MathHelper.Clamp((lifeTicks + CrescentFadeTicks - Age) / CrescentFadeTicks, 0f, 1f);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 up = new Vector2(0f, -20f);
            Vector2 tipPoint = Projectile.Center + forward * 12f;
            Vector2 upperBack = Projectile.Center - forward * 8f + up;
            Vector2 lowerBack = Projectile.Center - forward * 8f - up;

            DrawLightning(upperBack, tipPoint, StormBlue * 0.5f * opacity, GoldCore * opacity, 2f, 3, 2f);
            DrawLightning(lowerBack, tipPoint, StormBlue * 0.5f * opacity, GoldCore * opacity, 2f, 3, 2f, 1);
        }
    }
}
