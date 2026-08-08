using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
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

    public class RedKnightStandard : ModProjectile
    {
        Vector2 startPosition;
        bool initialized;

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/BlackKnightSpear";

        KnightStandardMode Mode => (KnightStandardMode)(int)Projectile.ai[2];
        int FlightTicks => Mode == KnightStandardMode.RedKnight ? 24 : 30;
        // The Great modes are now thrown BACK TO BACK (see RedKnightAttackController.TickRoyalStandard)
        // rather than all three at once, so each spear only gets a SHORT telegraph before its own
        // shockwave rather than a long shared charge — otherwise the sequence reintroduces exactly
        // the dead air the sequencing was meant to remove. RedKnight's single spear is unchanged.
        int ChargeTicks => Mode == KnightStandardMode.RedKnight ? 60 : 26;
        Vector2 GroundPoint => new Vector2(Projectile.ai[0], Projectile.ai[1]);
        // +5px versus the first pass: the spear was reading as hovering just above the floor
        // instead of driven into it.
        Vector2 PlantedCenter => GroundPoint - new Vector2(0f, 10f);
        /// <summary>Where the flame's ground band sits — the same +5px down as the spear.</summary>
        Vector2 FlameAnchor => GroundPoint + new Vector2(0f, 5f);

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 62;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
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
            return physicalSpear && age < FlightTicks;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = (PlantedCenter - startPosition).SafeNormalize(Vector2.UnitY);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - direction * 24f, Projectile.Center + direction * 24f,
                7f, ref collisionPoint);
        }

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
                Projectile.Center = Vector2.Lerp(startPosition, PlantedCenter, progress);
                Vector2 direction = PlantedCenter - startPosition;
                Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2;
                return;
            }

            Projectile.Center = PlantedCenter;
            Projectile.rotation = MathHelper.Pi;
            int plantedAge = age - FlightTicks;
            if (plantedAge == 0)
            {
                PlaySound(SoundID.Dig with { Volume = 0.65f, Pitch = -0.15f }, GroundPoint);
                float impactScale = Mode == KnightStandardMode.RedKnight ? 0.75f : 1f;
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
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
                Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() * 0.5f,
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

            // 1-in-9 at the start rising to 1-in-3 at full extent. Deliberately not denser than
            // that: Royal Standard now fires up to six of these waves, and at a 46t blaze lifetime
            // a 1-in-2 rate put ~140 decorative projectiles on screen at once.
            int chance = Math.Max(3, (int)MathHelper.Lerp(9f, 3f, travelProgress));
            if (!Main.rand.NextBool(chance))
            {
                return;
            }

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
            PlaySound(SoundID.Item74 with { Volume = 1f, Pitch = -0.25f }, Projectile.Center);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<RedKnightVFXBurst>(), 0, 0f, Main.myPlayer,
                    (float)RedKnightBurstKind.BombExplosion, 1f);
            }
            if (!Main.dedServ)
            {
                for (int i = 0; i < 28; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(5.5f, 5.5f);
                    Dust ember = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, velocity, 80,
                        new Color(255, 65, 25), Main.rand.NextFloat(1f, 1.8f));
                    ember.noGravity = true;
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
        int ActiveTicks => Math.Max(1, (int)Projectile.ai[1]);
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
            if (Age == TelegraphTicks && !Main.dedServ)
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
}
