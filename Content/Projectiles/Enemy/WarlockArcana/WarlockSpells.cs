using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WarlockNPC = tsorcRevamp.NPCs.Enemies.Warlock;

namespace tsorcRevamp.Content.Projectiles.Enemy.WarlockArcana
{
    // Every spell of the Warlock (NPCs/Enemies/Warlock.cs) lives in this file: one system, one file. They all draw through
    // Effects/WarlockArcana.fx (WarlockArcanaDraw below) and dust; none has a sprite, so they all borrow the transparent
    // InvisibleNothingProj texture on purpose.
    //
    // Ownership: ai[0] is the owner key = the Warlock's SpellSerial * 256 + its whoAmI (WarlockNPC.OwnerKey). The slot
    // alone finds the Warlock; the serial says whether it is still the same cast. Both are synced, so every machine agrees.
    //
    // Telegraph rule (user, 2026-11-17): no line or dotted-line telegraphs anywhere. Tells are the staff pose, dust, and
    // shader-drawn magic: a charging gem, prisms that turn to face their target, a crystal growing in the wall, a vortex.

    /// <summary>Palette, dust and the WarlockArcana.fx quad draw, shared by the Warlock and all of its spells.</summary>
    internal static class WarlockArcanaDraw
    {
        public static readonly Color Mid = new Color(115, 110, 245);
        public static readonly Color Accent = new Color(200, 160, 255);
        public static readonly Color Core = new Color(220, 235, 255);
        public static readonly Color Deep = new Color(65, 35, 125);

        private const float PixelBlockSize = 2f;

        private static Asset<Effect> _effect;
        private static Asset<Texture2D> _cloudNoise;
        private static Asset<Texture2D> _detailNoise;

        private static bool LoadAssets()
        {
            if (Main.dedServ)
            {
                return false;
            }

            _effect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/WarlockArcana", AssetRequestMode.ImmediateLoad);
            _cloudNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/T_CloudNoise_Tiled", AssetRequestMode.ImmediateLoad);
            _detailNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            return true;
        }

        // A procedural quad of `size` world pixels centred on `worldCenter`, rotated by `rotation` (0 = +x along the quad).
        // Premultiplied AlphaBlend (see the .fx header); restores texture slot 1 and the spritebatch afterwards.
        public static void Quad(string technique, Vector2 worldCenter, Vector2 size, float rotation, float opacity, float progress,
            float active, Vector4 extra)
        {
            if (opacity <= 0f || !LoadAssets())
            {
                return;
            }

            Texture2D primary = _cloudNoise.Value;
            Vector2 scale = size / primary.Size();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap, DepthStencilState.None,
                RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousTexture = device.Textures[1];
            SamplerState previousSampler = device.SamplerStates[1];

            try
            {
                device.Textures[1] = _detailNoise.Value;
                device.SamplerStates[1] = SamplerState.LinearWrap;

                Effect effect = _effect.Value;
                effect.CurrentTechnique = effect.Techniques[technique];
                effect.Parameters["MidColor"]?.SetValue(Mid.ToVector3());
                effect.Parameters["AccentColor"]?.SetValue(Accent.ToVector3());
                effect.Parameters["CoreColor"]?.SetValue(Core.ToVector3());
                effect.Parameters["Opacity"]?.SetValue(MathHelper.Clamp(opacity, 0f, 1f));
                effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"]?.SetValue(MathHelper.Clamp(progress, 0f, 1f));
                effect.Parameters["Active"]?.SetValue(MathHelper.Clamp(active, 0f, 1f));
                effect.Parameters["Extra"]?.SetValue(extra);

                // Pre-divided pixel grid (block count across the quad and its reciprocal): ps_2_0 has no preshader.
                Vector2 pixelBlocks = Vector2.Max(size / PixelBlockSize, Vector2.One);
                effect.Parameters["PixelGrid"]?.SetValue(new Vector4(pixelBlocks.X, pixelBlocks.Y, 1f / pixelBlocks.X, 1f / pixelBlocks.Y));
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primary, worldCenter - Main.screenPosition, null, Color.White, rotation, primary.Size() * 0.5f,
                    scale, SpriteEffects.None, 0f);
            }
            finally
            {
                device.Textures[1] = previousTexture;
                device.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        // Cos/sin of a spinning angle, for techniques that turn their noise frame (the shader has no per-pixel sincos budget).
        private static Vector4 Spin(float radiansPerSecond, float phase)
        {
            float angle = Main.GlobalTimeWrappedHourly * radiansPerSecond + phase;
            return new Vector4((float)Math.Cos(angle), (float)Math.Sin(angle), 0f, 0f);
        }

        // Energy sphere. `charge` 0..1 heats the heart and flares the prism rim.
        public static void Orb(Vector2 center, float diameter, float opacity, float charge, float phase = 0f)
        {
            Quad("WarlockOrb", center, new Vector2(diameter), 0f, opacity, charge, 1f, Spin(1.8f, phase));
        }

        // Beam from start to end. `thickness` is the QUAD height (the shader keeps the hot core in its middle ~20%);
        // `heat` 1 = live, 0 = afterglow.
        public static void Beam(Vector2 start, Vector2 end, float thickness, float opacity, float heat)
        {
            Vector2 span = end - start;
            float length = span.Length();
            if (length < 2f)
            {
                return;
            }

            Vector2 center = (start + end) * 0.5f;
            float rotation = span.ToRotation();
            Vector4 extra = new Vector4(length / thickness, 0f, 0f, 0f);
            Quad("WarlockBeam", center, new Vector2(length, thickness), rotation, opacity, heat, 1f, extra);
        }

        // Crystal prism, firing end along `rotation`. Quad is 2:1. `flare` 0..1 = about to fire.
        public static void Prism(Vector2 center, float length, float rotation, float opacity, float flare, int seed)
        {
            // Each prism's light band sweeps along it on its own phase, so a group of prisms never pulses in unison.
            float sweepPhase = (Main.GlobalTimeWrappedHourly * 0.6f + seed * 0.37f) % 1f;
            Vector4 extra = new Vector4(sweepPhase, 0f, 0f, 0f);
            Quad("WarlockPrism", center, new Vector2(length, length * 0.5f), rotation, opacity, flare, 1f, extra);
        }

        // Collapse vortex. `collapse` 0..1 contracts the disc toward the black heart.
        public static void Vortex(Vector2 center, float diameter, float opacity, float collapse)
        {
            Quad("WarlockVortex", center, new Vector2(diameter), 0f, opacity, collapse, 1f, Vector4.Zero);
        }

        // Ward ring lying on the floor at `floorCenter`. The quad is 1.3x the ring diameter wide and 160px tall, with the
        // floor line 78% of the way down. `formation` 0..1 grows the ring outward; `bastion` 0..1 raises the light curtain.
        public static void Ward(Vector2 floorCenter, float radius, float formation, float bastion, float opacity)
        {
            const float QuadHeight = 160f;
            const float FloorV = 0.78f;
            Vector2 size = new Vector2(radius * 2f * 1.3f, QuadHeight);
            Vector2 center = floorCenter - new Vector2(0f, QuadHeight * (FloorV - 0.5f));

            float grow = Math.Max(formation, 0.05f);
            float time = Main.GlobalTimeWrappedHourly;
            Vector4 extra = new Vector4(2.6f / grow, 8.33f / grow, (time * 0.6f) % 1f, (time * 0.03f) % 1f);
            Quad("WarlockWard", center, size, 0f, opacity, 0f, bastion, extra);
        }

        // Shock ring + flash. `age` 0..1.
        public static void Burst(Vector2 center, float diameter, float age, float opacity)
        {
            Quad("WarlockBurst", center, new Vector2(diameter), 0f, opacity, age, 1f, Vector4.Zero);
        }

        // One glowing mote in the Warlock's palette.
        public static Dust Mote(Vector2 position, Vector2 velocity, Color color, float scale)
        {
            Dust mote = Dust.NewDustPerfect(position, DustID.TintableDustLighted, velocity, 60, color, scale);
            mote.noGravity = true;
            return mote;
        }

        // A burst of motes flying outward from `center`.
        public static void MoteBurst(Vector2 center, int count, float speed, float scale)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(speed, speed);
                Color color = Mid;
                if (i % 3 == 0)
                {
                    color = Accent;
                }
                else if (i % 5 == 0)
                {
                    color = Core;
                }

                Mote(center, velocity, color, scale * Main.rand.NextFloat(0.8f, 1.25f));
            }

            Lighting.AddLight(center, Mid.ToVector3() * 0.8f);
        }
    }

    /// <summary>Owner lookup and ray maths shared by the Warlock and its spells.</summary>
    internal static class WarlockGeometry
    {
        // Owner key -> the Warlock in that slot, or null. Does not check the cast serial.
        public static WarlockNPC FindOwner(float ownerKey)
        {
            int index = (int)ownerKey & 255;
            if (index < 0 || index >= Main.maxNPCs)
            {
                return null;
            }

            NPC npc = Main.npc[index];
            if (!npc.active || npc.ModNPC is not WarlockNPC warlock)
            {
                return null;
            }

            return warlock;
        }

        // The owner, only while it is still running the cast that spawned this projectile.
        public static WarlockNPC FindSameCast(float ownerKey, WarlockNPC.Spell spell)
        {
            WarlockNPC warlock = FindOwner(ownerKey);
            if (warlock == null)
            {
                return null;
            }

            int serial = (int)ownerKey >> 8;
            bool sameCast = warlock.SpellSerial == serial && warlock.CurrentSpell == spell;
            if (!sameCast)
            {
                return null;
            }

            return warlock;
        }

        public static bool IsSolid(Vector2 worldPoint)
        {
            int tileX = (int)(worldPoint.X / 16f);
            int tileY = (int)(worldPoint.Y / 16f);
            if (!WorldGen.InWorld(tileX, tileY, 2))
            {
                return true;
            }

            Tile tile = Framing.GetTileSafely(tileX, tileY);
            return tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType];
        }

        // Walks a ray in 4px steps until it enters a solid tile or reaches maxLength. Returns the last open point.
        public static Vector2 CastToWall(Vector2 start, Vector2 direction, float maxLength, out bool hitWall)
        {
            const float Step = 4f;
            Vector2 unit = direction.SafeNormalize(Vector2.UnitX);
            Vector2 point = start;
            hitWall = false;

            for (float travelled = 0f; travelled < maxLength; travelled += Step)
            {
                Vector2 next = point + unit * Step;
                if (IsSolid(next))
                {
                    hitWall = true;
                    return point;
                }

                point = next;
            }

            return point;
        }

        // Mirror `direction` off the tile face it hit just past `wallPoint`. The face is whichever neighbour step is solid:
        // only the x step -> a vertical wall (flip x); only the y step -> a floor or ceiling (flip y); both or neither
        // (a corner) -> straight back.
        public static Vector2 Reflect(Vector2 wallPoint, Vector2 direction)
        {
            Vector2 unit = direction.SafeNormalize(Vector2.UnitX);
            Vector2 stepX = new Vector2(Math.Sign(unit.X) * 6f, 0f);
            Vector2 stepY = new Vector2(0f, Math.Sign(unit.Y) * 6f);
            bool solidX = unit.X != 0f && IsSolid(wallPoint + stepX);
            bool solidY = unit.Y != 0f && IsSolid(wallPoint + stepY);

            if (solidX && !solidY)
            {
                return new Vector2(-unit.X, unit.Y);
            }

            if (solidY && !solidX)
            {
                return new Vector2(unit.X, -unit.Y);
            }

            return -unit;
        }

        // First solid floor at or below `worldPoint`, searching at most maxTiles down. Returns the floor's top edge, or -1.
        public static float FindFloorY(Vector2 worldPoint, int maxTiles)
        {
            int tileX = (int)(worldPoint.X / 16f);
            int startY = (int)(worldPoint.Y / 16f);
            for (int tileY = startY; tileY <= startY + maxTiles; tileY++)
            {
                if (!WorldGen.InWorld(tileX, tileY, 2))
                {
                    return -1f;
                }

                Tile tile = Framing.GetTileSafely(tileX, tileY);
                bool standable = tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                if (standable)
                {
                    return tileY * 16f;
                }
            }

            return -1f;
        }
    }

    /// <summary>
    /// Energy Bolt: one blue-violet bolt released from the staff gem at 8 px/t, straight, no gravity or homing, 100t life.
    /// Every ending (tile, player, timeout) flashes and leaves the legacy splash: a WarlockBurst, spawned on the server.
    /// ai[0] = owner key.
    /// </summary>
    public class WarlockEnergyBolt : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 100;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.rotation += 0.12f;
            if (Main.dedServ)
            {
                return;
            }

            if (Projectile.timeLeft % 3 == 0)
            {
                Vector2 behind = Projectile.Center - Projectile.velocity * 0.5f + Main.rand.NextVector2Circular(4f, 4f);
                WarlockArcanaDraw.Mote(behind, -Projectile.velocity * 0.05f, WarlockArcanaDraw.Accent, 0.9f);
            }

            Lighting.AddLight(Projectile.Center, WarlockArcanaDraw.Mid.ToVector3() * 0.6f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // Hostile shots pierce players by default; this one ends on its victim (the global relay kills it everywhere).
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.5f, Pitch = 0.3f }, Projectile.Center);
                WarlockArcanaDraw.MoteBurst(Projectile.Center, 18, 3.5f, 1.1f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<WarlockBurst>(), Projectile.damage, 0f, Main.myPlayer, Projectile.ai[0]);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            WarlockArcanaDraw.Orb(Projectile.Center, 28f, 1f, 0.6f, Projectile.identity);
            return false;
        }
    }

    /// <summary>
    /// The splash where a bolt ends: a 48px-radius circle, harmless for its first 2t flash, live for 12t, then a 12t harmless
    /// fade. One hit per player (ordinary player immunity). Damage is inherited from the bolt. ai[0] = owner key.
    /// </summary>
    public class WarlockBurst : ModProjectile
    {
        public const float Radius = 24f;
        private const int FlashTicks = 2;
        private const int LiveTicks = 12;
        private const int FadeTicks = 12;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)(Radius * 2f);
            Projectile.height = (int)(Radius * 2f);
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = FlashTicks + LiveTicks + FadeTicks;
        }

        public override bool? CanDamage()
        {
            bool live = Age > FlashTicks && Age <= FlashTicks + LiveTicks;
            if (!live)
            {
                return false;
            }

            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.Distance(nearest, Projectile.Center) <= Radius;
        }

        public override void AI()
        {
            Age++;
            Projectile.velocity = Vector2.Zero;
            if (!Main.dedServ && Age <= FlashTicks + LiveTicks)
            {
                Lighting.AddLight(Projectile.Center, WarlockArcanaDraw.Accent.ToVector3() * 0.7f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // The ring tops out at r = 0.8 of the half-quad, so a quad of Radius / 0.8 * 2 puts the ring on the hit edge.
            float total = FlashTicks + LiveTicks + FadeTicks;
            float age = Age / total;
            float opacity = 1f;
            if (Age > FlashTicks + LiveTicks)
            {
                opacity = 1f - (Age - FlashTicks - LiveTicks) / (float)FadeTicks;
            }

            WarlockArcanaDraw.Burst(Projectile.Center, Radius / 0.8f * 2f, age, opacity);
            return false;
        }
    }

    /// <summary>
    /// A beam segment from Projectile.Center to (ai[1], ai[2]): a 24px capsule, live for LiveTicks, then a harmless
    /// afterglow. Used by the Great Energy Beam and both Prism Beam segments. ai[0] = owner key.
    /// </summary>
    public class WarlockBeam : ModProjectile
    {
        public const float ColliderWidth = 24f;
        protected virtual int LiveTicks => 12;
        protected virtual int GlowTicks => 12;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        protected int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private Vector2 End => new Vector2(Projectile.ai[1], Projectile.ai[2]);

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = LiveTicks + GlowTicks;
        }

        public override bool? CanDamage()
        {
            if (Age > LiveTicks)
            {
                return false;
            }

            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, End,
                ColliderWidth, ref collisionPoint);
        }

        // The beam is a fixed segment: its origin never moves.
        public override bool ShouldUpdatePosition()
        {
            return false;
        }

        public override void AI()
        {
            Age++;
            if (Age == 1)
            {
                Projectile.timeLeft = LiveTicks + GlowTicks;
            }

            if (Main.dedServ)
            {
                return;
            }

            if (Age == 1)
            {
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.7f, Pitch = -0.2f }, Projectile.Center);
                WarlockArcanaDraw.MoteBurst(End, 16, 3f, 1.1f);
            }

            // Sparks along the live beam and where it meets the wall.
            if (Age <= LiveTicks)
            {
                Vector2 span = End - Projectile.Center;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 along = Projectile.Center + span * Main.rand.NextFloat();
                    Vector2 drift = span.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-1.2f, 1.2f);
                    WarlockArcanaDraw.Mote(along, drift, WarlockArcanaDraw.Accent, 1f);
                }

                WarlockArcanaDraw.Mote(End, Main.rand.NextVector2Circular(2.5f, 2.5f), WarlockArcanaDraw.Core, 1.2f);
                Lighting.AddLight(Projectile.Center, WarlockArcanaDraw.Mid.ToVector3());
                Lighting.AddLight(End, WarlockArcanaDraw.Mid.ToVector3());
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float heat = 1f;
            float opacity = 1f;
            if (Age > LiveTicks)
            {
                float glow = (Age - LiveTicks) / (float)GlowTicks;
                heat = 0f;
                opacity = 1f - glow;
            }

            // Quad 2.4x the collider: the hot core stays inside the 24px hit capsule, the braid and glow spill past it.
            WarlockArcanaDraw.Beam(Projectile.Center, End, ColliderWidth * 2.4f, opacity, heat);
            WarlockArcanaDraw.Orb(Projectile.Center, 30f * opacity, opacity, heat);
            return false;
        }
    }

    /// <summary>A Crossfire prism's shot: the same beam, live 10t with a 10t afterglow.</summary>
    public class WarlockCrossfireBeam : WarlockBeam
    {
        protected override int LiveTicks => 10;
        protected override int GlowTicks => 10;
    }

    /// <summary>
    /// Prism Beam's wall crystal: grows out of the wall where the frozen ray will strike, its long axis along the bounce,
    /// over the last 18t of the tell; shatters when the beams have fired, or at once if the cast is cancelled. Harmless.
    /// ai[0] = owner key, ai[1] = bounce angle (radians).
    /// </summary>
    public class WarlockWallPrism : ModProjectile
    {
        private const int GrowTicks = 18;
        private const int HoldTicks = 12;
        private const int ShatterTicks = 12;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private int _shatterAge = -1;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = GrowTicks + HoldTicks + ShatterTicks + 4;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void AI()
        {
            Age++;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = Projectile.ai[1];

            bool castAlive = WarlockGeometry.FindSameCast(Projectile.ai[0], WarlockNPC.Spell.PrismBeam) != null;
            bool beamsFired = Age > GrowTicks + HoldTicks;
            if (_shatterAge < 0 && (!castAlive || beamsFired))
            {
                _shatterAge = Age;
                if (!Main.dedServ)
                {
                    WarlockArcanaDraw.MoteBurst(Projectile.Center, 24, 3f, 1.1f);
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, Pitch = 0.4f }, Projectile.Center);
                }
            }

            if (_shatterAge >= 0 && Age - _shatterAge >= ShatterTicks)
            {
                Projectile.Kill();
                return;
            }

            // Growing: motes drift off the crystal ALONG the bounce, a short way, so its orientation is unmistakable.
            if (!Main.dedServ && _shatterAge < 0 && Age % 2 == 0)
            {
                Vector2 bounce = Projectile.ai[1].ToRotationVector2();
                Vector2 spot = Projectile.Center + Main.rand.NextVector2Circular(6f, 6f);
                WarlockArcanaDraw.Mote(spot, bounce * Main.rand.NextFloat(1.5f, 3f), WarlockArcanaDraw.Accent, 0.9f);
                Lighting.AddLight(Projectile.Center, WarlockArcanaDraw.Accent.ToVector3() * 0.5f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float grow = MathHelper.Clamp(Age / (float)GrowTicks, 0f, 1f);
            float opacity = 1f;
            if (_shatterAge >= 0)
            {
                opacity = 1f - (Age - _shatterAge) / (float)ShatterTicks;
            }

            float flare = MathHelper.Clamp((Age - GrowTicks) / (float)HoldTicks, 0f, 1f);
            float length = MathHelper.Lerp(12f, 34f, grow);
            WarlockArcanaDraw.Prism(Projectile.Center, length, Projectile.rotation, opacity * MathHelper.Lerp(0.4f, 1f, grow), flare,
                Projectile.identity);
            return false;
        }
    }

    /// <summary>
    /// Orrery Volley orb. Forms at the staff gem, drifts out to orbit it at 40px, and launches on its own schedule: orb i
    /// (formed at Warlock tick 12 + 10i) launches at Warlock tick 48 + 32i, i.e. at age 36 + 22i. For the 6 ticks before
    /// launch it holds still and the SERVER locks its aim on the target's position; launch is a server velocity write
    /// (synced), so every machine flies the same line. 7 px/t, no homing, 100t of flight with a 12t harmless fade.
    /// A poise break (or any change of cast) before launch scatters it harmlessly. ai[0] = owner key, ai[1] = target
    /// player, ai[2] = orb index.
    /// </summary>
    public class WarlockOrreryOrb : ModProjectile
    {
        public const float OrbitRadius = 40f;
        public const float FlightSpeed = 7f;
        private const int LockTicks = 6;
        private const int FlightTicks = 100;
        private const int FadeTicks = 12;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private int Index => (int)Projectile.ai[2];
        private int LaunchAge => 36 + 22 * Index;
        private bool Launched => Projectile.velocity.LengthSquared() > 1f;

        private Vector2 _holdPosition;
        private int _launchedAge = -1;
        private int _scatterAge = -1;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage()
        {
            bool fading = _launchedAge >= 0 && Age - _launchedAge > FlightTicks - FadeTicks;
            if (!Launched || fading || _scatterAge >= 0)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            Age++;
            Projectile.rotation += 0.15f;

            // Scattering: harmless motes for 12t, then gone.
            if (_scatterAge >= 0)
            {
                Projectile.velocity = Vector2.Zero;
                if (Age - _scatterAge >= FadeTicks)
                {
                    Projectile.Kill();
                }

                return;
            }

            if (Launched)
            {
                if (_launchedAge < 0)
                {
                    _launchedAge = Age;
                }

                Projectile.tileCollide = true;
                if (Age - _launchedAge >= FlightTicks)
                {
                    Projectile.Kill();
                    return;
                }

                if (!Main.dedServ && Age % 3 == 0)
                {
                    WarlockArcanaDraw.Mote(Projectile.Center, -Projectile.velocity * 0.08f, WarlockArcanaDraw.Accent, 1f);
                }

                return;
            }

            WarlockNPC warlock = WarlockGeometry.FindSameCast(Projectile.ai[0], WarlockNPC.Spell.Orrery);
            if (warlock == null)
            {
                _scatterAge = Age;
                if (!Main.dedServ)
                {
                    WarlockArcanaDraw.MoteBurst(Projectile.Center, 14, 2f, 0.9f);
                }

                return;
            }

            int lockAge = LaunchAge - LockTicks;
            if (Age < lockAge)
            {
                // Orbit the gem, evenly spaced by index, turning with the Warlock's facing. The first 10t drift out from it.
                int count = warlock.OrreryCount;
                float angle = Age * 0.05f * warlock.NPC.direction + Index * MathHelper.TwoPi / count;
                float radius = OrbitRadius * MathHelper.Clamp(Age / 10f, 0f, 1f);
                Projectile.Center = warlock.GemTip + angle.ToRotationVector2() * radius;
                Projectile.velocity = Vector2.Zero;
                _holdPosition = Projectile.Center;
                return;
            }

            // Aim lock: hold still and flash. The server freezes the aim at the lock tick and launches at LaunchAge.
            Projectile.Center = _holdPosition;
            Projectile.velocity = Vector2.Zero;
            if (Main.netMode != NetmodeID.MultiplayerClient && Age >= LaunchAge)
            {
                Player target = Main.player[(int)Projectile.ai[1]];
                Vector2 aim = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX * warlock.NPC.direction);
                Projectile.velocity = aim * FlightSpeed;
                Projectile.netUpdate = true;
            }

            if (!Main.dedServ)
            {
                if (Age == lockAge)
                {
                    SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.6f, Pitch = 0.2f + Index * 0.15f }, Projectile.Center);
                }

                WarlockArcanaDraw.Mote(Projectile.Center + Main.rand.NextVector2CircularEdge(14f, 14f), Vector2.Zero,
                    WarlockArcanaDraw.Core, 0.8f);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ && _scatterAge < 0)
            {
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.45f, Pitch = 0.5f }, Projectile.Center);
                WarlockArcanaDraw.MoteBurst(Projectile.Center, 30, 3.5f, 1.1f);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float grow = MathHelper.Clamp(Age / 10f, 0.25f, 1f);
            float opacity = 1f;
            float charge = 0.3f;

            if (_scatterAge >= 0)
            {
                opacity = 1f - (Age - _scatterAge) / (float)FadeTicks;
            }
            else if (_launchedAge >= 0 && Age - _launchedAge > FlightTicks - FadeTicks)
            {
                opacity = (FlightTicks - (Age - _launchedAge)) / (float)FadeTicks;
            }
            else if (!Launched && Age >= LaunchAge - LockTicks)
            {
                charge = 1f; // the lock flash
            }

            WarlockArcanaDraw.Orb(Projectile.Center, 30f * grow, opacity, charge, Index * 1.7f);
            return false;
        }
    }

    /// <summary>
    /// Crossfire prism. Spawned by the server on a 240px circle around the target; forms over 30t, turns to face the
    /// player, locks at age 36 + 20i (the server freezes the lane through the player's spot, synced in ExtraAI), fires a
    /// WarlockCrossfireBeam through it 18t later, then shatters. A change of cast before firing shatters it harmlessly.
    /// ai[0] = owner key, ai[1] = target player, ai[2] = prism index.
    /// </summary>
    public class WarlockCrossfirePrism : ModProjectile
    {
        public const int FormTicks = 30;
        public const int FirstLockAge = 36;
        public const int LockSpacing = 20;
        public const int LockToFireTicks = 18;
        public const float BeamReach = 720f;
        private const int AfterFireTicks = 10;
        private const int ShatterTicks = 12;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private int Index => (int)Projectile.ai[2];
        private int LockAge => FirstLockAge + LockSpacing * Index;
        private int FireAge => LockAge + LockToFireTicks;

        private Vector2 _lockedPoint;
        private bool _locked;
        private int _shatterAge = -1;

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_locked);
            writer.WriteVector2(_lockedPoint);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _locked = reader.ReadBoolean();
            _lockedPoint = reader.ReadVector2();
        }

        public override void AI()
        {
            Age++;
            Projectile.velocity = Vector2.Zero;
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            Player target = Main.player[(int)Projectile.ai[1]];

            if (_shatterAge >= 0)
            {
                if (Age - _shatterAge >= ShatterTicks)
                {
                    Projectile.Kill();
                }

                return;
            }

            bool fired = Age > FireAge;
            bool castAlive = WarlockGeometry.FindSameCast(Projectile.ai[0], WarlockNPC.Spell.Crossfire) != null;
            bool done = Age >= FireAge + AfterFireTicks;
            if ((!fired && !castAlive) || done)
            {
                _shatterAge = Age;
                if (!Main.dedServ)
                {
                    WarlockArcanaDraw.MoteBurst(Projectile.Center, 20, 2.6f, 1f);
                    SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = 0.5f }, Projectile.Center);
                }

                return;
            }

            // Facing: turns smoothly toward the player until the lock, then holds the locked lane.
            Vector2 facePoint = target.Center;
            if (_locked)
            {
                facePoint = _lockedPoint;
            }

            float wanted = (facePoint - Projectile.Center).ToRotation();
            if (Age <= 1)
            {
                Projectile.rotation = wanted;
            }

            Projectile.rotation = Projectile.rotation.AngleLerp(wanted, 0.18f);

            if (authority && !_locked && Age >= LockAge)
            {
                _locked = true;
                _lockedPoint = target.Center;
                Projectile.netUpdate = true;
            }

            if (authority && Age == FireAge)
            {
                Vector2 direction = (_lockedPoint - Projectile.Center).SafeNormalize(Projectile.rotation.ToRotationVector2());
                Vector2 end = WarlockGeometry.CastToWall(Projectile.Center, direction, BeamReach, out _);
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<WarlockCrossfireBeam>(), Projectile.damage, 0f, Main.myPlayer,
                    Projectile.ai[0], end.X, end.Y);
            }

            if (Main.dedServ)
            {
                return;
            }

            // Forming: motes gather into the crystal. Locked: motes jet a short way out of its firing tip.
            if (Age < FormTicks)
            {
                Vector2 from = Projectile.Center + Main.rand.NextVector2CircularEdge(26f, 26f);
                WarlockArcanaDraw.Mote(from, (Projectile.Center - from) * 0.12f, WarlockArcanaDraw.Accent, 0.9f);
            }
            else if (_locked && Age < FireAge)
            {
                Vector2 tipDirection = Projectile.rotation.ToRotationVector2();
                Vector2 tip = Projectile.Center + tipDirection * 16f;
                WarlockArcanaDraw.Mote(tip, tipDirection * Main.rand.NextFloat(2f, 3.5f), WarlockArcanaDraw.Core, 0.9f);
            }

            if (Age == LockAge)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.5f, Pitch = 0.3f + Index * 0.2f }, Projectile.Center);
            }

            Lighting.AddLight(Projectile.Center, WarlockArcanaDraw.Accent.ToVector3() * 0.45f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float form = MathHelper.Clamp(Age / (float)FormTicks, 0f, 1f);
            float opacity = form;
            if (_shatterAge >= 0)
            {
                opacity = 1f - (Age - _shatterAge) / (float)ShatterTicks;
            }

            float flare = 0f;
            if (_locked)
            {
                flare = MathHelper.Clamp((Age - LockAge) / (float)LockToFireTicks, 0f, 1f);
            }

            WarlockArcanaDraw.Prism(Projectile.Center, 40f * Math.Max(form, 0.2f), Projectile.rotation, opacity, flare, Projectile.identity);
            return false;
        }
    }

    /// <summary>
    /// Collapse Point. A dark-hearted orb leaves the gem at 3.5 px/t toward the spot the target stood on at release (ai[1],
    /// ai[2]) and stops there, at a wall, or after 90t. Harmless in flight. It then collapses for 60t, pulling each client's
    /// own player inward (1.4 px/t at the centre, 0.3 at 200px, tile-checked), and bursts: 72px radius, live 10t, then a
    /// 14t fade. Once released it finishes even if the Warlock dies. ai[0] = owner key.
    /// </summary>
    public class WarlockCollapsePoint : ModProjectile
    {
        public const float TravelSpeed = 3.5f;
        public const float PullRadius = 200f;
        public const float BurstRadius = 72f;
        private const int MaxTravelTicks = 90;
        private const int CollapseTicks = 60;
        private const int BurstLiveTicks = 10;
        private const int BurstFadeTicks = 14;
        private const float PullAtCentre = 1.4f;
        private const float PullAtEdge = 0.3f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        // Ticks since the orb stopped, or -1 while it still travels. Per machine; the stop itself is a synced velocity write.
        private int _stoppedAge = -1;

        private int CollapseAge
        {
            get
            {
                if (_stoppedAge < 0)
                {
                    return -1;
                }

                return Age - _stoppedAge;
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;
            Projectile.ignoreWater = true;
        }

        public override bool? CanDamage()
        {
            int collapseAge = CollapseAge;
            bool burstLive = collapseAge > CollapseTicks && collapseAge <= CollapseTicks + BurstLiveTicks;
            if (!burstLive)
            {
                return false;
            }

            return null;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            return Vector2.Distance(nearest, Projectile.Center) <= BurstRadius;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = Vector2.Zero;
            return false;
        }

        public override void AI()
        {
            Age++;
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            Vector2 stopPoint = new Vector2(Projectile.ai[1], Projectile.ai[2]);

            // Travel: the server decides the stop (arrived / timed out) and syncs it as zero velocity.
            if (_stoppedAge < 0)
            {
                bool arrived = Vector2.Distance(Projectile.Center, stopPoint) <= TravelSpeed;
                bool timedOut = Age >= MaxTravelTicks;
                if (authority && (arrived || timedOut) && Projectile.velocity != Vector2.Zero)
                {
                    Projectile.velocity = Vector2.Zero;
                    Projectile.netUpdate = true;
                }

                if (Projectile.velocity.LengthSquared() < 0.01f && Age > 1)
                {
                    _stoppedAge = Age;
                    Projectile.tileCollide = false;
                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item117 with { Volume = 0.7f, Pitch = -0.5f }, Projectile.Center);
                    }
                }
                else if (!Main.dedServ && Age % 2 == 0)
                {
                    WarlockArcanaDraw.Mote(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), -Projectile.velocity * 0.2f,
                        WarlockArcanaDraw.Deep, 1f);
                }

                return;
            }

            Projectile.velocity = Vector2.Zero;
            int collapseAge = CollapseAge;

            if (collapseAge >= CollapseTicks + BurstLiveTicks + BurstFadeTicks)
            {
                Projectile.Kill();
                return;
            }

            if (collapseAge < CollapseTicks)
            {
                PullLocalPlayer();
            }

            if (Main.dedServ)
            {
                return;
            }

            if (collapseAge < CollapseTicks)
            {
                // Motes drawn inward from the pull radius, spiralling.
                for (int i = 0; i < 2; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(PullRadius, PullRadius) * Main.rand.NextFloat(0.6f, 1f);
                    Vector2 inward = -offset.SafeNormalize(Vector2.UnitY) * 3f + offset.RotatedBy(MathHelper.PiOver2).SafeNormalize(Vector2.UnitY) * 1.2f;
                    WarlockArcanaDraw.Mote(Projectile.Center + offset, inward, WarlockArcanaDraw.Mid, 1f);
                }

                Lighting.AddLight(Projectile.Center, WarlockArcanaDraw.Deep.ToVector3());
            }

            if (collapseAge == CollapseTicks)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = 0.2f }, Projectile.Center);
                WarlockArcanaDraw.MoteBurst(Projectile.Center, 40, 6f, 1.3f);
            }
        }

        // Runs on every machine, but only moves that machine's own player (the server has none), so the pull needs no packet.
        // A tile-checked position nudge rather than velocity: walls stop it, and the player's own walking always beats it.
        private void PullLocalPlayer()
        {
            if (Main.dedServ)
            {
                return;
            }

            Player player = Main.LocalPlayer;
            if (!player.active || player.dead || player.ghost)
            {
                return;
            }

            Vector2 toCentre = Projectile.Center - player.Center;
            float distance = toCentre.Length();
            if (distance > PullRadius || distance < 4f)
            {
                return;
            }

            float closeness = 1f - distance / PullRadius;
            float pullSpeed = MathHelper.Lerp(PullAtEdge, PullAtCentre, closeness);
            Vector2 pull = toCentre / distance * pullSpeed;
            Vector2 allowed = Collision.TileCollision(player.position, pull, player.width, player.height, true, true, (int)player.gravDir);
            player.position += allowed;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int collapseAge = CollapseAge;
            if (collapseAge < 0)
            {
                WarlockArcanaDraw.Vortex(Projectile.Center, 56f, 1f, 0.6f);
                return false;
            }

            if (collapseAge <= CollapseTicks)
            {
                // Opens over 10t; the disc (r = 0.7 of the half-quad) spans the pull radius, then contracts over the last 20t.
                float open = MathHelper.Clamp(collapseAge / 10f, 0f, 1f);
                float collapse = MathHelper.Clamp((collapseAge - (CollapseTicks - 20)) / 20f, 0f, 1f);
                WarlockArcanaDraw.Vortex(Projectile.Center, PullRadius * 2f / 0.7f * MathHelper.Lerp(0.3f, 1f, open), open, collapse);
                return false;
            }

            float burstAge = (collapseAge - CollapseTicks) / (float)(BurstLiveTicks + BurstFadeTicks);
            float burstOpacity = 1f;
            if (collapseAge > CollapseTicks + BurstLiveTicks)
            {
                burstOpacity = 1f - (collapseAge - CollapseTicks - BurstLiveTicks) / (float)BurstFadeTicks;
            }

            WarlockArcanaDraw.Burst(Projectile.Center, BurstRadius / 0.8f * 2f, burstAge, burstOpacity);
            return false;
        }
    }

    /// <summary>
    /// Covenant Ward ring + Bastion. Anchored on the floor under the planted gem, radius 320px. Allies inside it (never the
    /// Warlock) take 25% less damage while it stands (WarlockBastionGlobalNPC). The Warlock pays the heal at its own tick
    /// 90 (ring age 74); the ring then stands 240t more and fades over 30. A cast change before the payout collapses it at
    /// once (no heal, no Bastion); the Warlock's death fades it. Harmless. ai[0] = owner key.
    /// </summary>
    public class WarlockWardRing : ModProjectile
    {
        public const float Radius = 320f;
        public const float BastionDamageTaken = 0.75f;
        public const int PayoutAge = 74;
        private const int FormTicks = 20;
        private const int LingerTicks = 240;
        private const int FadeTicks = 30;
        private const int CollapseTicks = 20;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private int Age
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private int _endAge = -1;     // when it began to fade or collapse
        private int _endTicks;        // how long that fade lasts

        private bool Ending => _endAge >= 0;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = PayoutAge + LingerTicks + FadeTicks + 10;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        // Bastion is up from the moment the ring stands until it starts to fade or collapse.
        private bool BastionUp => !Ending;

        /// <summary>True if `npc` stands inside any live Bastion ring that it doesn't own. Runs wherever the hit is computed.</summary>
        public static bool Protects(NPC npc)
        {
            if (npc.friendly || npc.townNPC)
            {
                return false;
            }

            int ringType = ModContent.ProjectileType<WarlockWardRing>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != ringType || projectile.ModProjectile is not WarlockWardRing ring)
                {
                    continue;
                }

                int ownerIndex = (int)projectile.ai[0] & 255;
                bool ownerItself = ownerIndex == npc.whoAmI;
                if (ownerItself || !ring.BastionUp)
                {
                    continue;
                }

                if (Vector2.Distance(npc.Center, projectile.Center) <= Radius)
                {
                    return true;
                }
            }

            return false;
        }

        public override void AI()
        {
            Age++;
            Projectile.velocity = Vector2.Zero;

            if (Ending)
            {
                if (Age - _endAge >= _endTicks)
                {
                    Projectile.Kill();
                }

                return;
            }

            WarlockNPC owner = WarlockGeometry.FindOwner(Projectile.ai[0]);
            bool channelBroken = Age < PayoutAge && WarlockGeometry.FindSameCast(Projectile.ai[0], WarlockNPC.Spell.Ward) == null;
            if (owner == null || channelBroken)
            {
                _endAge = Age;
                _endTicks = FadeTicks;
                if (channelBroken)
                {
                    _endTicks = CollapseTicks;
                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.5f, Pitch = -0.3f }, Projectile.Center);
                    }
                }

                return;
            }

            if (Age >= PayoutAge + LingerTicks)
            {
                _endAge = Age;
                _endTicks = FadeTicks;
                return;
            }

            if (Main.dedServ)
            {
                return;
            }

            // Rune motes along the ring's floor ellipse, and a shimmer on every ally it protects.
            for (int i = 0; i < 2; i++)
            {
                float side = 1f;
                if (Main.rand.NextBool())
                {
                    side = -1f;
                }

                Vector2 spot = Projectile.Center + new Vector2(side * Radius * Main.rand.NextFloat(0.85f, 1f) * Math.Min(1f, Age / (float)FormTicks), -2f);
                WarlockArcanaDraw.Mote(spot, new Vector2(0f, -Main.rand.NextFloat(0.4f, 1.4f)), WarlockArcanaDraw.Accent, 0.9f);
            }

            if (Age % 6 == 0)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC ally = Main.npc[i];
                    bool protectedAlly = ally.active && !ally.friendly && i != ((int)Projectile.ai[0] & 255)
                        && Vector2.Distance(ally.Center, Projectile.Center) <= Radius;
                    if (protectedAlly)
                    {
                        Vector2 spot = ally.position + new Vector2(Main.rand.NextFloat(ally.width), Main.rand.NextFloat(ally.height));
                        WarlockArcanaDraw.Mote(spot, new Vector2(0f, -1f), WarlockArcanaDraw.Accent, 0.8f);
                    }
                }
            }

            if (Age == PayoutAge)
            {
                // The payout flash (dust, not shader: the Ward technique had no slot budget left for it).
                for (int i = 0; i < 40; i++)
                {
                    float x = Main.rand.NextFloat(-Radius, Radius);
                    WarlockArcanaDraw.Mote(Projectile.Center + new Vector2(x, -4f), new Vector2(0f, -Main.rand.NextFloat(2f, 5f)),
                        WarlockArcanaDraw.Core, 1.2f);
                }
            }

            Lighting.AddLight(Projectile.Center + new Vector2(0f, -12f), WarlockArcanaDraw.Accent.ToVector3() * 0.4f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float formation = MathHelper.Clamp(Age / (float)FormTicks, 0f, 1f);
            float opacity = 1f;
            float bastion = 1f;
            if (Ending)
            {
                float endProgress = (Age - _endAge) / (float)Math.Max(1, _endTicks);
                opacity = 1f - endProgress;
                bastion = 0f;
            }

            WarlockArcanaDraw.Ward(Projectile.Center, Radius, formation, bastion * formation, opacity);
            return false;
        }
    }

    /// <summary>Bastion: allies inside a Warlock's Ward ring take 25% less damage. See WarlockWardRing.Protects.</summary>
    public class WarlockBastionGlobalNPC : GlobalNPC
    {
        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (WarlockWardRing.Protects(npc))
            {
                modifiers.FinalDamage *= WarlockWardRing.BastionDamageTaken;
            }
        }
    }
}
