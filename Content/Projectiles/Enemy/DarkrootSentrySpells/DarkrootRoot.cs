using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Content.Projectiles.Enemy.DarkrootSentrySpells
{
    /// <summary>
    /// One Rootwake root. Spawned by the server at the cast's lock tick, already standing on its chosen floor point
    /// (Projectile.Bottom), with a negative age = ticks until eruption. The whole lifecycle runs off that synced age:
    ///   age &lt; 0      harmless ground bud, grows over its first 12t on screen
    ///   0..4          visible emergence (harmless)
    ///   4..16         LIVE: a 24px-wide x 128px-high tapered root
    ///   16..28        harmless sinking / fade, then removed
    ///   1000..1012    cancelled before eruption: the bud fades, nothing ever erupts
    /// The root never moves and is never solid; it obstructs only by damage.
    /// </summary>
    public class DarkrootRoot : DarkrootHazard
    {
        public const int RootHeight = 128;
        public const int RootWidth = 24;
        const int EmergeTicks = 4;
        const int LiveEnd = 16;
        const int SinkEnd = 28;
        const int CancelledBud = 1000;
        const int BudFadeTicks = 12;
        const int BudGrowTicks = 12;

        float _budAgeAtSpawn;

        public override bool IsLive => Age >= EmergeTicks && Age < LiveEnd;

        /// <summary>A bud that has not erupted yet still counts as a threat for the owner's grace check.</summary>
        public override bool Threatening => Age < LiveEnd;

        public override void SetDefaults()
        {
            Projectile.width = RootWidth;
            Projectile.height = RootHeight;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.aiStyle = -1;
            Projectile.timeLeft = 600;
            Projectile.DamageType = Terraria.ModLoader.DamageClass.Magic;
        }

        public override void Disarm()
        {
            if (Age < 0f)
            {
                Age = CancelledBud;
                Projectile.netUpdate = true;
                return;
            }

            if (Age < LiveEnd)
            {
                Age = LiveEnd;
                Projectile.netUpdate = true;
            }
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            if (_budAgeAtSpawn == 0f)
            {
                _budAgeAtSpawn = Age < 0f ? Age : -1f;
            }

            DisarmIfOrphaned();

            // Terrain changed under a marked root: cancel that slot rather than move it to an unmarked spot.
            if (Main.netMode != NetmodeID.MultiplayerClient && Age < EmergeTicks)
            {
                Point support = (Projectile.Bottom + new Vector2(0f, 8f)).ToTileCoordinates();
                Tile supportTile = Framing.GetTileSafely(support);
                bool supported = supportTile.HasTile && (Main.tileSolid[supportTile.TileType] || Main.tileSolidTop[supportTile.TileType]);

                if (!supported)
                {
                    Disarm();
                }
            }

            Age++;

            bool budFadeDone = Age >= CancelledBud + BudFadeTicks;
            bool sinkDone = Age >= SinkEnd && Age < CancelledBud;
            if (budFadeDone || sinkDone)
            {
                Projectile.Kill();
                return;
            }

            if (Main.dedServ)
            {
                return;
            }

            Vector2 floor = Projectile.Bottom;

            // Inward motes gather on the bud over its final 20t before eruption.
            if (Age > -20f && Age < 0f && (int)Age % 3 == 0)
            {
                DarkrootWoodVFX.Motes(floor + new Vector2(0f, -6f), 2, 14f, true);
            }

            // Eruption: outward dirt and leaf fragments (about 16), a low crack.
            if (Age == 1f)
            {
                DarkrootWoodVFX.Motes(floor, 10, 10f, false, DustID.Dirt);
                DarkrootWoodVFX.Motes(floor + new Vector2(0f, -20f), 6, 10f, false, DustID.GrassBlades);
                SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.55f, Pitch = 0.2f }, floor);
            }

            // Sinking sheds about 12 bark fragments.
            if (Age == LiveEnd + 1)
            {
                DarkrootWoodVFX.Motes(floor + new Vector2(0f, -RootHeight * 0.5f), 12, 14f, false, DustID.WoodFurniture);
            }
        }

        /// <summary>
        /// The live root is a tapered column standing on Projectile.Bottom: three stacked capsules, 24px thick for
        /// the lower 44px, 18px to 88px, 10px to the 128px tip. The shader trunk sits inside this silhouette except
        /// for its wander (up to ~5px near the tip) and the side barbs, which are deliberately harmless decoration.
        /// </summary>
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 floor = Projectile.Bottom;
            Vector2 lowTop = floor + new Vector2(0f, -44f);
            Vector2 midTop = floor + new Vector2(0f, -88f);
            Vector2 tip = floor + new Vector2(0f, -RootHeight + 5f);

            bool lowHit = MeleeBladeCollision.SegmentIntersectsRect(floor + new Vector2(0f, -6f), lowTop, 24f, targetHitbox);
            bool midHit = MeleeBladeCollision.SegmentIntersectsRect(lowTop, midTop, 18f, targetHitbox);
            bool tipHit = MeleeBladeCollision.SegmentIntersectsRect(midTop, tip, 10f, targetHitbox);
            return lowHit || midHit || tipHit;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 floor = Projectile.Bottom;

            if (Age < 0f || Age >= CancelledBud)
            {
                // Ground bud, half sunk into the floor line. Grows over 12t from spawn, brightens as eruption nears.
                float sinceSpawn = Age - _budAgeAtSpawn;
                float growth = MathHelper.Clamp(sinceSpawn / BudGrowTicks, 0f, 1f);
                float nearEruption = 0f;
                float opacity = 0.7f;

                if (Age < 0f)
                {
                    nearEruption = MathHelper.Clamp(1f + Age / 40f, 0f, 1f);
                }
                else
                {
                    opacity *= MathHelper.Clamp(1f - (Age - CancelledBud) / BudFadeTicks, 0f, 1f);
                }

                DarkrootWoodVFX.Draw("DarkrootBud", floor + new Vector2(0f, -4f), new Vector2(40f, 40f), 0f, new Vector2(0.5f, 0.5f), growth, nearEruption, opacity);
                return false;
            }

            // Growth reveals bottom-to-top over the 4t emergence; sinking lowers it again over 12t.
            float rise = 1f;
            float fade = 1f;
            if (Age < EmergeTicks)
            {
                rise = Age / EmergeTicks;
            }
            else if (Age >= LiveEnd)
            {
                float sink = (Age - LiveEnd) / (SinkEnd - LiveEnd);
                rise = 1f - sink * 0.85f;
                fade = 1f - sink;
            }

            // 48x136 quad anchored at its bottom centre on the floor point; uv.y = 1 at the floor. The extra width
            // holds the trunk's wander and side barbs. SegmentRange.x is repurposed as this root's shape seed, taken
            // from the synced floor X so every client draws the same twist.
            float shapeSeed = floor.X / 97f % 1f;
            WoodQuad quad = new WoodQuad
            {
                Center = floor,
                Size = new Vector2(48f, 136f),
                Rotation = 0f,
                Origin = new Vector2(0.5f, 1f),
                SegmentRange = new Vector2(shapeSeed, 0f),
            };

            DarkrootWoodVFX.Draw("DarkrootRoot", new[] { quad }, rise, IsLive ? 1f : 0f, fade);
            return false;
        }
    }
}
