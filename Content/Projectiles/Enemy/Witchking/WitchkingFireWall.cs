using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Witchking
{
    /// <summary>
    /// Hellfire Plunge's wall of fire: erupts from the floor where the fire sword is buried, 20 tiles
    /// tall, then rolls 40 tiles along the floor in one direction. Ground-crawler: each tick it snaps to the floor,
    /// climbing up to 3 tiles or dropping up to 5; a taller wall or a deeper pit ends it early (fade).
    /// Drawn as a stacked column of Destined Death blaze sprites (the Great Red Knight's wave flame) with a short
    /// tail of wisps shed off the back that fall behind and fade. Everything drawn is inside the hitbox: the column
    /// plus the TrailLength tail, so every flame on screen burns. Thin enough to ROLL through: closing at 8+8 px/t
    /// the overlap (40 wall + 56 tail + 20 player) lasts ~7 ticks, well inside 22 roll i-frames.
    /// ai[0] = direction (+1 / -1). ai[1] = floor Y in world px (spawner passes the floor under the sword).
    /// </summary>
    public class WitchkingFireWall : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        public const float WallHeight = 20f * 16f;
        public const float TravelDistance = 40f * 16f;
        private const float WallWidth = 40f;
        // Wisps live in this band behind the wall; the hitbox covers it too.
        private const float TrailLength = 56f;
        private const float Speed = 8f;
        private const int EruptTicks = 15;
        private const int TravelTicks = (int)(TravelDistance / Speed);
        private const int FadeTicks = 20;
        private const int MaxClimbTiles = 3;
        private const int MaxDropTiles = 5;
        private const int DestinedDeathTicks = 600;

        // Column sprites: one blaze every SlotSpacing px up the wall. DestinedDeathBlaze frames are 86x82 (5 frames).
        private const float SlotSpacing = 28f;
        private const int BlazeFrameCount = 5;
        private const int BlazeTicksPerFrame = 5;

        // Wisps fall back at 1.6-3.6 px/t relative to the wall for up to 15 ticks: at most 54px, inside TrailLength.
        private const int WispLifetime = 15;
        private const float WispMaxLag = 3.6f;

        // A blaze shed off the back of the wall. Stored RELATIVE to the wall (Behind = px behind its centre, Height =
        // px above the floor) so wisps stop with the wall when it fades instead of overtaking it. Client-only decor.
        private struct Wisp
        {
            public float Behind;
            public float Height;
            public float LagSpeed;
            public float RiseSpeed;
            public int Age;
            public int Frame;
            public float Scale;
            public float Rotation;
        }

        private readonly List<Wisp> _wisps = new List<Wisp>();

        private int Age => (int)Projectile.localAI[0];

        private float Direction => Projectile.ai[0] >= 0f ? 1f : -1f;

        // Age the fade started (0 = not fading). Every peer reaches the same value: the walk is deterministic.
        private int FadeStartAge
        {
            get => (int)Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        private bool Travelling => FadeStartAge == 0 && Age > EruptTicks;

        // 0..1 share of the full height that is up this tick: rises through the eruption, sinks through the fade.
        private float HeightFraction
        {
            get
            {
                if (FadeStartAge > 0)
                {
                    float fadeProgress = (Age - FadeStartAge) / (float)FadeTicks;
                    return MathHelper.Clamp(1f - fadeProgress, 0f, 1f);
                }

                float erupt = Age / (float)EruptTicks;
                return MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(erupt, 0f, 1f));
            }
        }

        public override void SetDefaults()
        {
            Projectile.width = (int)WallWidth;
            Projectile.height = (int)WallHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = EruptTicks + TravelTicks + FadeTicks + 5;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            float floorY = Projectile.ai[1];

            if (Age == 1 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.4f }, Projectile.Center);
            }

            // Roll along the floor once fully erupted, until the travel distance is spent.
            if (Travelling)
            {
                float centerX = Projectile.Center.X + Direction * Speed;
                bool onFloor = TrySnapToFloor(centerX, ref floorY);
                if (!onFloor || Age >= EruptTicks + TravelTicks)
                {
                    FadeStartAge = Age;
                }
                else
                {
                    Projectile.Center = new Vector2(centerX, Projectile.Center.Y);
                    Projectile.ai[1] = floorY;
                }
            }

            Projectile.Center = new Vector2(Projectile.Center.X, floorY - WallHeight / 2f);

            if (FadeStartAge > 0 && Age - FadeStartAge >= FadeTicks)
            {
                Projectile.Kill();
                return;
            }

            UpdateWisps();
            SpawnWallDust(floorY);
        }

        // Ground-crawler snap: climb a step of up to MaxClimbTiles, or drop up to MaxDropTiles. False = a taller wall or
        // a deeper pit, which ends the crawler. Shared with Grond's void shockwave.
        internal static bool TrySnapToFloor(float centerX, ref float floorY)
        {
            int tileX = (int)(centerX / 16f);
            int floorTileY = (int)(floorY / 16f);

            int climbed = 0;
            while (WorldGen.SolidTile(tileX, floorTileY - 1))
            {
                if (climbed >= MaxClimbTiles)
                {
                    return false;
                }
                floorTileY--;
                climbed++;
            }

            int dropped = 0;
            while (!WorldGen.SolidTile(tileX, floorTileY))
            {
                if (dropped >= MaxDropTiles)
                {
                    return false;
                }
                floorTileY++;
                dropped++;
            }

            floorY = floorTileY * 16f;
            return true;
        }

        // The hitbox is everything that is drawn: the wall column plus the wisp tail behind it, from the floor to the
        // current height. The tail only exists once the wall is moving.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float currentHeight = WallHeight * HeightFraction;
            float floorY = Projectile.ai[1];

            float leftEdge = Projectile.Center.X - WallWidth / 2f;
            float width = WallWidth;
            bool hasTail = Age > EruptTicks;
            if (hasTail)
            {
                width += TrailLength;
                if (Direction > 0f)
                {
                    leftEdge -= TrailLength;
                }
            }

            Rectangle burning = new Rectangle(
                (int)leftEdge,
                (int)(floorY - currentHeight),
                (int)width,
                (int)currentHeight);
            return burning.Intersects(targetHitbox);
        }

        // Harmless until it is at least 40% up, so the eruption itself is the last beat of the tell.
        public override bool? CanDamage() => HeightFraction >= 0.4f ? null : false;

        // Runs on the hit player's machine. Same debuff and duration as the Great Red Knight's wave.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<Buffs.Debuffs.DestinedDeath>(), DestinedDeathTicks);
        }

        // Shed new wisps off the back of the moving wall and age the old ones. Wisps only lag while the wall travels,
        // so they never leave the tail band; when the wall fades they hold position and fade with it.
        private void UpdateWisps()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = _wisps.Count - 1; i >= 0; i--)
            {
                Wisp wisp = _wisps[i];
                wisp.Age++;
                if (Travelling)
                {
                    wisp.Behind += wisp.LagSpeed;
                }
                wisp.Height += wisp.RiseSpeed;
                wisp.Rotation -= Direction * 0.03f;
                if (wisp.Age % BlazeTicksPerFrame == 0)
                {
                    wisp.Frame = (wisp.Frame + 1) % BlazeFrameCount;
                }

                if (wisp.Age >= WispLifetime)
                {
                    _wisps.RemoveAt(i);
                    continue;
                }
                _wisps[i] = wisp;
            }

            if (!Travelling)
            {
                return;
            }

            // Two a tick, from anywhere up the wall below its crest (so a rising wisp stays under the hitbox top).
            float currentHeight = WallHeight * HeightFraction;
            for (int i = 0; i < 2; i++)
            {
                Wisp wisp = new Wisp();
                wisp.Behind = WallWidth * 0.3f;
                wisp.Height = Main.rand.NextFloat(0f, System.Math.Max(0f, currentHeight - 40f));
                wisp.LagSpeed = Main.rand.NextFloat(1.6f, WispMaxLag);
                wisp.RiseSpeed = Main.rand.NextFloat(0.2f, 1f);
                wisp.Frame = Main.rand.Next(BlazeFrameCount);
                wisp.Scale = Main.rand.NextFloat(0.35f, 0.6f);
                wisp.Rotation = Main.rand.NextFloat(-0.2f, 0.2f);
                _wisps.Add(wisp);
            }
        }

        // Smoke over the crest and Destined Death embers off the front; the blaze sprites carry the body of the wall.
        private void SpawnWallDust(float floorY)
        {
            if (Main.dedServ)
            {
                return;
            }

            float currentHeight = WallHeight * HeightFraction;
            int emberCount = 1 + (int)(4f * HeightFraction);
            for (int i = 0; i < emberCount; i++)
            {
                Vector2 position = new Vector2(
                    Projectile.Center.X + Direction * Main.rand.NextFloat(0f, WallWidth / 2f),
                    floorY - Main.rand.NextFloat(0f, currentHeight));
                int dustType = DustID.RedTorch;
                if (Main.rand.NextBool(3))
                {
                    dustType = DustID.Shadowflame;
                }
                Vector2 velocity = new Vector2(Direction * Main.rand.NextFloat(0.5f, 2f), Main.rand.NextFloat(-3f, -1f));
                Dust ember = Dust.NewDustPerfect(position, dustType, velocity, 80, default, Main.rand.NextFloat(1.1f, 1.7f));
                ember.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 crest = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-WallWidth / 2f, WallWidth / 2f), floorY - currentHeight);
                Dust smoke = Dust.NewDustPerfect(crest, DustID.Wraith, new Vector2(0f, -1.5f), 120, Color.Black, 1.8f);
                smoke.noGravity = true;
            }

            for (float y = 0f; y < currentHeight; y += 48f)
            {
                Lighting.AddLight(new Vector2(Projectile.Center.X, floorY - y), 0.7f, 0.08f, 0.04f);
            }
        }

        // Wisps first (behind), then the column. Each column slot gets a fixed scale, sway phase and starting frame
        // from its index, so the wall flickers in place instead of reshuffling every tick. Returns false: the
        // MagicPixel placeholder texture is never drawn (it showed as a white line).
        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D blaze = TextureAssets.Projectile[ModContent.ProjectileType<DestinedDeathBlaze>()].Value;
            int frameHeight = blaze.Height / BlazeFrameCount;
            Vector2 origin = new Vector2(blaze.Width * 0.5f, frameHeight);
            SpriteEffects flip = SpriteEffects.None;
            if (Direction < 0f)
            {
                flip = SpriteEffects.FlipHorizontally;
            }

            float floorY = Projectile.ai[1];
            float wallFade = MathHelper.Clamp(HeightFraction * 1.5f, 0f, 1f);

            foreach (Wisp wisp in _wisps)
            {
                float life = wisp.Age / (float)WispLifetime;
                float opacity = (1f - life) * wallFade;
                float scale = wisp.Scale * (1f - life * 0.4f);
                Vector2 worldPosition = new Vector2(Projectile.Center.X - Direction * wisp.Behind, floorY - wisp.Height);
                Rectangle frame = new Rectangle(0, wisp.Frame * frameHeight, blaze.Width, frameHeight);
                Main.EntitySpriteDraw(blaze, worldPosition - Main.screenPosition, frame, Color.White * opacity,
                    wisp.Rotation, origin, scale, flip, 0f);
            }

            float currentHeight = WallHeight * HeightFraction;
            int slotCount = (int)(currentHeight / SlotSpacing) + 1;
            for (int slot = 0; slot < slotCount; slot++)
            {
                // Bottom slot sits on the floor; the top two taper so the crest reads as tongues, not a flat lid.
                float slotHeight = slot * SlotSpacing;
                float crestTaper = MathHelper.Clamp((currentHeight - slotHeight) / (SlotSpacing * 2f), 0.35f, 1f);
                float slotScale = (0.62f + 0.14f * (float)System.Math.Sin(slot * 2.3f)) * crestTaper;
                float sway = (float)System.Math.Sin(Age * 0.25f + slot * 1.7f) * 6f;
                float rotation = (float)System.Math.Sin(Age * 0.18f + slot) * 0.12f;
                int frameIndex = (Age / BlazeTicksPerFrame + slot * 2) % BlazeFrameCount;

                Vector2 worldPosition = new Vector2(Projectile.Center.X + sway, floorY - slotHeight + SlotSpacing * 0.5f);
                Rectangle frame = new Rectangle(0, frameIndex * frameHeight, blaze.Width, frameHeight);
                Main.EntitySpriteDraw(blaze, worldPosition - Main.screenPosition, frame, Color.White * wallFade,
                    rotation, origin, slotScale, flip, 0f);
            }

            return false;
        }
    }
}
