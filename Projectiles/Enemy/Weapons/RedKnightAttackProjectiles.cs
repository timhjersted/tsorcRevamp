using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    internal enum KnightStandardMode
    {
        RedKnight,
        GreatCenter,
        GreatLeft,
        GreatRight
    }

    internal enum RedKnightBurstKind
    {
        SpearImpact,
        StandardImpact,
        BombExplosion,
        /// <summary>
        /// The thrown-firebomb detonation. Draws the same RedKnightBombBlast technique as
        /// <see cref="BombExplosion"/> but as TWO layered shells (a big fast faint outer one behind
        /// a tighter core) so it reads as a volume instead of one flat disc. Kept as a separate
        /// kind so the plain BombExplosion look is untouched for its other callers
        /// (EnemyGreatAttack, the planted Red Knight standard bomb).
        /// </summary>
        BombExplosionLayered
    }

    public class RedKnightLungeHitbox : ModProjectile
    {
        const float SourceOverlap = 22f;

        public override string Texture => "tsorcRevamp/NPCs/Puppets/PuppetPlaceholder";

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 48;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 12;
            Projectile.tileCollide = false;
            Projectile.alpha = 255;
            Projectile.hide = true;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 center = Projectile.Center;
            Projectile.width = Math.Max(1, (int)Projectile.ai[0]);
            Projectile.height = Math.Max(1, (int)Projectile.ai[1]);
            Projectile.timeLeft = Math.Max(1, (int)Projectile.ai[2]);
            Projectile.Center = center;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            tsorcGlobalProjectile globalProjectile = Projectile.GetGlobalProjectile<tsorcGlobalProjectile>();
            if (!globalProjectile.TryGetSourceNPC(out NPC sourceNPC) || !sourceNPC.active)
            {
                Projectile.Kill();
                return;
            }

            int direction = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.Center = sourceNPC.Center
                + new Vector2(direction * (Projectile.width * 0.5f - SourceOverlap), -4f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            tsorcGlobalProjectile globalProjectile = Projectile.GetGlobalProjectile<tsorcGlobalProjectile>();
            if (globalProjectile.TryGetSourceNPC(out NPC sourceNPC)
                && sourceNPC.ModNPC is IHumanoidMeleeHitEffects hitEffects)
            {
                hitEffects.OnHumanoidMeleeHit(target);
            }
        }
    }

    /// <summary>
    /// Adder's Thread flies on a fixed 36-tick arc to the sampled ground point, plants there, then
    /// recalls to its source knight. The visible poison thread and its collision use the same line.
    /// </summary>
    public class RedKnightAdderSpear : ModProjectile
    {
        const int FlightTicks = 36;
        const int RecallStart = 93;
        const int RecallTicks = 18;
        int Age => (int)Projectile.localAI[0];
        Vector2 PlantPoint => new(Projectile.ai[1], Projectile.ai[2]);

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/BlackKnightSpear";

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = RecallStart + RecallTicks + 12;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.scale = 0.8f;
        }

        public override void AI()
        {
            int sourceIndex = (int)Projectile.ai[0];
            if (sourceIndex < 0 || sourceIndex >= Main.maxNPCs || !Main.npc[sourceIndex].active)
            {
                Projectile.Kill();
                return;
            }
            NPC source = Main.npc[sourceIndex];

            if (Age < FlightTicks)
            {
                Projectile.velocity.Y += 0.26f;
                Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            }
            else if (Age < RecallStart)
            {
                Projectile.Center = PlantPoint;
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation = 0f;
                Lighting.AddLight(Projectile.Center, new Vector3(0.16f, 0.28f, 0.02f));
                if (Main.rand.NextBool(3))
                {
                    Dust mote = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 8f),
                        DustID.CursedTorch, Main.rand.NextVector2Circular(0.3f, 0.3f), 120,
                        new Color(138, 204, 24), Main.rand.NextFloat(0.55f, 0.85f));
                    mote.noGravity = true;
                }
            }
            else
            {
                float progress = MathHelper.Clamp((Age - RecallStart + 1f) / RecallTicks, 0f, 1f);
                Vector2 recallPoint = source.Center + new Vector2(source.direction * 18f, -7f);
                Vector2 previous = Projectile.Center;
                Projectile.Center = Vector2.Lerp(PlantPoint, recallPoint, progress * progress);
                Projectile.velocity = Projectile.Center - previous;
                Projectile.rotation = Projectile.velocity.SafeNormalize(new Vector2(source.direction, 0f)).ToRotation()
                    + MathHelper.PiOver2;
            }

            Projectile.localAI[0]++;
        }

        public override bool? CanDamage() => Age < FlightTicks || (Age >= RecallStart && Age < RecallStart + RecallTicks);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Age < RecallStart)
            {
                return base.Colliding(projHitbox, targetHitbox);
            }
            NPC source = Main.npc[(int)Projectile.ai[0]];
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                source.Center, Projectile.Center, 11f, ref collisionPoint);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 5 * 60);
            target.AddBuff(BuffID.Darkness, 10 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Age >= FlightTicks)
            {
                NPC source = Main.npc[(int)Projectile.ai[0]];
                float charge = MathHelper.Clamp((Age - FlightTicks) / (float)(RecallStart - FlightTicks), 0f, 1f);
                DrawThread(source.Center, Projectile.Center, charge, Age >= RecallStart);
            }
            return true;
        }

        static void DrawThread(Vector2 start, Vector2 end, float charge, bool active)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 2f) return;
            float rotation = delta.ToRotation();
            Vector2 origin = new(0f, 0.5f);
            Color shadow = new Color(8, 18, 1) * (0.45f + charge * 0.35f);
            Color body = (active ? new Color(190, 245, 45) : new Color(88, 142, 12)) * (0.35f + charge * 0.55f);
            Main.EntitySpriteDraw(pixel, start - Main.screenPosition, null, shadow, rotation, origin,
                new Vector2(length, active ? 13f : 8f), SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(pixel, start - Main.screenPosition, null, body, rotation, origin,
                new Vector2(length, active ? 4f : 2f), SpriteEffects.None, 0f);
        }
    }

    /// <summary>
    /// A small Storm Herald aperture that materializes one blood lance. It owns both the warning
    /// and the release so the portal, dust burst, and server-authored projectile cannot drift apart.
    /// </summary>
    public class RedCourtLancePortal : ModProjectile
    {
        const int MaterializeTicks = 24;
        const int ReleaseFadeTicks = 12;
        int Age => (int)Projectile.localAI[0];

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.timeLeft = MaterializeTicks + ReleaseFadeTicks + 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            int sourceIndex = (int)Projectile.ai[0];
            if (sourceIndex < 0 || sourceIndex >= Main.maxNPCs || !Main.npc[sourceIndex].active)
            {
                Projectile.Kill();
                return;
            }

            if (Age < MaterializeTicks && Main.rand.NextBool(2))
            {
                Vector2 dustPosition = Projectile.Center + Main.rand.NextVector2CircularEdge(28f, 18f);
                Dust gather = Dust.NewDustPerfect(dustPosition,
                    Main.rand.NextBool(2) ? DustID.Blood : DustID.Wraith,
                    (Projectile.Center - dustPosition).SafeNormalize(Vector2.Zero)
                        * Main.rand.NextFloat(0.7f, 1.8f),
                    105, default, Main.rand.NextFloat(0.75f, 1.12f));
                gather.noGravity = true;
            }

            if (Age == MaterializeTicks)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center,
                        Projectile.velocity, ModContent.ProjectileType<EnemyAncientBloodLanceProj>(),
                        Projectile.damage, 0f, Main.myPlayer, ai2: 1f);
                }
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.42f, Pitch = -0.5f }, Projectile.Center);
                    for (int i = 0; i < 22; i++)
                    {
                        bool blood = i % 2 == 0;
                        Vector2 velocity = Main.rand.NextVector2Circular(3.8f, 2.8f)
                            - Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.2f, 1.2f);
                        Dust burst = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 8f),
                            blood ? DustID.Blood : DustID.Wraith, velocity, blood ? 80 : 120,
                            default, Main.rand.NextFloat(0.9f, 1.38f));
                        burst.noGravity = true;
                    }
                }
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.42f, 0.025f, 0.035f)
                * (Age < MaterializeTicks ? 0.5f : 0.85f));
            Projectile.localAI[0]++;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Age / (float)MaterializeTicks, 0f, 1f);
            float opacity = Age <= MaterializeTicks
                ? MathHelper.Lerp(0.2f, 0.92f, progress)
                : MathHelper.Clamp(1f - (Age - MaterializeTicks) / (float)ReleaseFadeTicks, 0f, 1f);
            RedKnightVFX.DrawCourtPortal(Projectile.Center, progress, opacity,
                Projectile.identity * 0.47f + Projectile.ai[1] * 0.31f);
            return false;
        }
    }

    public class RedKnightStandard : ModProjectile
    {
        Vector2 startPosition;
        bool initialized;
        int dynamicFlightTicks;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemyAncientBloodLanceProj";

        KnightStandardMode Mode => (KnightStandardMode)(int)Projectile.ai[2];
        int FlightTicks => Mode == KnightStandardMode.RedKnight ? 24 : 30;
        int ResolvedFlightTicks => dynamicFlightTicks > 0
            ? dynamicFlightTicks
            : FlightTicks;
        // Great mode is Royal Standard's stronger centre throw; the ordinary Red Knight keeps the
        // shorter charge used by Crimson Standard.
        int ChargeTicks => Mode == KnightStandardMode.RedKnight ? 60 : 120;
        Vector2 GroundPoint => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        // +5px versus the first pass: the spear was reading as hovering just above the floor
        // instead of driven into it.
        Vector2 PlantedCenter => GroundPoint - new Vector2(0f, 10f);
        /// <summary>Where the flame's ground band sits — the same +5px down as the spear.</summary>
        Vector2 FlameAnchor => GroundPoint + new Vector2(0f, 5f);

        const float RoyalGravityPerTick = 0.28f;

        static float CalculateArcHeight(Vector2 source, Vector2 plantedCenter)
            => MathHelper.Clamp(Vector2.Distance(source, plantedCenter) * 0.35f, 64f, 320f);

        static int CalculateFlightTicks(Vector2 source, Vector2 plantedCenter, bool weightyRoyalArc)
        {
            float distance = Vector2.Distance(source, plantedCenter);
            if (!weightyRoyalArc)
            {
                return Math.Max(20, (int)(distance / 13f));
            }

            // y = lerpY - 4H*p*(1-p) has constant acceleration 8H/T^2. Solve T from
            // the desired Terraria-like gravity, then also cap horizontal travel near 9px/tick.
            float gravityTicks = MathF.Sqrt(8f * CalculateArcHeight(source, plantedCenter) / RoyalGravityPerTick);
            float horizontalTicks = MathF.Abs(plantedCenter.X - source.X) / 9f;
            return (int)MathHelper.Clamp(MathF.Ceiling(MathF.Max(gravityTicks, horizontalTicks)), 36f, 150f);
        }

        static Vector2 CalculateFlightPosition(Vector2 source, Vector2 plantedCenter, float progress,
            bool weightyRoyalArc)
        {
            float arcHeight = CalculateArcHeight(source, plantedCenter);
            Vector2 position = Vector2.Lerp(source, plantedCenter, progress);
            float arcEnvelope = weightyRoyalArc
                ? 4f * progress * (1f - progress)
                : System.MathF.Sin(progress * MathHelper.Pi);
            position.Y -= arcEnvelope * arcHeight;
            return position;
        }

        // Used by the held Royal/Crimson Standard pose. It samples the same first segment the
        // projectile will traverse, so the lance previews its true upward launch tangent.
        internal static Vector2 InitialFlightDirection(Vector2 source, Vector2 groundPoint,
            bool weightyRoyalArc)
        {
            Vector2 plantedCenter = groundPoint - new Vector2(0f, 10f);
            int flightTicks = CalculateFlightTicks(source, plantedCenter, weightyRoyalArc);
            return (CalculateFlightPosition(source, plantedCenter, 1f / flightTicks, weightyRoyalArc) - source)
                .SafeNormalize(Vector2.UnitY);
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 62;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FlightTicks + ChargeTicks + 40;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.scale = 0.8f;
            // Required for DrawBehind to be honoured; the projectile is drawn manually from the
            // layer DrawBehind assigns it to instead of the default projectile pass.
            Projectile.hide = true;
        }

        /// <summary>
        /// Draw the planted spear (and its flame) BEHIND tiles, so it reads as physically driven
        /// into the ground rather than pasted on top of it. Matches the repo's existing convention
        /// for this — e.g. ArtoriasImpalingSword uses the same DrawBehind + Projectile.hide pair.
        /// The flame rides the same layer, which is what we want: everything above the floor is in
        /// open air and unoccluded, while the shader's new downward billow is naturally masked by
        /// the floor it is burning against.
        /// </summary>
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCsAndTiles.Add(index);
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage()
        {
            int age = (int)Projectile.localAI[0];
            bool physicalSpear = Mode == KnightStandardMode.RedKnight || Mode == KnightStandardMode.GreatCenter;
            return physicalSpear && age < ResolvedFlightTicks;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            // Match the visible lance's instantaneous tangent, not the straight start-to-impact
            // chord underneath its high arc.
            Vector2 direction = Projectile.velocity.SafeNormalize(
                (PlantedCenter - startPosition).SafeNormalize(Vector2.UnitY));
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - direction * 24f, Projectile.Center + direction * 24f,
                7f, ref collisionPoint);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), 600);
        }

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                startPosition = Projectile.Center;
                dynamicFlightTicks = CalculateFlightTicks(
                    startPosition, PlantedCenter, Mode != KnightStandardMode.RedKnight);
                // ai[1] is GroundPoint.Y. Overwriting it with the flight duration retargeted the
                // lance to world Y ~= 20-30 on the very next tick, so Royal Standard appeared to
                // telegraph and then never execute. Keep duration separate and sync it below.
                Projectile.timeLeft = dynamicFlightTicks + ChargeTicks + 80;
                if (Main.netMode == NetmodeID.Server)
                {
                    Projectile.netUpdate = true;
                }
            }

            int flightDuration = ResolvedFlightTicks;
            int age = (int)Projectile.localAI[0]++;
            if (age < flightDuration)
            {
                bool weightyRoyalArc = Mode != KnightStandardMode.RedKnight;
                float progress = (float)age / flightDuration;
                Vector2 currentPos = CalculateFlightPosition(
                    startPosition, PlantedCenter, progress, weightyRoyalArc);
                Projectile.Center = currentPos;

                float nextProgress = System.Math.Min(1f, (float)(age + 1) / flightDuration);
                Vector2 nextPos = CalculateFlightPosition(
                    startPosition, PlantedCenter, nextProgress, weightyRoyalArc);

                Vector2 flightDir = nextPos - currentPos;
                if (flightDir.LengthSquared() > 0.001f)
                {
                    Projectile.velocity = flightDir;
                    Projectile.rotation = flightDir.ToRotation() + MathHelper.PiOver2;
                }
                return;
            }

            Projectile.Center = PlantedCenter;
            int plantedAge = age - flightDuration;
            if (plantedAge == 0)
            {
                PlaySound(SoundID.Dig with { Volume = 0.65f, Pitch = -0.15f }, GroundPoint);
                float impactScale = Mode == KnightStandardMode.RedKnight ? 0.75f : 1f;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        float angle = i * (MathHelper.TwoPi / 16f);
                        Vector2 velocity = angle.ToRotationVector2() * 2.6f;
                        float dir = System.MathF.Cos(angle) < 0f ? -1f : 1f;
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), GroundPoint, velocity,
                            ModContent.ProjectileType<DestinedDeathBlaze>(), 0, 0f, Main.myPlayer,
                            dir, 1f);
                    }
                }
                // Hardmode gets the DestinedDeathExplosion sheet + red/black dust; pre-hardmode
                // keeps the original burst untouched.
                if (!DestinedDeathExplosion.TrySpawn(Projectile.GetSource_FromThis(),
                        GroundPoint, impactScale))
                {
                    SpawnBurst(RedKnightBurstKind.StandardImpact, GroundPoint, impactScale);
                }
                EmitPlantDust();
            }
            if (plantedAge == ChargeTicks)
            {
                FireWaves();
            }
            if (plantedAge > ChargeTicks + 35)
            {
                Projectile.Kill();
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(initialized);
            writer.Write(startPosition.X);
            writer.Write(startPosition.Y);
            writer.Write(dynamicFlightTicks);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            initialized = reader.ReadBoolean();
            startPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            dynamicFlightTicks = reader.ReadInt32();
        }

        void FireWaves()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                switch (Mode)
                {
                    case KnightStandardMode.GreatLeft:
                        SpawnWave(1);
                        break;
                    case KnightStandardMode.GreatRight:
                        SpawnWave(-1);
                        break;
                    default:
                        SpawnWave(-1);
                        SpawnWave(1);
                        break;
                }
            }
            PlaySound(SoundID.Item74 with { Volume = 0.75f, Pitch = -0.4f }, GroundPoint);
            SpawnBurst(RedKnightBurstKind.StandardImpact, GroundPoint,
                Mode == KnightStandardMode.RedKnight ? 0.9f : 1.2f);
        }

        void EmitPlantDust()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 7; i++)
            {
                int dustType = i < 4 ? DustID.Stone : DustID.RedTorch;
                Dust dust = Dust.NewDustPerfect(GroundPoint + new Vector2(Main.rand.NextFloat(-7f, 7f), -2f),
                    dustType, new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-2.8f, -0.7f)),
                    100, default, Main.rand.NextFloat(0.65f, 1f));
                dust.noGravity = dustType == DustID.Torch;
            }
        }

        void SpawnWave(int direction)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), GroundPoint - new Vector2(0f, 9f),
                new Vector2(direction * (Mode == KnightStandardMode.RedKnight ? 6f : 7f), 0f),
                ModContent.ProjectileType<RedKnightGroundWave>(), Projectile.damage, 2.5f,
                Main.myPlayer, ai0: Mode == KnightStandardMode.RedKnight ? 210f : 520f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = (int)Projectile.localAI[0];
            if (age < FlightTicks)
            {
                Vector2 direction = (PlantedCenter - startPosition).SafeNormalize(Vector2.UnitY);
                // Was RedKnightVFX.DrawSpearWake; now the shared Black Knight grey wake. The old
                // `empowered` (non-RedKnight standards) becomes a larger, stronger quad.
                bool empoweredStandard = Mode != KnightStandardMode.RedKnight;
                EnemyVFX.DrawBlackKnightSpearWake(Projectile.Center - direction * 14f, direction.ToRotation(),
                    empoweredStandard ? new Vector2(76f, 17f) : new Vector2(68f, 15f),
                    empoweredStandard ? 0.56f : 0.48f);
                DrawSpearSprite(lightColor);
                return false;
            }

            // Order INVERTED versus the first pass: the spear sprite is drawn first and the flame
            // over the top of it, so the fire engulfs the planted spear instead of the spear
            // sitting flatly on top of its own flame.
            DrawSpearSprite(lightColor);
            float progress = MathHelper.Clamp((age - FlightTicks) / (float)Math.Max(1, ChargeTicks), 0f, 1f);
            RedKnightVFX.DrawStandardCharge(FlameAnchor, progress, Mode);
            return false;
        }

        void DrawSpearSprite(Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            float rotation = Projectile.rotation + MathHelper.PiOver4;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
                Projectile.GetAlpha(lightColor), rotation, texture.Size() * 0.5f,
                Projectile.scale, SpriteEffects.None, 0f);
        }

        static void SpawnBurst(RedKnightBurstKind kind, Vector2 position, float scale)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(new EntitySource_Misc("RedKnightVFX"), position, Vector2.Zero,
                    ModContent.ProjectileType<RedKnightVFXBurst>(), 0, 0f, Main.myPlayer, (float)kind, scale);
            }
        }

        static void PlaySound(SoundStyle sound, Vector2 position)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(sound, position);
            }
        }
    }

    public class RedKnightGroundWave : ModProjectile
    {
        // The nominal size handed to the VFX helper; the helper scales this up into the actual quad.
        static readonly Vector2 NominalVisual = new(54f, 20f);

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            // Hitbox is deliberately ~20% inside the VISIBLE flame in both axes, derived from the
            // shader's own reach constants rather than eyeballed, so the fire always looks a little
            // more dangerous than it is. Erring this way is always right (vfx-shader-tips §39): a
            // visual that overruns its hitbox feels generous, the reverse feels like a bug.
            float visibleWidth = RedKnightVFX.FlameReachWidth(
                RedKnightVFX.GroundWaveQuadWidth(NominalVisual.X));
            float visibleAbove = RedKnightVFX.FlameReachAbove(
                RedKnightVFX.GroundWaveQuadHeight(NominalVisual.Y));
            Projectile.width = Math.Max(8, (int)(visibleWidth * 0.8f));
            Projectile.height = Math.Max(8, (int)(visibleAbove * 0.8f));
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 84;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), 600);
        }

        public override void AI()
        {
            float travelLimit = Math.Max(64f, Projectile.ai[0]);
            Projectile.localAI[0] += Math.Abs(Projectile.velocity.X);
            if (Projectile.localAI[0] >= travelLimit)
            {
                Projectile.Kill();
                return;
            }

            float groundY = PuppetGroundDustWave.FindGroundY(Projectile.Center.X, Projectile.Center.Y + 9f);
            // The flame's ground band sits on Projectile.Bottom, and the hitbox now sits entirely
            // ABOVE that band (the shader's downward billow is decorative and carries no damage).
            Projectile.Center = new Vector2(Projectile.Center.X, groundY - Projectile.height * 0.5f);
            Lighting.AddLight(Projectile.Center, new Vector3(0.7f, 0.08f, 0.02f));

            SpawnBlazes(Math.Min(Projectile.localAI[0] / travelLimit, 1f));

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                int dustType = Main.rand.NextBool(4) ? DustID.Shadowflame : DustID.RedTorch;
                Dust ember = Dust.NewDustPerfect(Projectile.Bottom + new Vector2(Main.rand.NextFloat(-18f, 18f), -2f),
                    dustType, new Vector2(-Projectile.velocity.X * 0.08f, Main.rand.NextFloat(-2.4f, -0.8f)),
                    100, new Color(205, 24, 54), Main.rand.NextFloat(0.7f, 1.05f));
                ember.noGravity = true;
            }
        }

        /// <summary>
        /// Decorative DestinedDeathBlaze sprites riding the wave. Density RAMPS with how far the
        /// wave has travelled rather than being a fixed burst at t=0, and past ~65% of the run some
        /// of them lift off instead of hugging the floor, so the tail of the attack builds into a
        /// growing wall of flame rather than thinning out.
        /// Client-side only: these carry no hitbox, so there is nothing to keep in sync.
        /// </summary>
        void SpawnBlazes(float travelProgress)
        {
            if (Main.dedServ || Main.netMode == NetmodeID.Server)
            {
                return;
            }

            // 1-in-9 at the start rising to 1-in-3 at full extent. Keep the decorative layer
            // subordinate to the two damaging waves and their safe-space readability.
            int chance = Math.Max(3, (int)MathHelper.Lerp(9f, 3f, travelProgress));
            if (!Main.rand.NextBool(chance))
            {
                return;
            }

            for (int i = 0; i < 2; i++)
            {
                int direction = Projectile.velocity.X < 0f ? -1 : 1;
                // Lift-off only near the end of the run, and only for some of them.
                bool lifting = travelProgress > 0.65f && Main.rand.NextBool(2);
                Vector2 spawn = Projectile.Bottom
                    + new Vector2(Main.rand.NextFloat(-26f, 26f), Main.rand.NextFloat(-4f, 3f));
                Vector2 velocity = new(Projectile.velocity.X * Main.rand.NextFloat(0.55f, 0.95f),
                    Main.rand.NextFloat(-0.6f, 0.2f));

                Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawn, velocity,
                    ModContent.ProjectileType<DestinedDeathBlaze>(), 0, 0f, Main.myPlayer,
                    direction, lifting ? 1f : 0f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
            RedKnightVFX.DrawGroundWave(Projectile.Bottom, Projectile.velocity, NominalVisual, opacity);
            return false;
        }
    }

    public class RedKnightDelayedBomb : ModProjectile
    {
        const int FlightTicks = 36;
        const int PlantedTicks = 84;
        Vector2 startPosition;
        bool initialized;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/EnemyFirebomb";

        Vector2 GroundPoint => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        Vector2 RestingCenter => GroundPoint - new Vector2(0f, 9f);

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FlightTicks + PlantedTicks + 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                startPosition = Projectile.Center;
            }

            int age = (int)Projectile.localAI[0]++;
            if (age < FlightTicks)
            {
                float progress = MathHelper.SmoothStep(0f, 1f, (age + 1f) / FlightTicks);
                Projectile.Center = Vector2.Lerp(startPosition, RestingCenter, progress);
                Projectile.rotation += 0.24f;
                return;
            }

            Projectile.Center = RestingCenter;
            Projectile.rotation = 0f;
            int plantedAge = age - FlightTicks;
            if (plantedAge == 0)
            {
                PlaySound(SoundID.Dig with { Volume = 0.5f }, Projectile.Center);
            }
            if (plantedAge >= PlantedTicks)
            {
                Projectile.hostile = true;
                Projectile.Resize(120, 120);
                Projectile.Center = GroundPoint - new Vector2(0f, 40f);
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 2);
                Projectile.netUpdate = true;
            }
            Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 0.12f, 0.02f));
        }

        public override bool? CanDamage()
        {
            return Projectile.localAI[0] >= FlightTicks + PlantedTicks;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = (int)Projectile.localAI[0];
            float fuseProgress = age < FlightTicks ? 0f : MathHelper.Clamp((age - FlightTicks) / (float)PlantedTicks, 0f, 1f);
            Vector2 fusePoint = Projectile.Center + new Vector2(3f, -8f).RotatedBy(Projectile.rotation);
            RedKnightVFX.DrawBombFuse(fusePoint, fuseProgress, planted: age >= FlightTicks);
            return age < FlightTicks + PlantedTicks;
        }

        public override void OnKill(int timeLeft)
        {
            PlaySound(SoundID.Item74 with { PitchVariance = 0.5f }, Projectile.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<RedKnightVFXBurst>(), 0, 0f, Main.myPlayer,
                    (float)RedKnightBurstKind.BombExplosionLayered, 1f);
            }

            int dustCount = 52;
            float dustScale = 1.45f;
            for (int i = 0; i < dustCount; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X + 36, Projectile.position.Y + 36), Projectile.width - 74, Projectile.height - 74, 6, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 100, default(Color), dustScale);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 2.4f;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int blazeDamage = Projectile.damage;
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * (MathHelper.TwoPi / 8f);
                    Vector2 dir = angle.ToRotationVector2();
                    Vector2 blazeVel = dir * 0.56f;
                    int proj = Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, blazeVel,
                        ModContent.ProjectileType<EnemySpellBlaze>(), blazeDamage, 5f, Main.myPlayer);
                    if (proj >= 0 && proj < Main.maxProjectiles)
                    {
                        Main.projectile[proj].timeLeft = 90;
                    }
                }
            }

            if (!Main.dedServ)
            {
                // MIDGROUND — the body of the fireball. 80 motes of RedTorch, reach 68px, scale 0.75-1.5, noGravity.
                for (int i = 0; i < 80; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Unit();
                    bool soot = i % 4 == 0;
                    Dust ember = Dust.NewDustPerfect(
                        Projectile.Center + direction * Main.rand.NextFloat(4f, 68f),
                        soot ? DustID.Shadowflame : DustID.RedTorch,
                        direction * Main.rand.NextFloat(1.6f, 6.8f),
                        soot ? 150 : 90,
                        soot ? new Color(16, 3, 8) : new Color(214, 22, 42),
                        Main.rand.NextFloat(0.75f, 1.5f));
                    ember.noGravity = true;
                }

                // FOREGROUND — 35 tiny fast sparks that outrun the body and die quickly.
                for (int i = 0; i < 35; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Unit();
                    Dust spark = Dust.NewDustPerfect(
                        Projectile.Center + direction * Main.rand.NextFloat(6f, 26f),
                        DustID.Torch,
                        direction * Main.rand.NextFloat(7f, 12.5f),
                        60, new Color(255, 176, 96),
                        Main.rand.NextFloat(0.45f, 0.85f));
                    spark.noGravity = true;
                }
            }
        }

        static void PlaySound(SoundStyle sound, Vector2 position)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(sound, position);
            }
        }
    }

    public class RedKnightLightningLane : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        int Age => (int)Projectile.localAI[0];
        int TelegraphTicks => Math.Max(1, (int)Projectile.ai[0]);
        int ActiveTicks => Math.Max(1, Math.Abs((int)Projectile.ai[1]));
        bool SilentRelease => Projectile.ai[1] < 0f;
        float Length => Math.Max(64f, Projectile.ai[2]);

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 70;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Age == TelegraphTicks && !SilentRelease && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.62f, Pitch = -0.42f }, Projectile.Center);
            }
            Lighting.AddLight(Projectile.Center, new Vector3(0.54f, 0.02f, 0.06f)
                * (Age >= TelegraphTicks ? 0.9f : 0.35f));
            Projectile.localAI[0]++;
            if (Age >= TelegraphTicks + ActiveTicks + 12)
            {
                Projectile.Kill();
            }
        }

        public override bool? CanDamage()
        {
            return Age >= TelegraphTicks && Age < TelegraphTicks + ActiveTicks;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                Projectile.Center + direction * Length, 12f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = MathHelper.Clamp(Age / (float)TelegraphTicks, 0f, 1f);
            bool active = Age >= TelegraphTicks && Age < TelegraphTicks + ActiveTicks;
            float fade = Age < TelegraphTicks + ActiveTicks ? 1f
                : 1f - (Age - TelegraphTicks - ActiveTicks) / 12f;
            // identity is the per-instance noise phase, so twelve bolts around the arena (or four
            // across a Stormbreaker volley) do not all trace the identical path (vfx-shader-tips §33).
            RedKnightVFX.DrawLightningLane(Projectile.Center, Projectile.velocity, Length,
                progress, active, fade, phase: Projectile.identity * 0.37f);
            return false;
        }
    }

    public class CrimsonDominionController : ModProjectile
    {
        // The original single 600-tick timeline. Nothing runs it end-to-end any more (see the two
        // modes below), but the FINALE still replays its SealStart→TotalTicks tail verbatim, so
        // these remain the constants that place the seal, the nova and the fade on the clock.
        public const int BuildTicks = 45;
        public const int SweepTicks = 300;
        // +120t over the original 90t so the pre-nova charge-up gives a real warning window
        // instead of the detonation arriving almost as soon as it becomes visible.
        public const int EscapeTicks = 210;
        public const int NovaTicks = 10;
        public const int FadeTicks = 35;
        public const int EscapeStart = BuildTicks + SweepTicks;
        public const int NovaStart = EscapeStart + EscapeTicks;
        public const int FadeStart = NovaStart + NovaTicks;
        public const int TotalTicks = FadeStart + FadeTicks;
        public const float Radius = 420f;

        // The finishing seal FILLS over the last SealFillTicks of the escape window, then blasts
        // over NovaTicks + FadeTicks — about two seconds of readable "circle fills, circle explodes".
        // The escape window (EscapeTicks) is a balance number and is deliberately NOT shortened;
        // only the moment the seal becomes visible moves.
        public const int SealFillTicks = 90;
        public const int SealStart = NovaStart - SealFillTicks;

        // -----------------------------------------------------------------------------------
        // TWO MODES. Crimson Dominion is a permanent phase that ends only when the knight dies,
        // so this controller has exactly two jobs — the old "full 600-tick timeline" mode is gone
        // because nothing spawned it any more:
        //
        //   CONTAINMENT mode — spawned from RedKnightAttackController.TickCrimsonDominion on the
        //                  beat the spear plant lands (Timer 60). Builds the arena-wide "stay
        //                  inside the safe zone" field, then HOLDS it, damaging, INDEFINITELY:
        //                  through the rest of the 300t plant-and-hold AND through all of phase 2's
        //                  melee. There is no fixed duration and no escape countdown — the field is
        //                  simply the arena for the rest of the fight. It does NOT run the old
        //                  twelve-strike arena-edge barrage (superseded by the Dominion Stage A-D
        //                  lightning loop) and it does NOT run the Escape/Nova/Fade tail — that
        //                  escape-and-nova drama is reserved exclusively for the death finale.
        //                  It ends exactly one way: the death finale starting.
        //   FINALE mode  — spawned from GreatRedKnight.CheckDead. Skips straight to the seal
        //                  fill, so the sequence the player sees is exactly "circle fills, circle
        //                  explodes" with no ring and no wall.
        //
        // The mode rides on the MAGNITUDE of ai[0]: |ai[0]| == 2 means finale, anything else means
        // containment, and RotationDirection only ever reads the SIGN — so all three ai slots keep
        // their existing meanings and the flag syncs for free (netImportant).
        //
        // HANDOFF. Containment polls its host NPC (ai[2] = whoAmI) for
        // GreatRedKnight.InDominionDeathSequence and begins its fade the instant that latches —
        // i.e. on the tick CheckDead pins the knight at 1 HP, a full DominionDeathReplantTicks (60)
        // BEFORE the finale controller spawns. Polling rather than having CheckDead hunt for the
        // projectile slot is what makes it correct in multiplayer for free: dominionDeathTimer is
        // already synced through Send/ReceiveExtraAI, so every client independently reaches the
        // same conclusion on the same tick with no new packet and no owner bookkeeping. The 60t
        // lead means the wall is fully down before the seal starts filling — no double field, and
        // no lingering damage source once the death sequence owns the arena.
        // -----------------------------------------------------------------------------------
        public const int FinaleTotalTicks = SealFillTicks + NovaTicks + FadeTicks;

        /// <summary>Dominion attack Timer on which phase 1 spawns this controller (the plant beat).</summary>
        public const int Phase1SpawnBeat = 60;

        /// <summary>Ticks the containment field takes to fade once the death finale latches. Must
        /// stay comfortably under GreatRedKnight.DominionDeathReplantTicks (60) so the wall is gone
        /// before the finale controller spawns its seal.</summary>
        public const int ContainmentFadeTicks = 45;

        public override string Texture => "Terraria/Images/MagicPixel";

        bool FinaleMode => Math.Abs(Projectile.ai[0]) >= 1.5f;

        /// <summary>Finale mode replays the NORMAL-mode timeline window SealStart→TotalTicks, so
        /// its age is offset forward to land on SealStart and every phase comparison below works
        /// unmodified. Containment mode has no fixed duration, so it counts on its own clock.</summary>
        int Age => FinaleMode
            ? SealStart + (FinaleTotalTicks - Projectile.timeLeft)
            : (int)Projectile.localAI[0];

        /// <summary>Containment mode only: ticks spent fading out, 0 while the field is live.</summary>
        int FadeProgress => (int)Projectile.localAI[1];

        int RotationDirection => Projectile.ai[0] < 0f ? -1 : 1;
        float BaseRotation => Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            // Containment refreshes this every tick (see TickContainment); the value here only has
            // to be large enough that it never expires between updates.
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        // ai[] is not populated during SetDefaults, so finale mode's lifetime has to be applied here.
        public override void OnSpawn(IEntitySource source)
        {
            if (FinaleMode)
            {
                Projectile.timeLeft = FinaleTotalTicks;
            }
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!FinaleMode)
            {
                TickContainment();
                return;
            }

            float intensity = Age < NovaStart ? 0.46f
                : Age < FadeStart ? 1f : 0.28f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.62f, 0.025f, 0.045f) * intensity);

            if (Age == NovaStart)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.38f }, Projectile.Center);
                if (Main.netMode != NetmodeID.Server)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 direction = Main.rand.NextVector2Unit();
                        Dust dust = Dust.NewDustPerfect(Projectile.Center
                            + direction * Main.rand.NextFloat(48f, Radius),
                            Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.RedTorch,
                            direction * Main.rand.NextFloat(1.4f, 3.8f), 90,
                            new Color(210, 20, 50), Main.rand.NextFloat(0.8f, 1.25f));
                        dust.noGravity = true;
                    }
                }
            }
        }

        /// <summary>
        /// The indefinite containment state. There is no timeline here on purpose: after the
        /// BuildTicks ramp the field just HOLDS — the shader's own animation is driven by
        /// Main.GlobalTimeWrappedHourly inside DrawDominionQuad, so its noise cycles forever with
        /// no age input needed, and holding a constant opacity is the whole of "ongoing".
        /// The only thing that advances is the fade, and only once the death finale latches.
        /// </summary>
        void TickContainment()
        {
            // No fixed duration — top the lifetime up every tick and keep the age on the
            // projectile's own counter so it can run for the rest of the fight.
            Projectile.timeLeft = 600;
            Projectile.localAI[0]++;

            if (FadeProgress > 0)
            {
                Projectile.localAI[1]++;
                if (FadeProgress > ContainmentFadeTicks)
                {
                    Projectile.Kill();
                    return;
                }
            }
            else if (ShouldYieldToFinale())
            {
                Projectile.localAI[1] = 1f;
                // The wall coming down is its own audible beat: it tells the player the arena
                // constraint is lifting because something worse is starting.
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.15f }, Projectile.Center);
            }

            float build = Age < BuildTicks ? Age / (float)BuildTicks : 1f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.62f, 0.025f, 0.045f)
                * 0.72f * build * (1f - ContainmentFade));
        }

        /// <summary>0 while the containment field is fully up, 1 once it has finished fading.</summary>
        float ContainmentFade => FadeProgress <= 0 ? 0f
            : MathHelper.Clamp(FadeProgress / (float)ContainmentFadeTicks, 0f, 1f);

        /// <summary>
        /// True the moment Great Red Knight's death finale takes the arena over — or if the host
        /// NPC has gone away entirely, which must never leave an orphaned arena-wide hazard behind.
        /// Polled rather than pushed; see the handoff note above.
        /// </summary>
        bool ShouldYieldToFinale()
        {
            int host = (int)Projectile.ai[2];
            if (host < 0 || host >= Main.maxNPCs)
            {
                return true;
            }
            NPC hostNPC = Main.npc[host];
            if (!hostNPC.active)
            {
                return true;
            }
            return hostNPC.ModNPC is NPCs.Bosses.SuperHardMode.GreatRedKnight knight
                && knight.InDominionDeathSequence;
        }

        /// <summary>True while the containment wall is a live hazard: once it has built in, until
        /// the death finale triggers its fade.</summary>
        bool ContainmentActive => !FinaleMode && Age >= BuildTicks && FadeProgress <= 0;

        public override bool? CanDamage()
        {
            // The finale nova DAMAGES, exactly as the mid-fight one did — reviewed and confirmed
            // as intended. It is Great Red Knight's genuine last attack, not a cosmetic parting
            // shot: the 90-tick seal fill is the telegraph, and getting clear of a 420px radius in
            // ~1.5s is meant to be tight. A player who reads it lives; one who stands in it does
            // not.
            bool novaActive = FinaleMode && Age >= NovaStart && Age < FadeStart;
            return ContainmentActive || novaActive;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (ContainmentActive)
            {
                // The wall itself hurts if a player presses against it; the actual offense inside
                // the ring comes entirely from the Dominion lightning loop (Stages A-D, driven by
                // RedKnightAttackController.TickDominionSequence) rather than from a hazard owned
                // by this controller — which is why the old twelve arena-edge strikes are gone.
                return FarthestCornerDistance(targetHitbox, Projectile.Center) >= Radius;
            }

            if (FinaleMode && Age >= NovaStart && Age < FadeStart)
            {
                return ClosestPointDistance(targetHitbox, Projectile.Center) <= Radius;
            }
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            RedKnightVFX.DrawCrimsonDominion(Projectile.Center, Age, BaseRotation, RotationDirection,
                FinaleMode, ContainmentFade);
            return false;
        }

        static float ClosestPointDistance(Rectangle rectangle, Vector2 center)
        {
            float closestX = MathHelper.Clamp(center.X, rectangle.Left, rectangle.Right);
            float closestY = MathHelper.Clamp(center.Y, rectangle.Top, rectangle.Bottom);
            return Vector2.Distance(center, new Vector2(closestX, closestY));
        }

        static float FarthestCornerDistance(Rectangle rectangle, Vector2 center)
        {
            float maximumDistance = 0f;
            maximumDistance = Math.Max(maximumDistance, Vector2.Distance(center, rectangle.TopLeft()));
            maximumDistance = Math.Max(maximumDistance, Vector2.Distance(center, rectangle.TopRight()));
            maximumDistance = Math.Max(maximumDistance, Vector2.Distance(center, rectangle.BottomLeft()));
            maximumDistance = Math.Max(maximumDistance, Vector2.Distance(center, rectangle.BottomRight()));
            return maximumDistance;
        }
    }

    public class RedKnightVFXBurst : ModProjectile
    {
        const int DefaultLifetime = 24;
        // Bomb detonations run 20t longer than the other bursts — a 0.4s explosion was over before
        // the eye had resolved it. Impact bursts keep the original snappy 24t.
        const int BombLifetime = DefaultLifetime + 20;

        RedKnightBurstKind Kind => (RedKnightBurstKind)(int)Projectile.ai[0];

        int Lifetime => Kind == RedKnightBurstKind.BombExplosion
                || Kind == RedKnightBurstKind.BombExplosionLayered
            ? BombLifetime
            : DefaultLifetime;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.timeLeft = DefaultLifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        // ai[] is not populated during SetDefaults, so the kind-dependent lifetime has to be applied
        // here instead.
        public override void OnSpawn(IEntitySource source)
        {
            Projectile.timeLeft = Lifetime;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            RedKnightVFX.DrawBurst(Kind, Projectile.Center,
                progress, Projectile.ai[1] <= 0f ? 1f : Projectile.ai[1]);
            return false;
        }
    }

    /// <summary>
    /// Fixed cast-time anchor for Red Knight's ten-second outer-lane poison rain. The controller
    /// remains at the knight's release position, so later movement cannot drag the safe central
    /// space or the rain bands around. Only the server samples lanes and creates damaging orbs.
    /// </summary>
    public class RedKnightPoisonRainController : ModProjectile
    {
        public const int RainDurationTicks = 10 * 60;
        const float InnerLaneOffset = 400f;
        const float OuterLaneOffset = 650f;
        const float DesiredSpawnHeight = 340f;
        const float MinimumSpawnHeight = 112f;
        const int MinimumVolleyDelay = 18;
        const int MaximumVolleyDelay = 31;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.timeLeft = RainDurationTicks;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Projectile.localAI[0]--;
            if (Projectile.localAI[0] > 0f)
            {
                return;
            }

            Projectile.localAI[0] = Main.rand.Next(MinimumVolleyDelay, MaximumVolleyDelay + 1);
            int ballCount = Main.rand.NextFloat() < 0.38f ? 2 : 1;
            int firstSide = Main.rand.NextBool() ? 1 : -1;
            for (int i = 0; i < ballCount; i++)
            {
                // Two-ball beats use opposite outer lanes, keeping the center readable without
                // making every beat perfectly mirrored or mechanically dense.
                int preferredSide = i == 0 ? firstSide : -firstSide;
                if (!TryChooseRainOrigin(Projectile.Center, preferredSide, out Vector2 origin))
                {
                    continue;
                }

                Vector2 velocity = new Vector2(0f, Main.rand.NextFloat(2.2f, 3.15f));
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, velocity,
                    ModContent.ProjectileType<EnemySpellAbyssPoisonStrikeBall>(), Projectile.damage,
                    0f, Main.myPlayer, ai2: 2f);
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public static bool HasUsableLane(Vector2 anchor)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                for (float offset = InnerLaneOffset; offset <= OuterLaneOffset; offset += 62.5f)
                {
                    if (TryFindRainOrigin(anchor, anchor.X + side * offset, out _))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static bool TryChooseRainOrigin(Vector2 anchor, int preferredSide, out Vector2 origin)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                int side = attempt < 3 ? preferredSide : -preferredSide;
                float offset = Main.rand.NextFloat(InnerLaneOffset, OuterLaneOffset);
                if (TryFindRainOrigin(anchor, anchor.X + side * offset, out origin))
                {
                    return true;
                }
            }
            origin = Vector2.Zero;
            return false;
        }

        static bool TryFindRainOrigin(Vector2 anchor, float x, out Vector2 origin)
        {
            // Start at the desired height, then walk downward. Requiring an unobstructed vertical
            // line to the knight's elevation rejects sky above a roof and chooses open space below
            // that roof instead; the 16x40 body check prevents materializing inside thin ceilings.
            int steps = (int)((DesiredSpawnHeight - MinimumSpawnHeight) / 16f);
            for (int step = 0; step <= steps; step++)
            {
                Vector2 candidate = new Vector2(x, anchor.Y - DesiredSpawnHeight + step * 16f);
                Vector2 boxTopLeft = candidate - new Vector2(8f, 8f);
                Vector2 laneEnd = new Vector2(x, anchor.Y);
                if (!Collision.SolidCollision(boxTopLeft, 16, 40)
                    && Collision.CanHitLine(candidate, 2, 2, laneEnd, 2, 2))
                {
                    origin = candidate;
                    return true;
                }
            }
            origin = Vector2.Zero;
            return false;
        }
    }

    /// <summary>
    /// Ten equally spaced poison drops sweep across a player-centered 600px curtain. The player
    /// position and sweep direction are fixed at release; individual origins hug the underside of
    /// valid ceilings where possible and never spawn with less than twelve tiles of headroom.
    /// </summary>
    public class RedKnightPoisonCurtainController : ModProjectile
    {
        public const int BallCount = 10;
        public const int MaximumObstructedLanes = 4;
        public const int BallIntervalTicks = 20;
        public const float CurtainWidth = 600f;
        public const float MinimumHeightAbovePlayer = 12f * 16f;
        const float OpenSkySpawnHeight = 320f;
        const int CeilingSearchTiles = 40;
        const int Lifetime = (BallCount - 1) * BallIntervalTicks + 2;

        int Age => Lifetime - Projectile.timeLeft;
        int SweepDirection => Projectile.ai[0] >= 0f ? 1 : -1;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient
                || Age < 0 || Age > (BallCount - 1) * BallIntervalTicks
                || Age % BallIntervalTicks != 0)
            {
                return;
            }

            int index = Age / BallIntervalTicks;
            float spacing = CurtainWidth / (BallCount - 1);
            float leftToRightOffset = -CurtainWidth * 0.5f + index * spacing;
            float x = Projectile.Center.X + SweepDirection * leftToRightOffset;
            if (!TryFindCurtainOrigin(Projectile.Center, x, out Vector2 origin))
            {
                return;
            }

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin,
                new Vector2(0f, 2.7f),
                ModContent.ProjectileType<EnemySpellAbyssPoisonStrikeBall>(), Projectile.damage,
                0f, Main.myPlayer, ai2: 2f);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        public static bool HasUsableCurtain(Vector2 playerAnchor)
        {
            float spacing = CurtainWidth / (BallCount - 1);
            int usableLanes = 0;
            for (int i = 0; i < BallCount; i++)
            {
                float x = playerAnchor.X - CurtainWidth * 0.5f + i * spacing;
                if (TryFindCurtainOrigin(playerAnchor, x, out _))
                {
                    usableLanes++;
                }
            }

            // The curtain may contain natural gaps, but it is not worth committing the bag card
            // once half or more of its ten scheduled beats would be swallowed by terrain.
            return usableLanes >= BallCount - MaximumObstructedLanes;
        }

        static bool TryFindCurtainOrigin(Vector2 playerAnchor, float x, out Vector2 origin)
        {
            float highestAllowedY = playerAnchor.Y - MinimumHeightAbovePlayer;
            int tileX = Utils.Clamp((int)(x / 16f), 2, Main.maxTilesX - 3);
            int startTileY = Utils.Clamp((int)(highestAllowedY / 16f),
                CeilingSearchTiles + 2, Main.maxTilesY - 4);
            bool foundCeiling = false;

            // Search upward from the twelve-tile boundary. The first solid tile is the nearest
            // eligible ceiling; its underside becomes the preferred spawn point.
            for (int tileY = startTileY; tileY >= startTileY - CeilingSearchTiles; tileY--)
            {
                Tile tile = Framing.GetTileSafely(tileX, tileY);
                bool solidCeiling = tile.HasTile && !tile.IsActuated
                    && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
                if (!solidCeiling)
                {
                    continue;
                }

                foundCeiling = true;
                Vector2 candidate = new Vector2(x, (tileY + 1) * 16f + 10f);
                if (candidate.Y <= highestAllowedY && IsClearCurtainLane(candidate, playerAnchor))
                {
                    origin = candidate;
                    return true;
                }

                // A nearer obstruction already blocks the vertical lane. A higher ceiling cannot
                // make that lane usable, so fail rather than spawning above solid terrain.
                break;
            }

            if (!foundCeiling)
            {
                Vector2 openSkyCandidate = new Vector2(x, playerAnchor.Y - OpenSkySpawnHeight);
                if (IsClearCurtainLane(openSkyCandidate, playerAnchor))
                {
                    origin = openSkyCandidate;
                    return true;
                }
            }

            origin = Vector2.Zero;
            return false;
        }

        static bool IsClearCurtainLane(Vector2 candidate, Vector2 playerAnchor)
        {
            if (candidate.Y > playerAnchor.Y - MinimumHeightAbovePlayer
                || Collision.SolidCollision(candidate - new Vector2(8f), 16, 24))
            {
                return false;
            }

            Vector2 laneEnd = new Vector2(candidate.X, playerAnchor.Y);
            return Collision.CanHitLine(candidate, 2, 2, laneEnd, 2, 2);
        }
    }

    /// <summary>
    /// One false Crimson Teleport destination. The inward fire portal warns for exactly 45 ticks,
    /// then the same cached center becomes an 84px circular blast for six ticks. The bright active
    /// body is scaled from that radius, so presentation and collision describe the same space.
    /// </summary>
    public class RedKnightTeleportFeintBlast : ModProjectile
    {
        public const float Radius = 84f;
        const int TelegraphTicks = 45;
        const int ActiveTicks = 6;
        const int ResidueTicks = 28;
        const int Lifetime = TelegraphTicks + ActiveTicks + ResidueTicks;

        int Age => Lifetime - Projectile.timeLeft;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage()
            => Age >= TelegraphTicks && Age < TelegraphTicks + ActiveTicks;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float closestX = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
            float closestY = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
            return Vector2.DistanceSquared(Projectile.Center, new Vector2(closestX, closestY))
                <= Radius * Radius;
        }

        public override void AI()
        {
            if (Age < TelegraphTicks)
            {
                float progress = Age / (float)TelegraphTicks;
                Lighting.AddLight(Projectile.Center,
                    new Vector3(0.35f, 0.035f, 0.015f) * (0.35f + progress * 0.65f));
                if (!Main.dedServ)
                {
                    int count = Age > TelegraphTicks - 12 ? 3 : 2;
                    for (int i = 0; i < count; i++)
                    {
                        Vector2 outward = Main.rand.NextVector2Unit();
                        Vector2 position = Projectile.Center + outward * Radius;
                        Dust ember = Dust.NewDustPerfect(position,
                            Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.RedTorch,
                            -outward * Main.rand.NextFloat(1.2f, 2.8f), 105,
                            new Color(214, 28, 30), Main.rand.NextFloat(0.65f, 1f));
                        ember.noGravity = true;
                    }
                }
            }
            else
            {
                Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 0.16f, 0.04f));
                if (Age == TelegraphTicks)
                {
                    SoundEngine.PlaySound(SoundID.Item74 with
                    {
                        Volume = 0.78f,
                        Pitch = -0.25f,
                        PitchVariance = 0.08f
                    }, Projectile.Center);
                    if (!Main.dedServ)
                    {
                        SpawnBombBreakup();
                        for (int i = 0; i < 28; i++)
                        {
                            Vector2 outward = (MathHelper.TwoPi * i / 28f).ToRotationVector2();
                            Dust flame = Dust.NewDustPerfect(
                                Projectile.Center + outward * Main.rand.NextFloat(10f, 38f),
                                i % 4 == 0 ? DustID.Shadowflame : DustID.RedTorch,
                                outward * Main.rand.NextFloat(3.2f, 7.2f), 80,
                                new Color(238, 45, 24), Main.rand.NextFloat(0.8f, 1.25f));
                            flame.noGravity = true;
                        }
                    }
                }
            }
        }

        void SpawnBombBreakup()
        {
            // Match EnemyFirebomb's three different particle jobs, trimmed modestly because this
            // branch always detonates twice only 45 ticks apart. Count provides density; all scales
            // stay below the blocky >2 range even after Terraria's built-in scale jitter.

            // MIDGROUND: red flame body with a quarter dark soot.
            for (int i = 0; i < 60; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                bool soot = i % 4 == 0;
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(4f, 68f),
                    soot ? DustID.Shadowflame : DustID.RedTorch,
                    direction * Main.rand.NextFloat(1.6f, 6.8f),
                    soot ? 150 : 90,
                    soot ? new Color(16, 3, 8) : new Color(214, 22, 42),
                    Main.rand.NextFloat(0.75f, 1.45f));
                ember.noGravity = true;
                if (soot)
                {
                    ember.noLight = true;
                }
            }

            // FOREGROUND: small fast sparks form the sharp leading edge.
            for (int i = 0; i < 26; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(6f, 26f),
                    DustID.Torch, direction * Main.rand.NextFloat(7f, 12.5f),
                    60, new Color(255, 176, 96), Main.rand.NextFloat(0.45f, 0.85f));
                spark.noGravity = true;
            }

            // BACKGROUND: slow smoke billows after both the hit window and shader core fade.
            for (int i = 0; i < 14; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                Dust smoke = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(10f, 46f),
                    DustID.Smoke,
                    direction * Main.rand.NextFloat(0.6f, 2.2f) - Vector2.UnitY * 0.7f,
                    170, new Color(38, 30, 32), Main.rand.NextFloat(0.5f, 0.8f));
                smoke.fadeIn = Main.rand.NextFloat(1.3f, 1.7f);
                smoke.noLight = true;
            }

            // Four vanilla smoke-gore puffs, using the same 61-63 family as EnemyFirebomb.
            for (int i = 0; i < 4; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / 4f + MathHelper.PiOver4)
                    .ToRotationVector2();
                int goreIndex = Gore.NewGore(Projectile.GetSource_FromThis(),
                    Projectile.Center - new Vector2(24f),
                    direction * Main.rand.NextFloat(1.1f, 2.3f) + Vector2.UnitY * 0.7f,
                    Main.rand.Next(61, 64), Main.rand.NextFloat(0.78f, 0.94f));
                Main.gore[goreIndex].scale = Main.rand.NextFloat(0.9f, 1.08f);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire3, 3 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Age < TelegraphTicks)
            {
                RedKnightVFX.DrawTeleportFeintTell(Projectile.Center, Radius,
                    Age / (float)TelegraphTicks);
            }
            else
            {
                float progress = (Age - TelegraphTicks) / (float)(ActiveTicks + ResidueTicks);
                float scale = Radius * 2f / 132f;
                RedKnightVFX.DrawBurst(RedKnightBurstKind.BombExplosion,
                    Projectile.Center, MathHelper.Clamp(progress, 0f, 1f), scale);
            }
            return false;
        }
    }
}
