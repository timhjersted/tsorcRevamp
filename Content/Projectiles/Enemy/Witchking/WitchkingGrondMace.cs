using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Projectiles.Summon.Whips;
using tsorcRevamp.NPCs;
using tsorcRevamp.Utilities;
using WitchkingBoss = tsorcRevamp.NPCs.Bosses.SuperHardMode.Witchking;

namespace tsorcRevamp.Content.Projectiles.Enemy.Witchking
{
    /// <summary>
    /// Grond's Toll, the Witchking's phase-2 signature. The mace whirls overhead on its chain, speeding
    /// up across three turns while Blight smoke thickens on the head (0–50); at tick 30 the strike point locks — the
    /// floor under the target, at most 36 tiles out — and a violet marker burns on it until the slam. Ticks 50–58 the
    /// head drops onto the mark (the only damaging window); on 58 two void shockwaves run along the floor both ways
    /// (jumpable) and a Blight mire is left on the spot. 58–128 the mace drags back to the hand (the punish window).
    /// Not rollable by design: the slam is a positional test. The Witchking's pose beats read SlamStartTick/ImpactTick.
    /// ai[0] = owner whoAmI (an IEnemyWhipOwner: the hand anchor), ai[1] = target player. Strike point = velocity
    /// (ShouldUpdatePosition is false), synced once by the server at the lock.
    /// Drawn with the player whip's sheet (intentional sharing): handle, 18px links, mace head.
    /// Scale: Grond is three times the original toll in every dimension that reaches. Strike range 12 → 36 tiles, the
    /// whirl 70 → 210px wide (flattened to half height so it doesn't plough the low cave ceiling), the head drawn 1.4 →
    /// 4.2x with a 40 → 120px hitbox (the 30x50 head frame at 4.2x is 126px wide: the hitbox matches the drawn head),
    /// chain links thickening 1x → 3x toward the head, and the marker and impact burst three times as wide. The floor
    /// shockwaves and the mire are unchanged: they are separate hazards with their own counterplay (jumped, walked out).
    /// </summary>
    public class WitchkingGrondMace : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(WitchkingMaceProjectile));

        public const int AimLockTick = 30;
        public const int SlamStartTick = 50;
        public const int ImpactTick = 58;
        public const int TotalTicks = 128;
        private const float ReachScale = 3f;
        private const float OrbitRadius = 70f * ReachScale;
        // The whirl is flattened: full radius sideways, half of it up and down (reads as a lasso seen side-on).
        private const float OrbitVerticalSquash = 0.5f;
        private const float OrbitLift = 50f;
        private const float OrbitTurns = 3f;
        private const float MaxStrikeDistance = 12f * 16f * ReachScale;
        private const int FloorScanTiles = 30;
        private const float HeadHitboxSize = 40f * ReachScale;
        private const float HeadDrawScale = 1.4f * ReachScale;
        private const float MarkerHalfWidth = 28f * ReachScale;

        private static readonly Rectangle HandleFrame = new Rectangle(0, 0, 30, 36);
        private static readonly Rectangle LinkFrame = new Rectangle(0, 48, 30, 18);
        private static readonly Rectangle HeadFrame = new Rectangle(0, 70, 30, 50);

        private int Age => (int)Projectile.localAI[0];
        private Vector2 StrikePoint => Projectile.velocity;
        // Where the head's centre comes to rest: 40% of the head above the floor point, so the 120px head lands ON the
        // floor (its hitbox reaching ~108px up, past a standing player's 42) instead of half buried in it.
        private Vector2 HeadRestPoint => StrikePoint - new Vector2(0f, HeadHitboxSize * 0.4f);

        // The head's position at the end of the whirl, kept for the slam's start.
        private Vector2 _orbitEnd;

        public override void SetStaticDefaults()
        {
            // The chain can be 36 tiles long: keep drawing it while the head is far off screen.
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1400;
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)HeadHitboxSize;
            Projectile.height = (int)HeadHitboxSize;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalTicks + 5;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Age >= SlamStartTick && Age <= ImpactTick + 2 ? null : false;

        public override void AI()
        {
            if (!TryGetOwner(out NPC owner, out IEnemyWhipOwner whipOwner) || Age >= TotalTicks)
            {
                Projectile.Kill();
                return;
            }

            Projectile.localAI[0]++;
            Vector2 anchor = whipOwner.WhipAnchor;

            if (Age == AimLockTick)
            {
                LockStrikePoint(owner);
            }

            Vector2 head;
            if (Age < SlamStartTick)
            {
                // Accelerating whirl overhead: angle grows with progress^1.5, so the three turns speed up.
                float progress = Age / (float)SlamStartTick;
                float angle = owner.direction * OrbitTurns * MathHelper.TwoPi * MathF.Pow(progress, 1.5f) - MathHelper.PiOver2;
                Vector2 orbitCenter = anchor - new Vector2(0f, OrbitLift);
                Vector2 orbitOffset = angle.ToRotationVector2() * OrbitRadius;
                orbitOffset.Y *= OrbitVerticalSquash;
                head = orbitCenter + orbitOffset;
                _orbitEnd = head;
            }
            else if (Age < ImpactTick)
            {
                // Accelerating drop onto the marked floor.
                float slam = (Age - SlamStartTick) / (float)(ImpactTick - SlamStartTick);
                head = Vector2.Lerp(_orbitEnd, HeadRestPoint, slam * slam);
            }
            else
            {
                float drag = MathHelper.SmoothStep(0f, 1f, (Age - ImpactTick) / (float)(TotalTicks - ImpactTick));
                head = Vector2.Lerp(HeadRestPoint, anchor, drag);
            }

            Projectile.Center = head;

            if (Age == ImpactTick)
            {
                Impact();
            }

            SpawnTellDust(head);
        }

        // Server picks the floor under the target, at most 12 tiles out from the Witchking; clients compute the same
        // and then adopt the server's sync.
        private void LockStrikePoint(NPC owner)
        {
            int targetIndex = (int)Projectile.ai[1];
            Player target = Main.player[Math.Clamp(targetIndex, 0, Main.maxPlayers - 1)];
            float offsetX = MathHelper.Clamp(target.Center.X - owner.Center.X, -MaxStrikeDistance, MaxStrikeDistance);
            float strikeX = owner.Center.X + offsetX;

            int tileX = (int)(strikeX / 16f);
            int startY = (int)(target.Center.Y / 16f);
            float floorY = owner.Bottom.Y;
            for (int y = startY; y < startY + FloorScanTiles; y++)
            {
                if (WorldGen.SolidTile(tileX, y))
                {
                    floorY = y * 16f;
                    break;
                }
            }

            Projectile.velocity = new Vector2(strikeX, floorY);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.netUpdate = true;
            }
        }

        private void Impact()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item70 with { Volume = 1f, Pitch = -0.6f }, StrikePoint);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = -0.4f }, StrikePoint);

                // Three layers across the 120px head's footprint. Body: black Blight smoke and violet shadow thrown up off
                // the floor. Sparks: fast yellow Blight flecks. Atmosphere: slow tinted smoke that billows and outlives it.
                for (int i = 0; i < 70; i++)
                {
                    int dustType = DustID.Wraith;
                    if (i % 3 == 0)
                    {
                        dustType = DustID.Shadowflame;
                    }
                    Vector2 position = StrikePoint + new Vector2(Main.rand.NextFloat(-HeadHitboxSize / 2f, HeadHitboxSize / 2f), -4f);
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-9f, 9f), Main.rand.NextFloat(-9f, -1.5f));
                    Dust burst = Dust.NewDustPerfect(position, dustType, velocity, 40, default, Main.rand.NextFloat(1.1f, 1.6f));
                    burst.noGravity = true;
                }
                for (int i = 0; i < 30; i++)
                {
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-13f, 13f), Main.rand.NextFloat(-11f, -3f));
                    Dust spark = Dust.NewDustPerfect(StrikePoint, DustID.YellowTorch, velocity, 0, default, Main.rand.NextFloat(0.6f, 0.95f));
                    spark.noGravity = true;
                }
                for (int i = 0; i < 20; i++)
                {
                    Vector2 position = StrikePoint + new Vector2(Main.rand.NextFloat(-HeadHitboxSize, HeadHitboxSize), -8f);
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-2.4f, 2.4f), Main.rand.NextFloat(-2.2f, -0.4f));
                    Dust smoke = Dust.NewDustPerfect(position, DustID.Smoke, velocity, 150, new Color(36, 24, 48), Main.rand.NextFloat(0.6f, 0.85f));
                    smoke.noGravity = true;
                    smoke.noLight = true;
                    smoke.fadeIn = Main.rand.NextFloat(1.4f, 1.8f);
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float floorY = StrikePoint.Y;
            Vector2 waveCenter = new Vector2(StrikePoint.X, floorY - 16f);
            int waveType = ModContent.ProjectileType<WitchkingVoidShockwave>();
            int waveDamage = EnemyDamage.Projectile(WitchkingBoss.GrondWaveDamage);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), waveCenter, Vector2.Zero, waveType, waveDamage, 0f, Main.myPlayer, 1f, floorY);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), waveCenter, Vector2.Zero, waveType, waveDamage, 0f, Main.myPlayer, -1f, floorY);

            Vector2 mireCenter = new Vector2(StrikePoint.X, floorY - 20f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), mireCenter, Vector2.Zero,
                ModContent.ProjectileType<WitchkingBlightMire>(), 0, 0f, Main.myPlayer);
        }

        // Blight smoke thickening on the whirling head; from the lock to the slam a violet marker burns on the floor.
        private void SpawnTellDust(Vector2 head)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Whirl: Blight smoke streaming off the 120px head, 3 -> 7 motes/t as it speeds up, plus a yellow fleck a tick.
            if (Age < SlamStartTick)
            {
                int smokeCount = 3 + Age / 12;
                for (int i = 0; i < smokeCount; i++)
                {
                    Vector2 position = head + Main.rand.NextVector2Circular(HeadHitboxSize * 0.35f, HeadHitboxSize * 0.35f);
                    Dust smoke = Dust.NewDustPerfect(position, DustID.Wraith, Vector2.Zero, 60, default, Main.rand.NextFloat(1.1f, 1.5f));
                    smoke.noGravity = true;
                }
                Dust fleck = Dust.NewDustPerfect(head + Main.rand.NextVector2Circular(30f, 30f), DustID.YellowTorch,
                    Main.rand.NextVector2Circular(1.5f, 1.5f), 0, default, Main.rand.NextFloat(0.6f, 0.9f));
                fleck.noGravity = true;
            }

            // Marker: a violet strip as wide as the head burning on the floor from the lock to the slam, 6 -> 10 motes/t
            // as the slam nears, with bright edges so its full width reads.
            bool marking = Age >= AimLockTick && Age < ImpactTick;
            if (marking)
            {
                float markProgress = (Age - AimLockTick) / (float)(ImpactTick - AimLockTick);
                int markCount = 6 + (int)(markProgress * 4f);
                for (int i = 0; i < markCount; i++)
                {
                    Vector2 mark = StrikePoint + new Vector2(Main.rand.NextFloat(-MarkerHalfWidth, MarkerHalfWidth), -2f);
                    Vector2 rise = new Vector2(0f, -Main.rand.NextFloat(0.8f, 2.2f));
                    Dust marker = Dust.NewDustPerfect(mark, DustID.Shadowflame, rise, 30, default, Main.rand.NextFloat(1.0f, 1.4f));
                    marker.noGravity = true;
                }
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 edge = StrikePoint + new Vector2(side * MarkerHalfWidth, -2f);
                    Dust edgeGlint = Dust.NewDustPerfect(edge, DustID.PurpleTorch, new Vector2(0f, -1.6f), 0, new Color(235, 210, 255), 0.8f);
                    edgeGlint.noGravity = true;
                }
                Lighting.AddLight(StrikePoint, 0.4f, 0.1f, 0.55f);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            BlightBuildup.Apply(target, WitchkingMaceWhip.BlightPerHit);
        }

        // Chain: handle at the hand, 18px links along the line to the head, the head unstretched at the end.
        public override bool PreDraw(ref Color lightColor)
        {
            if (!TryGetOwner(out _, out IEnemyWhipOwner whipOwner))
            {
                return false;
            }

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 anchor = whipOwner.WhipAnchor;
            Vector2 head = Projectile.Center;
            Vector2 line = head - anchor;
            float length = line.Length();
            float rotation = line.ToRotation() - MathHelper.PiOver2; // the sheet faces down
            Vector2 direction = line.SafeNormalize(Vector2.UnitY);

            Color handleColor = Lighting.GetColor(anchor.ToTileCoordinates());
            Main.EntitySpriteDraw(texture, anchor - Main.screenPosition, HandleFrame, handleColor, rotation,
                new Vector2(HandleFrame.Width / 2f, 16f), 1f, SpriteEffects.None, 0);

            // Links thicken from 1x at the hand to ReachScale x at the head; each link advances by its own drawn length so
            // the chain stays closed. Stop where the scaled head's upper half begins.
            float headHalfLength = HeadFrame.Height / 2f * HeadDrawScale;
            float along = 18f;
            while (along < length - headHalfLength)
            {
                float linkScale = MathHelper.Lerp(1f, ReachScale, along / Math.Max(1f, length));
                Vector2 linkPosition = anchor + direction * along;
                Color linkColor = Lighting.GetColor(linkPosition.ToTileCoordinates());
                Main.EntitySpriteDraw(texture, linkPosition - Main.screenPosition, LinkFrame, linkColor, rotation,
                    new Vector2(LinkFrame.Width / 2f, 0f), linkScale, SpriteEffects.None, 0);
                along += LinkFrame.Height * linkScale;
            }

            Color headColor = Lighting.GetColor(head.ToTileCoordinates());
            Main.EntitySpriteDraw(texture, head - Main.screenPosition, HeadFrame, headColor, rotation,
                new Vector2(HeadFrame.Width / 2f, HeadFrame.Height / 2f), HeadDrawScale, SpriteEffects.None, 0);
            return false;
        }

        private bool TryGetOwner(out NPC owner, out IEnemyWhipOwner whipOwner)
        {
            int ownerIndex = (int)Projectile.ai[0];
            if (ownerIndex >= 0 && ownerIndex < Main.maxNPCs)
            {
                NPC candidate = Main.npc[ownerIndex];
                if (candidate.active && candidate.ModNPC is IEnemyWhipOwner anchorOwner)
                {
                    owner = candidate;
                    whipOwner = anchorOwner;
                    return true;
                }
            }

            owner = null;
            whipOwner = null;
            return false;
        }
    }
}
