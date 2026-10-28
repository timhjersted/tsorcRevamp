using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Content.Projectiles.Enemy.DemonFamily
{
    /// <summary>Which demon fired a shot. Passed to both demon projectiles in ai[0]; picks dust, light and the on-hit debuff.</summary>
    public enum DemonTier : byte
    {
        Oolacile = 0, // early pre-HM boss: orange torch fire, On Fire
        Defiled = 1,  // late pre-HM boss: black flame with pink accents, Curse buildup
        Ancient = 2,  // SHM elite: bright red flame with black smoke, Destined Death + Abyss
    }

    /// <summary>
    /// The demon family's element per tier: dust, light, and what a hit does to the player. Shared by the demons'
    /// contact hits (NPCs/Bosses/DemonBase.cs) and both projectiles below, so one tier's debuff can't drift between them.
    /// </summary>
    public static class DemonElement
    {
        public const int CurseDurationTicks = 300 * 60;
        public const int DestinedDeathTicks = 10 * 60;
        public const int AbyssTicks = 30 * 60;

        /// <summary>The flame body. T2's black flame is Wraith (54) smoke; T3's red is RedTorch (60).</summary>
        public static int FlameDust(DemonTier tier)
        {
            if (tier == DemonTier.Defiled)
            {
                return DustID.Wraith;
            }

            if (tier == DemonTier.Ancient)
            {
                return DustID.RedTorch;
            }

            return DustID.Torch;
        }

        /// <summary>The second colour, used sparingly. T2's pink is spawned through CurseBuildup.SpawnAccentDust instead (see SpawnAccent).</summary>
        public static int AccentDust(DemonTier tier)
        {
            if (tier == DemonTier.Defiled)
            {
                return DustID.PinkTorch;
            }

            if (tier == DemonTier.Ancient)
            {
                return DustID.Wraith;
            }

            return DustID.OrangeTorch;
        }

        /// <summary>Lighting.AddLight colour at full strength. T2 glows a dim pink so its black flame still reads in the dark.</summary>
        public static Vector3 LightColor(DemonTier tier)
        {
            if (tier == DemonTier.Defiled)
            {
                return new Vector3(0.45f, 0.12f, 0.32f);
            }

            if (tier == DemonTier.Ancient)
            {
                return new Vector3(0.95f, 0.08f, 0.08f);
            }

            return new Vector3(0.9f, 0.5f, 0.12f);
        }

        /// <summary>The tint used for the body flash, the commit flash and the despawn text.</summary>
        public static Color TintColor(DemonTier tier)
        {
            if (tier == DemonTier.Defiled)
            {
                return Color.HotPink;
            }

            if (tier == DemonTier.Ancient)
            {
                return Color.Red;
            }

            return Color.OrangeRed;
        }

        /// <summary>
        /// DemonFireball.fx palette: dark = outer body, mid = heat band, core = hot centre. T2/T3 bodies are near-black so
        /// the ball reads as black flame (premultiplied alpha darkens the background); T1 stays orange -> yellow.
        /// </summary>
        public static void FireballPalette(DemonTier tier, out Vector3 dark, out Vector3 mid, out Vector3 core)
        {
            if (tier == DemonTier.Defiled)
            {
                dark = new Vector3(0.05f, 0.01f, 0.04f);
                mid = new Vector3(0.80f, 0.10f, 0.50f);
                core = new Vector3(0.45f, 0.30f, 0.40f);
                return;
            }

            if (tier == DemonTier.Ancient)
            {
                dark = new Vector3(0.06f, 0.00f, 0.00f);
                mid = new Vector3(1.00f, 0.06f, 0.02f);
                core = new Vector3(0.50f, 0.25f, 0.12f);
                return;
            }

            dark = new Vector3(0.78f, 0.12f, 0.00f);
            mid = new Vector3(0.30f, 0.35f, 0.02f);
            core = new Vector3(0.35f, 0.32f, 0.12f);
        }

        /// <summary>
        /// One accent mote. T2 goes through CurseBuildup.SpawnAccentDust — untinted PinkTorch, the mod's colour code for
        /// "this builds curse" — so the Defiled Demon's fire matches the curse burst on the player exactly.
        /// </summary>
        public static Dust SpawnAccent(DemonTier tier, Vector2 position, int width, int height, Vector2 velocity, float scale)
        {
            if (tier == DemonTier.Defiled)
            {
                return CurseBuildup.SpawnAccentDust(position, width, height, velocity, scale);
            }

            Dust accent = Dust.NewDustDirect(position, width, height, AccentDust(tier), 0f, 0f, 100, default, scale);
            accent.noGravity = true;
            accent.velocity = velocity;
            return accent;
        }

        /// <summary>
        /// The tier's debuff for one hit. Called from OnHitPlayer hooks, which run on the hit player's own machine, so
        /// AddBuff / ApplyExplicit there syncs normally (unlike the old T3 server-side Abyss on cast).
        /// onFireTicks is T1's On Fire duration; curseAmount is T2's buildup. T3 always adds one Destined Death stack
        /// (each AddBuff re-application is +1 stack, max 6) and refreshes Abyss to 30s.
        /// </summary>
        public static void ApplyHitDebuff(Player target, DemonTier tier, int onFireTicks, int curseAmount)
        {
            if (tier == DemonTier.Oolacile)
            {
                target.AddBuff(BuffID.OnFire, onFireTicks);
                return;
            }

            if (tier == DemonTier.Defiled)
            {
                CurseBuildup.ApplyExplicit(target, curseAmount, CurseDurationTicks);
                return;
            }

            target.AddBuff(ModContent.BuffType<DestinedDeath>(), DestinedDeathTicks);
            target.AddBuff(ModContent.BuffType<Abyss>(), AbyssTicks);
        }
    }

    // One puff of a demon's breath. Dust-only, coloured by tier (ai[0]). Grows 0.6x → 1.4x over its life at constant speed,
    // so reach = Speed x LifetimeTicks (≈ 450px; the demon gates the breath at 420), and thins over the last FadeTicks
    // instead of popping. Stops on tiles. Pierces the player (a stream, not a bolt): its 40t post-hit immunity spaces hits.
    // Debuff per hit: T1 On Fire 4s, T2 Curse +10, T3 Destined Death +1 and Abyss 30s.
    public class DemonBreath : ModProjectile
    {
        public const float Speed = 9f;
        public const int LifetimeTicks = 50;
        private const int FadeTicks = 14;
        private const int OnFireTicks = 4 * 60;
        private const int CurseAmount = 10;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private DemonTier Tier => (DemonTier)(int)Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            DemonTier tier = Tier;
            float age = LifetimeTicks - Projectile.timeLeft;
            float growth = MathHelper.Clamp(age / LifetimeTicks, 0f, 1f);
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            float scale = MathHelper.Lerp(0.6f, 1.4f, growth);

            Lighting.AddLight(Projectile.Center, DemonElement.LightColor(tier) * fade);

            if (Main.dedServ)
            {
                return;
            }

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 sideways = new Vector2(-forward.Y, forward.X);
            int flameDust = DemonElement.FlameDust(tier);

            // Body: two flames per tick (one while thinning). The second is smaller and wider so they never stack into one blob.
            int flameCount = 2;
            if (fade < 1f)
            {
                flameCount = 1;
            }

            for (int i = 0; i < flameCount; i++)
            {
                Vector2 spot = Projectile.Center + Main.rand.NextVector2Circular(8f * scale, 8f * scale);
                float bodyScale = 2.2f * scale * fade + 0.4f;

                if (i == 1)
                {
                    bodyScale *= 0.62f;
                }

                Vector2 bodyVelocity = Projectile.velocity * 0.35f + sideways * Main.rand.NextFloat(-0.8f, 0.8f);
                Dust flame = Dust.NewDustPerfect(spot, flameDust, bodyVelocity, 60, default, bodyScale);
                flame.noGravity = true;
            }

            // Fine fast motes that outrun the body and break its silhouette up (FireBreath's foreground layer).
            Vector2 fineSpot = Projectile.Center + sideways * Main.rand.NextFloat(-9f, 9f);
            Vector2 fineVelocity = forward * Main.rand.NextFloat(1.4f, 3.6f) + sideways * Main.rand.NextFloat(-1.1f, 1.1f);
            Dust fine = Dust.NewDustPerfect(fineSpot, flameDust, fineVelocity, 120, default, Main.rand.NextFloat(0.5f, 0.95f) * fade);
            fine.noGravity = true;

            // Accent: T1 trailing orange ember, T2 curse-pink, T3 black smoke.
            if (Main.rand.NextBool(3))
            {
                Vector2 accentVelocity = -forward * Main.rand.NextFloat(0.3f, 1.2f);
                DemonElement.SpawnAccent(tier, Projectile.position, Projectile.width, Projectile.height, accentVelocity, 1.3f * scale * fade);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // A small smoky gutter wherever it ends (wall, timeout).
            int flameDust = DemonElement.FlameDust(Tier);
            for (int i = 0; i < 6; i++)
            {
                Dust smoke = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, flameDust, 0f, 0f, 120, default, 1.2f);
                smoke.noGravity = true;
                smoke.velocity *= 0.6f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            DemonElement.ApplyHitDebuff(target, Tier, OnFireTicks, CurseAmount);
        }
    }

    // Every fireball a demon throws. ai[0] = DemonTier, ai[1] = mode:
    //   ModeLob   (0) — Cinder Lob. Falls at Gravity px/tick²; the demon solves its launch to land on / past the player.
    //                   The Oolacile Demon's lobs bounce twice off floors (the old vanilla Fireball's behaviour) before bursting.
    //   ModeHeavy (1) — Long-range punisher. Same ballistic flight, twice the size and burst; never bounces.
    //   ModeFountain (2) — Fire Fountain shell (T2). Rises, hangs at its apex for ai[2] ticks glinting, then the server
    //                   aims it down at the player and flips it to ModeLob (synced via netUpdate, so clients just follow).
    //   ModeBolt  (3) — Doom Orb bolt (T3). Straight at BoltSpeed, no gravity.
    // No sprite: the ball is Effects/DemonFireball.fx in the tier palette, plus a light tier-dust trail and accent sparks,
    // so one class can be orange, black or red.
    // Every death (tile, player, timeout) plays the tier burst in OnKill; a timeout fades for FadeTicks first.
    // Debuff per hit: T1 On Fire 3s, T2 Curse +15 (heavy +20), T3 Destined Death +1 and Abyss 30s.
    public class DemonFireball : ModProjectile
    {
        public const float ModeLob = 0f;
        public const float ModeHeavy = 1f;
        public const float ModeFountain = 2f;
        public const float ModeBolt = 3f; // Doom Orb bolt (T3): no gravity, straight line
        public const float BoltSpeed = 12f;
        public const float FountainDiveSpeed = 11f;
        private const int FountainGlintTicks = 20;
        private const float FountainDiveSpread = 32f; // ±2 tiles around the player
        public const float Gravity = 0.2f;
        public const int LifetimeTicks = 180;
        private const int FadeTicks = 15;
        private const int OolacileBounces = 2;
        private const int OnFireTicks = 3 * 60;
        private const int LobCurseAmount = 15;
        private const int HeavyCurseAmount = 20;

        // Shader quad size (px, before rotation): width across, length along the flight line.
        private static readonly Vector2 LobDrawSize = new Vector2(22f, 34f);
        private static readonly Vector2 HeavyDrawSize = new Vector2(38f, 58f);

        private static Asset<Effect> fireballEffect;
        private static Asset<Texture2D> fireballNoise;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private DemonTier Tier => (DemonTier)(int)Projectile.ai[0];

        private bool IsHeavy => Projectile.ai[1] == ModeHeavy;

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            DemonTier tier = Tier;

            if (Projectile.ai[1] == ModeFountain)
            {
                // Fountain shell: rise under gravity, then freeze at the apex until its drop tick (ai[2], counted in
                // localAI[1] ticks since spawn). Glints in the tier accent for the last FountainGlintTicks.
                Projectile.localAI[1]++;
                float age = Projectile.localAI[1];
                float dropTick = Projectile.ai[2];

                // Hold the lifetime while it waits; a client that never hears the drop times out 60t late.
                if (age < dropTick + 60f)
                {
                    Projectile.timeLeft = LifetimeTicks;
                }

                if (Projectile.velocity.Y < 0f)
                {
                    Projectile.velocity.Y += Gravity;
                }
                else
                {
                    Projectile.velocity = Vector2.Zero;
                }

                bool glinting = age >= dropTick - FountainGlintTicks;
                if (glinting && !Main.dedServ && Main.rand.NextBool(2))
                {
                    DemonElement.SpawnAccent(tier, Projectile.Center - new Vector2(10f), 20, 20, Main.rand.NextVector2Circular(1.5f, 1.5f), 1.6f);
                }

                if (age == dropTick - FountainGlintTicks && !Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.35f, Pitch = 0.8f }, Projectile.Center);
                }

                // Server: dive at the closest player (±2 tiles), then it's an ordinary lob; netUpdate carries ai[1] + velocity.
                if (age >= dropTick && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int closest = Player.FindClosest(Projectile.position, Projectile.width, Projectile.height);
                    Vector2 aimPoint = Main.player[closest].Center + new Vector2(Main.rand.NextFloat(-FountainDiveSpread, FountainDiveSpread), 0f);
                    Projectile.velocity = Projectile.DirectionTo(aimPoint) * FountainDiveSpeed;
                    Projectile.ai[1] = ModeLob;
                    Projectile.netUpdate = true;
                }

                Lighting.AddLight(Projectile.Center, DemonElement.LightColor(tier));

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Dust hover = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DemonElement.FlameDust(tier), new Vector2(0f, -0.8f), 60, default, 1.3f);
                    hover.noGravity = true;
                }

                return;
            }

            // Gravity before the engine's position += velocity, matching the demon's discrete launch solve. Bolts fly straight.
            if (Projectile.ai[1] != ModeBolt)
            {
                Projectile.velocity.Y += Gravity;
            }

            float sizeScale = 1f;
            if (IsHeavy)
            {
                sizeScale = 1.8f;
            }

            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            Lighting.AddLight(Projectile.Center, DemonElement.LightColor(tier) * fade * sizeScale);

            if (Main.dedServ)
            {
                return;
            }

            int flameDust = DemonElement.FlameDust(tier);
            float coreRadius = 5f * sizeScale;

            // Core: one loose flame around the shader ball, so its edge breaks up into particles.
            Vector2 spot = Projectile.Center + Main.rand.NextVector2Circular(coreRadius, coreRadius);
            Dust core = Dust.NewDustPerfect(spot, flameDust, Projectile.velocity * 0.1f, 40, default, 1.4f * sizeScale * fade);
            core.noGravity = true;

            // Trail: one flame left behind, drifting back along the path.
            Dust trail = Dust.NewDustPerfect(Projectile.Center, flameDust, -Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(0.5f, 0.5f), 100, default, 1.2f * sizeScale * fade);
            trail.noGravity = true;

            if (Main.rand.NextBool(3))
            {
                DemonElement.SpawnAccent(tier, Projectile.position, Projectile.width, Projectile.height, -Projectile.velocity * 0.15f, 1.1f * sizeScale * fade);
            }
        }

        // The ball itself: DemonFireball.fx on a noise quad, nose along the velocity (+Y of the quad leads, so -90°),
        // tier palette from DemonElement.FireballPalette. Fades with the timeout like the dust.
        public override bool PreDraw(ref Color lightColor)
        {
            fireballEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/DemonFireball", AssetRequestMode.ImmediateLoad);
            fireballNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/SmoothNoise", AssetRequestMode.ImmediateLoad);

            Vector2 drawSize = LobDrawSize;
            if (IsHeavy)
            {
                drawSize = HeavyDrawSize;
            }

            float opacity = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            DemonElement.FireballPalette(Tier, out Vector3 dark, out Vector3 mid, out Vector3 core);
            Vector2 blocks = Vector2.Max(drawSize / 2f, Vector2.One);
            float seed = Projectile.identity * 0.137f % 1f;

            Effect effect = fireballEffect.Value;
            effect.Parameters["PixelGrid"].SetValue(new Vector4(blocks.X, blocks.Y, 1f / blocks.X, 1f / blocks.Y));
            effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["Opacity"].SetValue(opacity);
            effect.Parameters["Seed"].SetValue(seed);
            effect.Parameters["DarkColor"].SetValue(dark);
            effect.Parameters["MidColor"].SetValue(mid);
            effect.Parameters["CoreColor"].SetValue(core);

            Texture2D noise = fireballNoise.Value;
            // Hanging fountain shells have no velocity: point the nose straight down, the way they're about to go.
            float rotation = 0f;
            if (Projectile.velocity.LengthSquared() > 0.01f)
            {
                rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
            }

            Main.spriteBatch.End();
            try
            {
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(noise, Projectile.Center - Main.screenPosition, null, Color.White, rotation, noise.Size() * 0.5f, drawSize / noise.Size(), SpriteEffects.None, 0f);
            }
            finally
            {
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }

            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Oolacile lobs bounce off floors twice (localAI[0] counts bounces; tile collision is the same on every machine).
            bool canBounce = Tier == DemonTier.Oolacile && !IsHeavy && Projectile.localAI[0] < OolacileBounces;
            bool hitFloor = oldVelocity.Y > 0f && Projectile.velocity.Y != oldVelocity.Y;

            if (canBounce && hitFloor)
            {
                Projectile.localAI[0]++;
                Projectile.velocity.Y = -oldVelocity.Y * 0.6f;
                Projectile.velocity.X = oldVelocity.X * 0.85f;
                SpawnBurst(10, 3f);
                return false;
            }

            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int curseAmount = LobCurseAmount;
            if (IsHeavy)
            {
                curseAmount = HeavyCurseAmount;
            }

            DemonElement.ApplyHitDebuff(target, Tier, OnFireTicks, curseAmount);

            // Hostile shots pierce players unless they kill themselves; tsorcGlobalProjectile relays this local kill to the server in MP.
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            int burstCount = 40;
            float burstSpeed = 5f;

            if (IsHeavy)
            {
                burstCount = 70;
                burstSpeed = 7f;
            }

            SpawnBurst(burstCount, burstSpeed);

            if (!Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.5f, Pitch = 0.2f }, Projectile.Center);
            }
        }

        // Tier-coloured burst: flame body plus a quarter accent sparks. Used for bounces (small) and every death (full).
        private void SpawnBurst(int count, float speed)
        {
            if (Main.dedServ)
            {
                return;
            }

            DemonTier tier = Tier;
            int flameDust = DemonElement.FlameDust(tier);

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(speed, speed);

                if (i % 4 == 0)
                {
                    DemonElement.SpawnAccent(tier, Projectile.position, Projectile.width, Projectile.height, velocity, 1.4f);
                    continue;
                }

                Dust flame = Dust.NewDustPerfect(Projectile.Center, flameDust, velocity, 60, default, Main.rand.NextFloat(1.2f, 2.2f));
                flame.noGravity = true;
            }
        }
    }

    // The Oolacile Demon's ghost-skull (Spirit Ring + punisher). Vanilla DesertDjinnCurse's sprite and homing (aiStyle 107:
    // inertia 10, 7.5 px/t, stops steering inside 40px) rebuilt as our own class so it can carry poison and target the
    // demon's actual target in MP — ai[0] = target player index, set by the server at spawn.
    // Passes through walls deliberately (a ghost, like the vanilla skull); fades in over 10t and out over the last 30t.
    // Each hit: +PoisonAmount Poison buildup (meter 100 → Poisoned), then the skull bursts.
    public class DemonSpiritSkull : ModProjectile
    {
        public const int PoisonAmount = 50;
        private const int LifetimeTicks = 180;
        private const int FadeInTicks = 10;
        private const int FadeOutTicks = 30;
        private const float HomingInertia = 10f;
        private const float HomingSpeed = 7.5f;
        private const float HomingStopDistance = 40f;

        public override string Texture => UsefulFunctions.VanillaTextureFilepath(ProjectileID.DesertDjinnCurse, "Projectile");

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0];
            bool targetValid = targetIndex >= 0 && targetIndex < Main.maxPlayers && Main.player[targetIndex].active && !Main.player[targetIndex].dead;

            // Target gone: stop steering and fade out.
            if (!targetValid && Projectile.timeLeft > FadeOutTicks)
            {
                Projectile.timeLeft = FadeOutTicks;
            }

            if (targetValid)
            {
                Player target = Main.player[targetIndex];
                if (Projectile.Distance(target.Center) > HomingStopDistance)
                {
                    Vector2 toTarget = Projectile.DirectionTo(target.Center);
                    Projectile.velocity = (Projectile.velocity * (HomingInertia - 1f) + toTarget * HomingSpeed) / HomingInertia;
                }
            }

            Projectile.rotation = Projectile.velocity.X * 0.05f;
            Projectile.spriteDirection = Projectile.velocity.X > 0f ? 1 : -1;

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            Lighting.AddLight(Projectile.Center, 0.25f, 0.45f, 0.2f);

            if (Main.dedServ)
            {
                return;
            }

            // Trail: violet Shadowflame (27) wisps with a sickly green (GreenTorch) poison drip, 1 in 2 ticks each.
            if (Main.rand.NextBool(2))
            {
                Dust wisp = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DustID.Shadowflame, -Projectile.velocity * 0.2f, 120, default, 1.1f);
                wisp.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                Dust drip = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), DustID.GreenTorch, new Vector2(0f, 0.6f), 80, default, 1.3f);
                drip.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            PoisonBuildup.Apply(target, PoisonAmount);

            // Hostile shots pierce players unless they kill themselves; tsorcGlobalProjectile relays this local kill to the server in MP.
            Projectile.Kill();
        }

        public override Color? GetAlpha(Color lightColor)
        {
            float age = LifetimeTicks - Projectile.timeLeft;
            float fadeIn = MathHelper.Clamp(age / FadeInTicks, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)FadeOutTicks, 0f, 1f);
            return Color.White * 0.9f * fadeIn * fadeOut;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Burst: violet and green in equal parts, ~40 dust.
            for (int i = 0; i < 40; i++)
            {
                int dustType = DustID.Shadowflame;
                if (i % 2 == 0)
                {
                    dustType = DustID.GreenTorch;
                }

                Dust burst = Dust.NewDustPerfect(Projectile.Center, dustType, Main.rand.NextVector2Circular(4f, 4f), 60, default, Main.rand.NextFloat(1.2f, 1.9f));
                burst.noGravity = true;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.35f, Pitch = 0.3f }, Projectile.Center);
        }
    }

    // The Ancient Demon's Doom Orb. Harmless itself; a knot of black smoke around a red core that drifts to a point
    // HoverHeight above its target and fires BoltCount DemonFireball bolts at them, then implodes. ai[0] = target player,
    // ai[1] = owning NPC whoAmI (the demon gates "max 1 alive" on it). Timeline, in localAI[0] ticks since spawn:
    //   0..DriftTicks drift into place · bolt k fires at FirstBoltTick + k·BoltInterval, glinting red-white for the 20t
    //   before (sound at glint start) · ImplodeTick: smoke rushes inward for 20t → burst → gone.
    // Bolts aim on the server only; the drift follows the synced player position on every machine.
    public class DemonDoomOrb : ModProjectile
    {
        public const int BoltCount = 4;
        private const int DriftTicks = 50;
        private const int FirstBoltTick = 70;
        private const int BoltInterval = 40;
        private const int GlintTicks = 20;
        private const int ImplodeTick = FirstBoltTick + (BoltCount - 1) * BoltInterval + 40; // 230
        private const int ImplodeDurationTicks = 20;
        private const float HoverHeight = 160f; // 10 tiles above the player
        private const float DriftSpeed = 6f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.hostile = false; // harmless; the bolts carry the damage
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // deliberate: it hangs in the air over cover, the bolts are what tiles stop
            Projectile.ignoreWater = true;
            Projectile.timeLeft = ImplodeTick + ImplodeDurationTicks + 5;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            int age = (int)Projectile.localAI[0];
            int targetIndex = (int)Projectile.ai[0];
            bool targetValid = targetIndex >= 0 && targetIndex < Main.maxPlayers && Main.player[targetIndex].active && !Main.player[targetIndex].dead;

            // Drift: ease toward the hover point above the player (it keeps tracking them, at most DriftSpeed px/t).
            if (targetValid && age < ImplodeTick)
            {
                Vector2 hoverPoint = Main.player[targetIndex].Center - new Vector2(0f, HoverHeight);
                Vector2 toHover = hoverPoint - Projectile.Center;
                float hoverDistance = toHover.Length();
                float speed = MathHelper.Min(DriftSpeed, hoverDistance * 0.08f);
                Vector2 desired = toHover.SafeNormalize(Vector2.Zero) * speed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.15f);
            }
            else
            {
                Projectile.velocity *= 0.9f;
            }

            if (!targetValid && age < ImplodeTick)
            {
                Projectile.localAI[0] = ImplodeTick;
                age = ImplodeTick;
            }

            int boltIndex = -1;
            bool glinting = false;
            for (int k = 0; k < BoltCount; k++)
            {
                int fireTick = FirstBoltTick + k * BoltInterval;
                if (age == fireTick)
                {
                    boltIndex = k;
                }

                if (age >= fireTick - GlintTicks && age < fireTick)
                {
                    glinting = true;
                }

                if (age == fireTick - GlintTicks && !Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.4f, Pitch = -0.2f }, Projectile.Center);
                }
            }

            if (boltIndex >= 0)
            {
                if (!Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item73 with { Volume = 0.6f, Pitch = -0.3f }, Projectile.Center);
                }

                if (targetValid && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 boltVelocity = Projectile.DirectionTo(Main.player[targetIndex].Center) * DemonFireball.BoltSpeed;
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, boltVelocity, ModContent.ProjectileType<DemonFireball>(), Projectile.damage, 0f, Main.myPlayer, (float)DemonTier.Ancient, DemonFireball.ModeBolt);
                }
            }

            Lighting.AddLight(Projectile.Center, DemonElement.LightColor(DemonTier.Ancient) * 0.8f);

            if (age >= ImplodeTick + ImplodeDurationTicks)
            {
                Projectile.Kill();
                return;
            }

            if (Main.dedServ)
            {
                return;
            }

            bool imploding = age >= ImplodeTick;
            float swirlRadius = 22f;
            if (imploding)
            {
                float implodeProgress = (age - ImplodeTick) / (float)ImplodeDurationTicks;
                swirlRadius = MathHelper.Lerp(60f, 4f, implodeProgress);
            }

            // Body: Wraith (black) smoke orbiting the rim, drawn inward; RedTorch core flames. Glinting adds a bright
            // red-white flare on the core so "this one fires next" reads.
            for (int i = 0; i < 2; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(swirlRadius, swirlRadius);
                Vector2 orbitVelocity = offset.RotatedBy(MathHelper.PiOver2) * 0.06f - offset * 0.05f;
                Dust smoke = Dust.NewDustPerfect(Projectile.Center + offset, DustID.Wraith, orbitVelocity + Projectile.velocity, 80, default, 1.6f);
                smoke.noGravity = true;
            }

            Dust coreFlame = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(6f, 6f), DustID.RedTorch, Projectile.velocity, 40, default, 1.8f);
            coreFlame.noGravity = true;

            if (glinting)
            {
                Dust glint = Dust.NewDustPerfect(Projectile.Center, DustID.RedTorch, Main.rand.NextVector2Circular(2.5f, 2.5f), 0, Color.White, 2.4f);
                glint.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 60; i++)
            {
                int dustType = DustID.RedTorch;
                if (i % 3 == 0)
                {
                    dustType = DustID.Wraith;
                }

                Dust burst = Dust.NewDustPerfect(Projectile.Center, dustType, Main.rand.NextVector2Circular(6f, 6f), 60, default, Main.rand.NextFloat(1.4f, 2.2f));
                burst.noGravity = true;
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.6f, Pitch = -0.4f }, Projectile.Center);
        }
    }

    // The Ancient Demon's Death's Grasp. A low wave of red-black fire that crawls along the floor from the demon's feet
    // for a locked distance (ai[1] px, toward ai[0] = -1/+1 — the player's X at cast), then simmers SimmerTicks and erupts
    // into a column. Crawl hitbox is 32x24 (jumpable); the column is 64x192 for EruptTicks. A floor gap ends the crawl early,
    // where it erupts. Looks: decorative zero-damage DestinedDeathBlaze flames dropped along the path, lifting ones in the
    // column. Every hit: DemonElement Ancient (Destined Death +1, Abyss 30s).
    public class DemonGraspWave : ModProjectile
    {
        private const float CrawlSpeed = 6f;
        private const int SimmerTicks = 30;
        private const int EruptTicks = 24;
        private const int CrawlWidth = 32;
        private const int CrawlHeight = 24;
        private const int ColumnWidth = 64;
        private const int ColumnHeight = 192;

        // localAI[0] = distance crawled; localAI[1] = ticks since the crawl stopped (0 while crawling).
        private bool Stopped => Projectile.localAI[1] > 0f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = CrawlWidth;
            Projectile.height = CrawlHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false; // hugs the floor itself (TryFindGroundY), so tiles never kill it mid-crawl
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            float direction = Projectile.ai[0] < 0f ? -1f : 1f;
            float travelDistance = Projectile.ai[1];

            if (!Stopped)
            {
                float remaining = travelDistance - Projectile.localAI[0];
                float step = MathHelper.Min(CrawlSpeed, remaining);
                float nextX = Projectile.Center.X + direction * step;
                bool hasFloor = Weapons.PuppetGroundDustWave.TryFindGroundY(nextX, Projectile.Bottom.Y - 8f, out float groundY);

                if (!hasFloor || remaining <= 0f)
                {
                    Projectile.localAI[1] = 1f;
                    Projectile.velocity = Vector2.Zero;

                    if (!Main.dedServ)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.6f, Pitch = -0.6f }, Projectile.Center);
                    }
                }
                else
                {
                    // Move by hand (velocity zero) so the hitbox bottom sits on the floor every tick.
                    Projectile.velocity = Vector2.Zero;
                    Projectile.position.X += direction * step;
                    Projectile.position.Y = groundY - Projectile.height;
                    Projectile.localAI[0] += step;

                    // A decorative blaze every 4th tick along the path (zero damage: this projectile does the hurting).
                    bool dropBlaze = (int)Projectile.localAI[0] % 24 < (int)CrawlSpeed;
                    if (dropBlaze && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Bottom, new Vector2(direction * 0.5f, 0f), ModContent.ProjectileType<DestinedDeathBlaze>(), 0, 0f, Main.myPlayer, direction, 0f);
                    }
                }
            }
            else
            {
                Projectile.localAI[1]++;
            }

            int stoppedTicks = (int)Projectile.localAI[1];
            bool erupting = stoppedTicks > SimmerTicks;

            // Simmer: harmless while it gathers (no damage window the player can't see), embers boiling up off the spot.
            Projectile.hostile = !Stopped || erupting;

            if (stoppedTicks == SimmerTicks + 1)
            {
                // Grow into the column, keeping the bottom on the floor.
                Vector2 bottom = Projectile.Bottom;
                Projectile.width = ColumnWidth;
                Projectile.height = ColumnHeight;
                Projectile.Bottom = bottom;

                if (!Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.3f }, Projectile.Bottom);
                    UsefulFunctions.ScreenShake(Projectile.Bottom, 5f, 15, 6f, 800f);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 blazeSpot = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-24f, 24f), 0f);
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), blazeSpot, new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), -Main.rand.NextFloat(1f, 3f)), ModContent.ProjectileType<DestinedDeathBlaze>(), 0, 0f, Main.myPlayer, direction, 1f);
                    }
                }
            }

            if (stoppedTicks > SimmerTicks + EruptTicks)
            {
                Projectile.Kill();
                return;
            }

            Lighting.AddLight(Projectile.Center, DemonElement.LightColor(DemonTier.Ancient));

            if (Main.dedServ)
            {
                return;
            }

            if (!Stopped)
            {
                // Crawl: low red flames and black smoke licking forward off the front edge.
                Vector2 front = Projectile.Bottom + new Vector2(direction * CrawlWidth * 0.5f, -4f);
                Dust lick = Dust.NewDustPerfect(front + Main.rand.NextVector2Circular(6f, 4f), DustID.RedTorch, new Vector2(direction * 1.5f, -Main.rand.NextFloat(0.5f, 2f)), 40, default, 1.7f);
                lick.noGravity = true;
                DemonElement.SpawnAccent(DemonTier.Ancient, Projectile.position, Projectile.width, Projectile.height, new Vector2(0f, -1.2f), 1.3f);
            }
            else if (!erupting)
            {
                // Simmer: embers and smoke boiling up from a 48px spot, faster as the eruption nears.
                float simmerProgress = stoppedTicks / (float)SimmerTicks;
                for (int i = 0; i < 2; i++)
                {
                    Vector2 spot = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-24f, 24f), -2f);
                    Dust ember = Dust.NewDustPerfect(spot, DustID.RedTorch, new Vector2(0f, -MathHelper.Lerp(1f, 5f, simmerProgress)), 40, default, MathHelper.Lerp(1f, 2f, simmerProgress));
                    ember.noGravity = true;
                }
            }
            else
            {
                // Column: red flame pouring up the full 192px, black smoke on the edges.
                for (int i = 0; i < 4; i++)
                {
                    Vector2 spot = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-28f, 28f), -Main.rand.NextFloat(0f, 40f));
                    Dust flame = Dust.NewDustPerfect(spot, DustID.RedTorch, new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), -Main.rand.NextFloat(6f, 10f)), 30, default, 2.2f);
                    flame.noGravity = true;
                }

                DemonElement.SpawnAccent(DemonTier.Ancient, Projectile.position, Projectile.width, Projectile.height, new Vector2(0f, -4f), 1.6f);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            DemonElement.ApplyHitDebuff(target, DemonTier.Ancient, 0, 0);
        }
    }
}
