using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using WitchkingBoss = tsorcRevamp.NPCs.Bosses.SuperHardMode.Witchking;

namespace tsorcRevamp.Content.Projectiles.Enemy.Witchking
{
    /// <summary>
    /// Hellfire Plunge's wall of fire: erupts from the floor where the fire sword is buried, 20 tiles
    /// tall, then rolls 40 tiles along the floor in one direction. Ground-crawler: each tick it snaps to the floor,
    /// climbing up to 3 tiles or dropping up to 5; a taller wall or a deeper pit ends it early (fade). Thin (40px), so
    /// it is ROLLED through, not jumped: closing at 8+8 px/t the overlap lasts ~4 ticks, well inside 22 roll i-frames.
    /// ai[0] = direction (+1 / -1). ai[1] = floor Y in world px (spawner passes the floor under the sword).
    /// Dust-only visual (MagicPixel texture, never drawn).
    /// </summary>
    public class WitchkingFireWall : ModProjectile
    {
        public override string Texture => "Terraria/Images/MagicPixel";

        public const float WallHeight = 20f * 16f;
        public const float TravelDistance = 40f * 16f;
        private const float WallWidth = 40f;
        private const float Speed = 8f;
        private const int EruptTicks = 15;
        private const int TravelTicks = (int)(TravelDistance / Speed);
        private const int FadeTicks = 20;
        private const int MaxClimbTiles = 3;
        private const int MaxDropTiles = 5;

        private int Age => (int)Projectile.localAI[0];

        // Age the fade started (0 = not fading). Every peer reaches the same value: the walk is deterministic.
        private int FadeStartAge
        {
            get => (int)Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

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
            float direction = Projectile.ai[0] >= 0f ? 1f : -1f;
            float floorY = Projectile.ai[1];

            if (Age == 1 && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.4f }, Projectile.Center);
            }

            // Roll along the floor once fully erupted, until the travel distance is spent.
            bool travelling = FadeStartAge == 0 && Age > EruptTicks;
            if (travelling)
            {
                float centerX = Projectile.Center.X + direction * Speed;
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

        // The hitbox is the column that is actually up: full width, from the floor to the current height.
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float currentHeight = WallHeight * HeightFraction;
            float floorY = Projectile.ai[1];
            Rectangle column = new Rectangle(
                (int)(Projectile.Center.X - WallWidth / 2f),
                (int)(floorY - currentHeight),
                (int)WallWidth,
                (int)currentHeight);
            return column.Intersects(targetHitbox);
        }

        // Harmless until it is at least 40% up, so the eruption itself is the last beat of the tell.
        public override bool? CanDamage() => HeightFraction >= 0.4f ? null : false;

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            WitchkingBoss.ApplyFireHit(target);
        }

        // A column of rising flame the full current height, embers at the leading edge, smoke over the crest.
        private void SpawnWallDust(float floorY)
        {
            if (Main.dedServ)
            {
                return;
            }

            float currentHeight = WallHeight * HeightFraction;
            int flameCount = 2 + (int)(10f * HeightFraction);
            for (int i = 0; i < flameCount; i++)
            {
                Vector2 position = new Vector2(
                    Projectile.Center.X + Main.rand.NextFloat(-WallWidth / 2f, WallWidth / 2f),
                    floorY - Main.rand.NextFloat(0f, currentHeight));
                int dustType = Main.rand.NextBool(4) ? DustID.InfernoFork : DustID.Torch;
                Dust flame = Dust.NewDustPerfect(position, dustType, new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-4f, -1.5f)),
                    40, default, Main.rand.NextFloat(1.6f, 2.4f));
                flame.noGravity = true;
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 crest = new Vector2(Projectile.Center.X + Main.rand.NextFloat(-WallWidth / 2f, WallWidth / 2f), floorY - currentHeight);
                Dust smoke = Dust.NewDustPerfect(crest, DustID.Smoke, new Vector2(0f, -1.5f), 120, Color.Black, 1.8f);
                smoke.noGravity = true;
            }

            for (float y = 0f; y < currentHeight; y += 48f)
            {
                Lighting.AddLight(new Vector2(Projectile.Center.X, floorY - y), 0.9f, 0.45f, 0.1f);
            }
        }
    }
}
