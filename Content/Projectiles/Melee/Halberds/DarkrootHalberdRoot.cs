using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.DarkrootSentrySpells;
using tsorcRevamp.NPCs.Invaders;

namespace tsorcRevamp.Content.Projectiles.Melee.Halberds
{
    /// <summary>
    /// The Darkroot Halberd spin slam's root: erupts from the floor under the strongest enemy in range (or under
    /// the slam itself), drawn with the Darkroot Sentry's Rootwake root shader at RootScale size. Spawned by the
    /// owner already standing on its floor point (Projectile.Bottom); the clock is timeLeft, so every client plays
    /// the same phase from the spawn packet. The whole root physically moves up out of the floor and back down;
    /// drawing is clipped at the floor line, so the buried part never shows, even through gaps in the floor.
    ///   0..RiseTicks          erupts (fast, decelerating); hits once it has cleared the floor by ~1/5
    ///   ..+HoldTicks          stands; hits each enemy in it every HitInterval ticks
    ///   ..+SinkTicks          drops back down fast (accelerating), harmless; fades over the last few ticks
    /// </summary>
    public class DarkrootHalberdRoot : ModProjectile
    {
        public const float RootScale = 1.3f;
        public const int SearchRangeTiles = 20;
        public const float SpawnHalfHeight = DarkrootRoot.RootHeight * RootScale / 2f; // spawn at floor minus this so Bottom sits on the floor
        public const int RiseTicks = 40;
        public const int HoldTicks = 120;
        public const int HitInterval = 30;
        const int SinkTicks = 12;
        const int SinkFadeTicks = 5;
        const int Lifetime = RiseTicks + HoldTicks + SinkTicks;
        const float LiveFromRise = 0.2f; // share of the root above ground before it can hit
        const int FloorSearchTiles = 16;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        int Age => Lifetime - Projectile.timeLeft;
        float RootHeight => DarkrootRoot.RootHeight * RootScale;

        public override void SetDefaults()
        {
            Projectile.width = (int)(DarkrootRoot.RootWidth * RootScale);
            Projectile.height = (int)(DarkrootRoot.RootHeight * RootScale);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = HitInterval;
        }

        /// <summary>
        /// How far the root's base sits below the floor this tick, in px: RootHeight fully buried, 0 fully out.
        /// Rise eases out (bursts up, settles); the sink eases in (lets go, then drops).
        /// </summary>
        float BuriedDepth
        {
            get
            {
                if (Age < RiseTicks)
                {
                    float rise = Age / (float)RiseTicks;
                    float eased = 1f - (1f - rise) * (1f - rise) * (1f - rise);
                    return RootHeight * (1f - eased);
                }

                int sinkAge = Age - RiseTicks - HoldTicks;
                if (sinkAge <= 0)
                {
                    return 0f;
                }

                float sink = MathHelper.Clamp(sinkAge / (float)SinkTicks, 0f, 1f);
                return RootHeight * sink * sink;
            }
        }

        bool Live
        {
            get
            {
                bool clearedFloor = BuriedDepth <= RootHeight * (1f - LiveFromRise);
                bool beforeSink = Age < RiseTicks + HoldTicks;
                return clearedFloor && beforeSink;
            }
        }

        /// <summary>
        /// Owner only. Picks the eruption floor point: under the highest-current-HP enemy within SearchRangeTiles of
        /// the player, otherwise under the slam point. A target with no floor beneath it gets the root at its feet.
        /// </summary>
        public static Vector2 ChooseEruptionPoint(Player player, Vector2 slamPoint)
        {
            float range = SearchRangeTiles * 16f;
            NPC strongest = null;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                {
                    continue;
                }

                float distance = Vector2.Distance(player.Center, npc.Center);
                if (distance > range)
                {
                    continue;
                }

                if (strongest == null || npc.life > strongest.life)
                {
                    strongest = npc;
                }
            }

            if (strongest != null)
            {
                Vector2? targetFloor = FindFloor(strongest.Bottom - new Vector2(0f, 8f));
                if (targetFloor.HasValue)
                {
                    return targetFloor.Value;
                }

                return strongest.Bottom;
            }

            Vector2? slamFloor = FindFloor(slamPoint - new Vector2(0f, 16f));
            if (slamFloor.HasValue)
            {
                return slamFloor.Value;
            }

            return slamPoint;
        }

        static Vector2? FindFloor(Vector2 start)
        {
            int tileX = (int)(start.X / 16f);
            int tileY = (int)(start.Y / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5)
            {
                return null;
            }

            for (int depth = 0; depth <= FloorSearchTiles; depth++)
            {
                int y = tileY + depth;
                if (y >= Main.maxTilesY - 5)
                {
                    break;
                }

                Tile tile = Main.tile[tileX, y];
                bool solid = tile.HasTile && !tile.IsActuated && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                if (solid)
                {
                    return new Vector2(start.X, y * 16f);
                }
            }

            return null;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanCutTiles() => false;

        public override bool? CanDamage()
        {
            if (!Live)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            Vector2 floor = Projectile.Bottom;
            Lighting.AddLight(floor - new Vector2(0f, RootHeight * 0.4f), 0.15f, 0.3f, 0.12f);

            if (Main.dedServ)
            {
                return;
            }

            // Eruption: a crack and a burst of dirt and leaves, then soil keeps spraying from the hole while it rises.
            if (Age == 1)
            {
                DarkrootWoodVFX.Motes(floor, 14, 14f, false, DustID.Dirt);
                DarkrootWoodVFX.Motes(floor + new Vector2(0f, -24f), 8, 12f, false, DustID.GrassBlades);
                SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.7f, Pitch = 0.05f }, floor);
                SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.8f, Pitch = -0.4f }, floor);
            }

            bool rising = Age < RiseTicks;
            if (rising && Age % 3 == 0)
            {
                DarkrootWoodVFX.Motes(floor, 3, 12f, false, DustID.Dirt);
            }

            // Going back down: a rumble and dirt kicked out of the hole as it drops.
            int sinkAge = Age - RiseTicks - HoldTicks;
            if (sinkAge == 0)
            {
                SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.6f, Pitch = -0.6f }, floor);
                DarkrootWoodVFX.Motes(floor + new Vector2(0f, -RootHeight * 0.5f), 12, 16f, false, DustID.WoodFurniture);
            }

            if (sinkAge >= 0 && sinkAge < SinkTicks && sinkAge % 2 == 0)
            {
                DarkrootWoodVFX.Motes(floor, 3, 12f, false, DustID.Dirt);
            }
        }

        /// <summary>
        /// Same tapered silhouette as the Sentry's root, scaled: three stacked capsules, 24 / 18 / 10px thick over the
        /// lower, middle and top thirds (times RootScale), shifted down by BuriedDepth. Only the part above the floor
        /// can hit: each capsule is clamped to the floor line.
        /// </summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 floor = Projectile.Bottom;
            Vector2 rootBase = floor + new Vector2(0f, BuriedDepth);
            Vector2 lowStart = AboveFloor(rootBase + new Vector2(0f, -6f), floor);
            Vector2 lowTop = AboveFloor(rootBase + new Vector2(0f, -44f * RootScale), floor);
            Vector2 midTop = AboveFloor(rootBase + new Vector2(0f, -88f * RootScale), floor);
            Vector2 tip = AboveFloor(rootBase + new Vector2(0f, -RootHeight + 5f), floor);

            bool lowHit = MeleeBladeCollision.SegmentIntersectsRect(lowStart, lowTop, 24f * RootScale, targetHitbox);
            bool midHit = MeleeBladeCollision.SegmentIntersectsRect(lowTop, midTop, 18f * RootScale, targetHitbox);
            bool tipHit = MeleeBladeCollision.SegmentIntersectsRect(midTop, tip, 10f * RootScale, targetHitbox);
            return lowHit || midHit || tipHit;
        }

        static Vector2 AboveFloor(Vector2 point, Vector2 floor)
        {
            return new Vector2(point.X, System.Math.Min(point.Y, floor.Y));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 floor = Projectile.Bottom;

            // The whole, fully grown root moves; the floor-line clip hides whatever is still below ground.
            // It fades only over the last SinkFadeTicks, when almost nothing of it is still visible.
            float fade = MathHelper.Clamp(Projectile.timeLeft / (float)SinkFadeTicks, 0f, 1f);
            float liveFlag = 0f;
            if (Live)
            {
                liveFlag = 1f;
            }

            // Shape seed from the synced floor X so every client draws the same twist.
            float shapeSeed = floor.X / 97f % 1f;
            WoodQuad quad = new WoodQuad
            {
                Center = floor + new Vector2(0f, BuriedDepth),
                Size = new Vector2(48f, 136f) * RootScale,
                Rotation = 0f,
                Origin = new Vector2(0.5f, 1f),
                SegmentRange = new Vector2(shapeSeed, 0f),
            };

            DarkrootWoodVFX.Draw("DarkrootRoot", new[] { quad }, 1f, liveFlag, fade, floor.Y);
            return false;
        }
    }
}
