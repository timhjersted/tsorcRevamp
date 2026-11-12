using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Accessories.Defensive.Shields;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // Blaidd's projectiles, kept together because they only exist for him (NPCs/Puppets/Blaidd.cs).
    // Design: tsorcDocs/Blaidd_Rework_Proposal.md.

    /// <summary>
    /// Azure Suppression Flask: lobbed on a gravity arc solved by Blaidd to land past the player, harmless in
    /// flight. Every ending (ground, timeout) shatters it into a <see cref="BlaiddSuppressionBurst"/>.
    /// ai[0] = spin direction (-1 / +1).
    /// </summary>
    public class BlaiddSuppressionFlask : ModProjectile
    {
        public const float Gravity = 0.2f;
        private const int FlightLifetimeTicks = 240;

        public override string Texture => UsefulFunctions.VanillaTextureFilepath(ItemID.FlaskofNanites);

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            // No damage of its own: the burst's debuffs are the whole attack.
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = FlightLifetimeTicks;
        }

        public override void AI()
        {
            Projectile.velocity.Y += Gravity;

            float spinDirection = 1f;

            if (Projectile.ai[0] < 0f)
            {
                spinDirection = -1f;
            }

            Projectile.rotation += 0.25f * spinDirection;
            Lighting.AddLight(Projectile.Center, 0.1f, 0.25f, 0.6f);

            if (Main.dedServ)
            {
                return;
            }

            // Sparse glowing trail so the arc reads against a dark arena.
            if (Main.rand.NextBool(3))
            {
                Dust trail = Dust.NewDustPerfect(Projectile.Center, DustID.BlueTorch, -Projectile.velocity * 0.2f, 80, default, 1.1f);
                trail.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.9f }, Projectile.Center);

                for (int i = 0; i < 16; i++)
                {
                    Dust glass = Dust.NewDustPerfect(Projectile.Center, DustID.Glass, Main.rand.NextVector2Circular(3f, 3f), 0, default, 1.1f);
                    glass.noGravity = false;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<BlaiddSuppressionBurst>(), 0, 0f, Main.myPlayer);
            }
        }
    }

    /// <summary>
    /// The flask's payload: a large blue cloud at the impact, plus a blue ring that expands to <see cref="MaxRadius"/>
    /// (100 tiles). Deliberately NOT avoidable (2026-10-29 design call): it is the in-world explanation for the
    /// debuffs, so every living player inside 100 tiles gets them the tick the ring front reaches them. No damage.
    /// Each client applies the debuffs to its OWN player (player buffs are owner-authoritative and sync from there).
    /// </summary>
    public class BlaiddSuppressionBurst : ModProjectile
    {
        public const float MaxRadius = 100f * 16f;
        public const float RingSpeed = 16f;
        public const int DebuffTicks = (int)(3.5f * 60f * 60f);
        private const float CloudRadius = 160f;
        private const int CloudLingerTicks = 90;
        private const int RingFadeTicks = 20;
        // Ring dust is spawned only near the local player: the full 1600px ring is ~10,000px around, which would be
        // hundreds of dust a tick. 20px spacing along the arc, capped per tick.
        private const float RingDustSpacing = 20f;
        private const float RingDustCullDistance = 1100f;
        private const int RingDustCapPerTick = 60;

        private static int RingTravelTicks => (int)Math.Ceiling(MaxRadius / RingSpeed);

        private bool _appliedToLocalPlayer;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = RingTravelTicks + RingFadeTicks;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
            float age = Projectile.localAI[0];
            float ringRadius = Math.Min(age * RingSpeed, MaxRadius);

            if (Main.dedServ)
            {
                return;
            }

            if (age == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item107 with { Volume = 1f, Pitch = -0.3f }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.5f }, Projectile.Center);
                SpawnCloudBurst();
            }

            // Applied once, when the ring front passes the local player's distance from the impact.
            Player localPlayer = Main.LocalPlayer;
            bool localPlayerAlive = localPlayer.active && !localPlayer.dead;

            if (!_appliedToLocalPlayer && localPlayerAlive)
            {
                float localDistance = localPlayer.Distance(Projectile.Center);
                bool ringReachedPlayer = localDistance <= ringRadius && localDistance <= MaxRadius;

                if (ringReachedPlayer)
                {
                    _appliedToLocalPlayer = true;
                    localPlayer.AddBuff(ModContent.BuffType<Suppressed>(), DebuffTicks);
                    localPlayer.AddBuff(ModContent.BuffType<GrappleMalfunction>(), DebuffTicks);
                    localPlayer.GetModPlayer<BlaiddSuppressionPlayer>().SuppressedByBlaidd = true;
                    localPlayer.GetModPlayer<BlaiddSuppressionPlayer>().SuppressionExpiresAt = Main.GameUpdateCount + (ulong)DebuffTicks;
                    SpawnPlayerHaze(localPlayer);
                }
            }

            // Lingering cloud at the impact.
            if (age <= CloudLingerTicks)
            {
                float cloudFade = 1f - age / CloudLingerTicks;
                int cloudDust = (int)MathHelper.Lerp(1f, 4f, cloudFade);

                for (int i = 0; i < cloudDust; i++)
                {
                    Vector2 offset = Main.rand.NextVector2Circular(CloudRadius, CloudRadius * 0.7f);
                    Dust mist = Dust.NewDustPerfect(Projectile.Center + offset, DustID.BlueTorch,
                        new Vector2(0f, -Main.rand.NextFloat(0.2f, 0.8f)), 120, default, Main.rand.NextFloat(1.2f, 2f) * cloudFade + 0.4f);
                    mist.noGravity = true;
                }
            }

            // Ring front. It fades over the last RingFadeTicks of the projectile instead of popping out.
            float ringFade = MathHelper.Clamp(Projectile.timeLeft / (float)RingFadeTicks, 0f, 1f);
            float circumference = MathHelper.TwoPi * ringRadius;
            int points = (int)(circumference / RingDustSpacing);

            if (points < 12)
            {
                points = 12;
            }

            int spawned = 0;

            for (int i = 0; i < points && spawned < RingDustCapPerTick; i++)
            {
                float angle = MathHelper.TwoPi * i / points;
                Vector2 direction = angle.ToRotationVector2();
                Vector2 position = Projectile.Center + direction * ringRadius;
                bool nearLocalPlayer = Vector2.Distance(position, localPlayer.Center) <= RingDustCullDistance;

                if (!nearLocalPlayer || !Main.rand.NextBool(2))
                {
                    continue;
                }

                spawned++;
                Dust front = Dust.NewDustPerfect(position, DustID.IceTorch, direction * 1.5f, 60, default, 1.6f * ringFade + 0.3f);
                front.noGravity = true;
            }
        }

        private void SpawnCloudBurst()
        {
            for (int i = 0; i < 120; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(9f, 6f);
                int dustType = DustID.BlueTorch;

                if (i % 3 == 0)
                {
                    dustType = DustID.IceTorch;
                }

                Dust burst = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 80, default, Main.rand.NextFloat(1.6f, 2.6f));
                burst.noGravity = true;
            }
        }

        private static void SpawnPlayerHaze(Player player)
        {
            for (int i = 0; i < 24; i++)
            {
                Dust haze = Dust.NewDustDirect(player.position, player.width, player.height, DustID.BlueTorch, 0f, -1.5f, 100, default, 1.4f);
                haze.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    /// <summary>
    /// Ends the flask's debuffs once no Blaidd is alive (killed or withdrawn), so winning the fight doesn't leave the
    /// player crippled for minutes. Only debuffs this player got from the flask are tracked. Runs on the owning client.
    /// </summary>
    public class BlaiddSuppressionPlayer : ModPlayer
    {
        public bool SuppressedByBlaidd;
        public ulong SuppressionExpiresAt;

        public override void PostUpdateBuffs()
        {
            if (!SuppressedByBlaidd)
            {
                return;
            }

            int suppressedType = ModContent.BuffType<Suppressed>();
            int grappleType = ModContent.BuffType<GrappleMalfunction>();
            bool stillDebuffed = Player.HasBuff(suppressedType) || Player.HasBuff(grappleType);

            if (!stillDebuffed)
            {
                SuppressedByBlaidd = false;
                return;
            }

            if (NPC.AnyNPCs(ModContent.NPCType<Blaidd>()) && Main.GameUpdateCount < SuppressionExpiresAt)
            {
                return;
            }

            if (Player.GetModPlayer<Dworc.DworcSuppressionPlayer>().HasActiveSuppression)
            {
                SuppressedByBlaidd = false;
                return;
            }

            Player.ClearBuff(suppressedType);
            Player.ClearBuff(grappleType);
            SuppressedByBlaidd = false;
        }
    }

    /// <summary>
    /// Blaidd's shield, torn off and hurled away at the phase-two transition. Visual only: tumbles on gravity, rests
    /// on the ground, then fades. ai[0] = throw direction (-1 / +1).
    /// </summary>
    public class BlaiddDiscardedShield : ModProjectile
    {
        private const float Gravity = 0.3f;
        private const int RestTicks = 120;
        private const int FadeTicks = 30;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(SpikedIronShield));

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            bool resting = Projectile.ai[1] > 0f;

            if (resting)
            {
                Projectile.ai[1]++;
                Projectile.velocity = Vector2.Zero;
                int restedTicks = (int)Projectile.ai[1];
                int fadeStart = RestTicks - FadeTicks;

                if (restedTicks > fadeStart)
                {
                    Projectile.alpha = (int)MathHelper.Lerp(0f, 255f, (restedTicks - fadeStart) / (float)FadeTicks);
                }

                if (restedTicks >= RestTicks)
                {
                    Projectile.Kill();
                }

                return;
            }

            Projectile.velocity.Y += Gravity;

            float tumbleDirection = 1f;

            if (Projectile.ai[0] < 0f)
            {
                tumbleDirection = -1f;
            }

            Projectile.rotation += 0.3f * tumbleDirection;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Land flat and rest instead of dying on contact.
            Projectile.ai[1] = 1f;
            Projectile.rotation = 0f;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 10; i++)
            {
                Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Smoke, 0f, -0.6f, 120, default, 1f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color color = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
            return false;
        }
    }

    /// <summary>
    /// Draws one BlaiddMoonflame.fx quad additively, standing on the floor: the Moonlight Crescent's flame wall and the
    /// Moonrise Slam's crest. floorCenter is the bottom-centre of the quad, size = (width, height) px, direction the
    /// travel direction (tongues lean back against it), seed a per-projectile noise offset. Call from PreDraw; the
    /// default projectile batch is restored afterwards.
    /// </summary>
    public static class BlaiddMoonflameVisuals
    {
        // Tongue lean: uv.x shift per unit of flame height, signed against the travel direction.
        private const float TongueLean = 0.25f;

        private static Asset<Effect> _effect;
        private static Asset<Texture2D> _noise;

        public static void Draw(Vector2 floorCenter, Vector2 size, float direction, float opacity, float seed,
            Color core, Color body, Color rim)
        {
            if (Main.dedServ || opacity <= 0.01f)
            {
                return;
            }

            _effect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/BlaiddMoonflame", AssetRequestMode.ImmediateLoad);
            _noise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/T_PerlinNoise_Tiled", AssetRequestMode.ImmediateLoad);
            Effect effect = _effect.Value;
            Texture2D noise = _noise.Value;

            Main.spriteBatch.End();

            try
            {
                effect.CurrentTechnique = effect.Techniques["BlaiddMoonflame"];
                effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Opacity"]?.SetValue(opacity);
                effect.Parameters["Lean"]?.SetValue(-direction * TongueLean);
                effect.Parameters["Seed"]?.SetValue(seed);
                effect.Parameters["CoreColor"]?.SetValue(core.ToVector3());
                effect.Parameters["BodyColor"]?.SetValue(body.ToVector3());
                effect.Parameters["RimColor"]?.SetValue(rim.ToVector3());

                // 2px blocks across the on-screen quad, pre-divided here (ps_2_0 has no preshader).
                float blocksX = Math.Max(2f, size.X / 2f);
                float blocksY = Math.Max(2f, size.Y / 2f);
                effect.Parameters["PixelGrid"]?.SetValue(new Vector4(blocksX, blocksY, 1f / blocksX, 1f / blocksY));

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                    DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);

                // The noise texture is the quad; origin at its bottom-centre so the flame stands on floorCenter.
                Vector2 scale = size / noise.Size();
                Vector2 origin = new Vector2(noise.Width * 0.5f, noise.Height);
                Main.spriteBatch.Draw(noise, floorCenter - Main.screenPosition, null, Color.White, 0f, origin, scale, SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }
            finally
            {
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
    }

    /// <summary>
    /// Moonlight Crescent: a wall of bright turquoise moonflame released along the floor by Blaidd's phase-2 overhead.
    /// Travels TravelSpeed px/t for TravelLimit px (covers his 460px trigger range), rides stairs and slopes, and ends on
    /// a wall or pit with a flame puff. Hitbox is 3 tiles tall (jumpable). Drawn with the BlaiddMoonflame shader: a 56x96
    /// lead wall plus a 40x64 trailing wall 20px behind (the visual covers the 28x48 hitbox), with sparse embers.
    /// ai[0] = travel direction (-1 / +1).
    /// </summary>
    public class BlaiddMoonlightCrescent : ModProjectile
    {
        public const float TravelSpeed = 9.5f;
        private const float TravelLimit = 30f * 16f;
        private const int ArmDelayTicks = 4;
        private const float MaxClimbPixels = 32f;
        private const float MaxDropPixels = 80f;
        private const int HitboxHeight = 48;
        private const float FlameHeight = 80f;
        private const int FadeTicks = 10;
        private const int FadeInTicks = 4;
        private static readonly Vector2 LeadWallSize = new Vector2(56f, 96f);
        private static readonly Vector2 TrailWallSize = new Vector2(40f, 64f);
        private const float TrailWallOffset = 20f;
        // Bright turquoise flame palette: core, body, rim.
        private static readonly Color FlameCore = new Color(200, 255, 250);
        private static readonly Color FlameBody = new Color(40, 230, 220);
        private static readonly Color FlameRim = new Color(0, 160, 200);

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private float Direction => Projectile.ai[0] >= 0f ? 1f : -1f;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = HitboxHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = (int)(TravelLimit / TravelSpeed) + 2;
            Projectile.alpha = 255;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > ArmDelayTicks;

        public override void AI()
        {
            Projectile.localAI[0]++;

            float nextX = Projectile.Center.X + Direction * TravelSpeed;
            bool foundGround = PuppetGroundDustWave.TryFindGroundY(nextX, Projectile.Bottom.Y, out float groundY);

            if (!foundGround)
            {
                Projectile.Kill();
                return;
            }

            float verticalStep = groundY - Projectile.Bottom.Y;
            bool hitWall = verticalStep < -MaxClimbPixels;
            bool fellOffEdge = verticalStep > MaxDropPixels;

            if (hitWall || fellOffEdge)
            {
                Projectile.Kill();
                return;
            }

            float bottomX = Projectile.Bottom.X;
            Projectile.Bottom = new Vector2(bottomX, groundY);
            Projectile.velocity = new Vector2(Direction * TravelSpeed, 0f);
            Lighting.AddLight(Projectile.Center, 0.15f, 0.9f, 0.85f);

            if (Main.dedServ)
            {
                return;
            }

            // Sparse flame licks over the shader wall: rising, leaning back from the travel direction, shrinking with
            // height. The shader carries the body of the flame; these only break up its silhouette.
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            int lickCount = 1;

            if (Main.rand.NextBool(2))
            {
                lickCount = 2;
            }

            for (int i = 0; i < lickCount; i++)
            {
                float rise = Main.rand.NextFloat(0f, FlameHeight);
                float heightFraction = rise / FlameHeight;
                Vector2 position = Projectile.Bottom + new Vector2(-Direction * rise * 0.25f + Main.rand.NextFloat(-10f, 10f), -rise);
                Vector2 velocity = new Vector2(-Direction * Main.rand.NextFloat(0.3f, 1.2f), -Main.rand.NextFloat(1.5f, 3.5f));
                Color color = Color.Lerp(FlameBody, FlameRim, heightFraction);

                if (heightFraction < 0.3f)
                {
                    color = Color.Lerp(FlameCore, FlameBody, heightFraction / 0.3f);
                }

                float scale = MathHelper.Lerp(1.6f, 0.7f, heightFraction) * fade;
                Dust flame = Dust.NewDustPerfect(position, DustID.RainbowMk2, velocity, 0, color, scale);
                flame.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                Dust ember = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-12f, 12f), -4f),
                    DustID.IceTorch, new Vector2(Direction * Main.rand.NextFloat(0.5f, 2f), -Main.rand.NextFloat(1f, 3f)),
                    60, default, Main.rand.NextFloat(1.2f, 1.7f) * fade);
                ember.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-10f, 10f), -Main.rand.NextFloat(0f, 40f));
                Dust puff = Dust.NewDustPerfect(position, DustID.RainbowMk2, Main.rand.NextVector2Circular(2.5f, 2.5f), 0, FlameBody,
                    Main.rand.NextFloat(1f, 1.5f));
                puff.noGravity = true;
            }
        }

        // Fades in over FadeInTicks and out over the last FadeTicks. The trailing wall is dimmer and uses its own noise
        // seed so the two never flicker in step.
        public override bool PreDraw(ref Color lightColor)
        {
            float fadeIn = MathHelper.Clamp(Projectile.localAI[0] / FadeInTicks, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            float opacity = fadeIn * fadeOut;
            float seed = Projectile.identity * 0.137f % 1f;

            Vector2 trailFloor = Projectile.Bottom + new Vector2(-Direction * TrailWallOffset, 0f);
            BlaiddMoonflameVisuals.Draw(trailFloor, TrailWallSize, Direction, opacity * 0.7f, seed + 0.5f, FlameCore, FlameBody, FlameRim);
            BlaiddMoonflameVisuals.Draw(Projectile.Bottom, LeadWallSize, Direction, opacity, seed, FlameCore, FlameBody, FlameRim);
            return false;
        }
    }

    /// <summary>
    /// Moonrise Slam's ground shockwave: a thin blue front riding the floor away from the landing. Spawned as a
    /// mirrored pair, 8 px/t x 40t = 320px to each side. Low enough to jump; a roll goes through it. Drawn as a 44x44
    /// blue BlaiddMoonflame crest (covers the 22x34 hitbox) with a few spray dusts.
    /// Same floor-riding rules as <see cref="TaurusQuakeShockwave"/>. ai[0] = travel direction (-1 / +1).
    /// </summary>
    public class BlaiddMoonWave : ModProjectile
    {
        public const float TravelSpeed = 8f;
        public const int LifetimeTicks = 40;
        // Harmless for the first ticks so the landing dust leads the damage.
        private const int ArmDelayTicks = 6;
        private const float MaxClimbPixels = 32f;
        private const float MaxDropPixels = 80f;
        private const int WaveHeight = 34;
        private const int FadeTicks = 12;
        private static readonly Vector2 CrestSize = new Vector2(44f, 44f);
        private static readonly Color CrestCore = new Color(215, 235, 255);
        private static readonly Color CrestBody = new Color(90, 150, 255);
        private static readonly Color CrestRim = new Color(40, 80, 210);
        // Seconds x 60. Not specified in the design ask; matched to the sword's 6s Withered Armor.
        private const int WeakTicks = 6 * 60;
        private const int ManaSicknessTicks = 6 * 60;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private float Direction => Projectile.ai[0] >= 0f ? 1f : -1f;

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = WaveHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = LifetimeTicks;
            Projectile.alpha = 255;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > ArmDelayTicks;

        // Runs on the hit player's machine (hostile projectile), which owns that player's buffs.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Weak, WeakTicks);
            target.AddBuff(BuffID.ManaSickness, ManaSicknessTicks);
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            float nextX = Projectile.Center.X + Direction * TravelSpeed;
            bool foundGround = PuppetGroundDustWave.TryFindGroundY(nextX, Projectile.Bottom.Y, out float groundY);

            if (!foundGround)
            {
                Projectile.Kill();
                return;
            }

            float verticalStep = groundY - Projectile.Bottom.Y;
            bool hitWall = verticalStep < -MaxClimbPixels;
            bool fellOffEdge = verticalStep > MaxDropPixels;

            if (hitWall || fellOffEdge)
            {
                Projectile.Kill();
                return;
            }

            float bottomX = Projectile.Bottom.X;
            Projectile.Bottom = new Vector2(bottomX, groundY);
            Projectile.velocity = new Vector2(Direction * TravelSpeed, 0f);
            Lighting.AddLight(Projectile.Center, 0.1f, 0.25f, 0.6f);

            if (Main.dedServ)
            {
                return;
            }

            // Spray off the crest; the shader draws the crest itself.
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            int dustCount = 1;

            for (int i = 0; i < dustCount; i++)
            {
                float rise = Main.rand.NextFloat(0f, Projectile.height);
                Vector2 position = Projectile.Bottom + new Vector2(rise * 0.3f * Direction + Main.rand.NextFloat(-6f, 6f), -rise);
                Vector2 velocity = new Vector2(Direction * Main.rand.NextFloat(1.2f, 3f), -Main.rand.NextFloat(0.4f, 1.6f));
                Dust crest = Dust.NewDustPerfect(position, DustID.IceTorch, velocity, 60, default, Main.rand.NextFloat(1.1f, 1.6f) * fade);
                crest.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 12; i++)
            {
                Dust puff = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-8f, 8f), -Main.rand.NextFloat(0f, 20f)),
                    DustID.BlueTorch, Main.rand.NextVector2Circular(2f, 2f), 90, default, Main.rand.NextFloat(1f, 1.4f));
                puff.noGravity = true;
            }
        }

        // Fades in over the harmless ArmDelayTicks and out over the last FadeTicks.
        public override bool PreDraw(ref Color lightColor)
        {
            float fadeIn = MathHelper.Clamp(Projectile.localAI[0] / ArmDelayTicks, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
            float seed = Projectile.identity * 0.137f % 1f;
            BlaiddMoonflameVisuals.Draw(Projectile.Bottom, CrestSize, Direction, fadeIn * fadeOut, seed, CrestCore, CrestBody, CrestRim);
            return false;
        }
    }
}
