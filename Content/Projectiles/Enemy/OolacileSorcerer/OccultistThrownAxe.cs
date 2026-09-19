using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.OolacileSorcerer;

namespace tsorcRevamp.Projectiles.Enemy.OolacileSorcerer
{
    /// <summary>
    /// The Grand Occultist's physical axe during Destined Axe Cast.
    /// ai[0] = embedded flag, ai[1] = owning puppet NPC index, ai[2] = already hit a player.
    /// It deliberately remains in the floor after impact so PuppetNPC's shared retrieval phase can
    /// hide the held weapon, leap to this projectile, kill it, and restore the held axe.
    /// </summary>
    public class OccultistThrownAxe : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyOccultistAxe));

        private bool Embedded => Projectile.ai[0] == 1f;
        private int OwnerNpcIndex => (int)Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 54;
            Projectile.scale = 0.7f;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = 480;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => Embedded || Projectile.ai[2] == 1f ? false : null;

        public override void AI()
        {
            if (OwnerNpcIndex < 0 || OwnerNpcIndex >= Main.maxNPCs || !Main.npc[OwnerNpcIndex].active)
            {
                Projectile.Kill();
                return;
            }

            if (Embedded)
            {
                Projectile.velocity = Vector2.Zero;
                Projectile.hostile = false;
                if (!Main.dedServ && Main.rand.NextBool(5))
                {
                    Dust ember = Dust.NewDustPerfect(
                        Projectile.Bottom + new Vector2(Main.rand.NextFloat(-18f, 18f), -4f),
                        Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch,
                        new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(-1.3f, -0.4f)),
                        35, new Color(255, 220, 45), Main.rand.NextFloat(0.5f, 0.85f));
                    ember.noGravity = true;
                    Lighting.AddLight(Projectile.Center, 0.65f, 0.5f, 0.08f);
                }
                return;
            }

            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.18f, 15f);
            float spinDirection = Projectile.velocity.X < 0f ? -1f : 1f;
            Projectile.rotation += 0.3f * spinDirection;

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust trail = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.06f, 0.14f),
                    30, new Color(255, 220, 45), Main.rand.NextFloat(0.55f, 0.95f));
                trail.noGravity = true;
                Lighting.AddLight(Projectile.Center, 0.8f, 0.62f, 0.1f);
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            // The thrown axe is the ranged-start melee combo's one physical contact. It uses the
            // same three-hit/30-second Madness cadence as the held axe, and the projectile's ai[2]
            // gate below prevents repeated reports while it remains in flight.
            MadnessBuildup.Apply(target, 45, 30 * 60);
            // Keep flying so there is still a physical weapon for the boss to retrieve.
            Projectile.ai[2] = 1f;
            Projectile.netUpdate = true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            bool hitGroundWhileFalling = oldVelocity.Y > 0f && Projectile.velocity.Y != oldVelocity.Y;
            if (!hitGroundWhileFalling)
            {
                if (Projectile.velocity.X != oldVelocity.X)
                {
                    Projectile.velocity.X = -oldVelocity.X * 0.25f;
                }
                if (Projectile.velocity.Y != oldVelocity.Y)
                {
                    Projectile.velocity.Y = Math.Abs(oldVelocity.Y) * 0.25f;
                }
                return false;
            }

            Projectile.ai[0] = 1f;
            Projectile.hostile = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.rotation = oldVelocity.X < 0f ? -MathHelper.PiOver4 : MathHelper.PiOver4;
            Projectile.netUpdate = true;

            Vector2 groundZero = Projectile.Bottom - new Vector2(0f, 4f);
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.75f, Pitch = -0.45f }, groundZero);
                SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.85f, Pitch = -0.3f }, groundZero);
                UsefulFunctions.ScreenShake(groundZero, 5.5f, 16, distanceFalloff: 850f);
                SpawnImpactDust(groundZero, oldVelocity);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                DestinedDeathExplosion.TrySpawn(Projectile.GetSource_FromThis(), groundZero, 1.35f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    groundZero,
                    Vector2.Zero,
                    ModContent.ProjectileType<OccultistAxeImpactField>(),
                    25,
                    0f,
                    Main.myPlayer);
            }
            return false;
        }

        private static void SpawnImpactDust(Vector2 center, Vector2 impactVelocity)
        {
            // Body: fills the requested six-tile radius without turning individual particles into blocks.
            for (int i = 0; i < 64; i++)
            {
                // Upper hemisphere: this is a side-view ground eruption, not a top-down radial decal.
                Vector2 direction = Main.rand.NextFloat(MathHelper.Pi, MathHelper.TwoPi).ToRotationVector2();
                bool blood = i % 3 != 0;
                Dust body = Dust.NewDustPerfect(
                    center + direction * Main.rand.NextFloat(5f, 96f),
                    blood ? DustID.Blood : DustID.Wraith,
                    direction * Main.rand.NextFloat(1.4f, 6.2f)
                        + new Vector2(impactVelocity.X * 0.05f, -1.2f),
                    blood ? 80 : 155, default,
                    blood ? Main.rand.NextFloat(0.7f, 1.3f) : Main.rand.NextFloat(0.5f, 0.85f));
                body.noGravity = true;
                body.noLight = !blood;
            }

            // Fast foreground edge.
            for (int i = 0; i < 22; i++)
            {
                Vector2 direction = Main.rand.NextFloat(MathHelper.Pi, MathHelper.TwoPi).ToRotationVector2();
                Dust spark = Dust.NewDustPerfect(center + direction * Main.rand.NextFloat(2f, 18f),
                    DustID.Blood, direction * Main.rand.NextFloat(7f, 11.5f), 45, default,
                    Main.rand.NextFloat(0.4f, 0.75f));
                spark.noGravity = true;
            }

            // Slow black residue outlives the bright body.
            for (int i = 0; i < 16; i++)
            {
                Vector2 direction = Main.rand.NextFloat(MathHelper.Pi, MathHelper.TwoPi).ToRotationVector2();
                Dust smoke = Dust.NewDustPerfect(center + direction * Main.rand.NextFloat(8f, 76f),
                    DustID.Wraith, direction * Main.rand.NextFloat(0.5f, 1.8f) + new Vector2(0f, -0.8f),
                    175, default, Main.rand.NextFloat(0.45f, 0.7f));
                smoke.noGravity = true;
                smoke.noLight = true;
                smoke.fadeIn = Main.rand.NextFloat(1.15f, 1.5f);
            }
        }
    }

    /// <summary>
    /// Server-authored, center-out field builder for the axe impact. Thirteen one-tile modules cover
    /// exactly six tiles on either side of the planted axe and reuse the flask attack's proven flame.
    /// </summary>
    public class OccultistAxeImpactField : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private static readonly int[] ColumnOffsets =
        {
            0, -1, 1, -2, 2, -3, 3, -4, 4, -5, 5, -6, 6,
        };

        private const int ColumnInterval = 2;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = ColumnOffsets.Length * ColumnInterval + 2;
            Projectile.penetrate = -1;
            Projectile.hide = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;
        public override bool PreDraw(ref Color lightColor) => false;

        public override void AI()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int elapsed = ColumnOffsets.Length * ColumnInterval + 2 - Projectile.timeLeft;
            if (elapsed < 0 || elapsed % ColumnInterval != 0)
            {
                return;
            }

            int columnIndex = elapsed / ColumnInterval;
            if (columnIndex >= ColumnOffsets.Length)
            {
                return;
            }

            int tileX = (int)(Projectile.Center.X / 16f) + ColumnOffsets[columnIndex];
            int startY = (int)(Projectile.Center.Y / 16f);
            if (!TryFindGroundColumn(tileX, startY, out int groundY))
            {
                return;
            }

            int heightTiles = Main.rand.Next(1, 4);
            Vector2 spawnCenter = new Vector2(tileX * 16f + 8f, groundY * 16f - 8f);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnCenter,
                Vector2.Zero,
                ModContent.ProjectileType<OccultistDestinedDeathFlameColumn>(),
                Projectile.damage,
                0f,
                Main.myPlayer,
                heightTiles,
                columnIndex);
        }

        private static bool TryFindGroundColumn(int x, int impactY, out int groundY)
        {
            groundY = impactY;
            if (x < 5 || x >= Main.maxTilesX - 5)
            {
                return false;
            }

            for (int y = Math.Max(5, impactY - 5); y <= impactY + 18 && y < Main.maxTilesY - 5; y++)
            {
                Tile tile = Framing.GetTileSafely(x, y);
                Tile above = Framing.GetTileSafely(x, y - 1);
                bool standable = tile.HasTile && Main.tileSolid[tile.TileType];
                bool blockedAbove = above.HasTile && Main.tileSolid[above.TileType]
                    && !Main.tileSolidTop[above.TileType];
                if (standable && !blockedAbove)
                {
                    groundY = y;
                    return true;
                }
            }
            return false;
        }
    }
}
