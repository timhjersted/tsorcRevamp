using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Weapons.Enemy;

namespace tsorcRevamp.Content.Projectiles.Enemy.OolacileSorcerer
{
    /// <summary>
    /// Grand Occultist-only flask. It mirrors Studded Leather Warrior's ballistic flask lifecycle,
    /// but owns a separate Destined Death field so EnemyFireFlask and all existing Destined Death
    /// projectiles remain unchanged.
    /// </summary>
    public class OccultistDestinedDeathFlask : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyDestinedDeathFlask));

        private const float Gravity = 0.18f;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 300;
            Projectile.penetrate = 1;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.knockBack = 3f;
            Projectile.light = 0.3f;
        }

        public override void AI()
        {
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + Gravity, 16f);
            Projectile.rotation += Projectile.velocity.X * 0.075f;

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                bool blood = Main.rand.NextBool(2);
                Dust trail = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height,
                    blood ? DustID.Blood : DustID.Wraith, 0f, 0f, blood ? 90 : 150, default,
                    Main.rand.NextFloat(0.55f, 0.95f));
                trail.noGravity = true;
                trail.noLight = !blood;
                trail.velocity = Projectile.velocity * -0.08f + new Vector2(0f, -0.5f);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<DestinedDeath>(), 12 * 60);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.9f, Pitch = -0.35f }, Projectile.Center);
                SpawnImpactDust();
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 groundZero = FindGroundZero(Projectile.Center);
            DestinedDeathExplosion.TrySpawn(Projectile.GetSource_Death(), groundZero, 1.25f);

            int field = Projectile.NewProjectile(Projectile.GetSource_Death(), groundZero, Vector2.Zero,
                ModContent.ProjectileType<OccultistDestinedDeathField>(), Projectile.damage, 0f, Main.myPlayer);
            if (field < Main.maxProjectiles && Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncProjectile, number: field);
            }
        }

        private void SpawnImpactDust()
        {
            // Blood is the red body and Wraith is the black billowing layer requested for this attack.
            // Scales remain below 1.62 after vanilla's hidden 1.2x jitter.
            for (int i = 0; i < 48; i++)
            {
                bool blood = i % 3 != 0;
                Vector2 velocity = Main.rand.NextVector2Circular(5.5f, 4.2f);
                Dust mote = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 8f),
                    blood ? DustID.Blood : DustID.Wraith, velocity, blood ? 80 : 155, default,
                    blood ? Main.rand.NextFloat(0.75f, 1.35f) : Main.rand.NextFloat(0.5f, 0.85f));
                mote.noGravity = true;
                mote.noLight = !blood;
                if (!blood)
                {
                    mote.fadeIn = Main.rand.NextFloat(1.15f, 1.45f);
                }
            }

            for (int i = 0; i < 16; i++)
            {
                Dust spark = Dust.NewDustPerfect(Projectile.Center, DustID.Blood,
                    Main.rand.NextVector2CircularEdge(8.5f, 6.5f), 45, default,
                    Main.rand.NextFloat(0.4f, 0.7f));
                spark.noGravity = true;
            }
        }

        private static Vector2 FindGroundZero(Vector2 impact)
        {
            int tileX = (int)(impact.X / 16f);
            int startY = (int)(impact.Y / 16f) - 4;
            for (int y = startY; y <= startY + 28 && y < Main.maxTilesY - 5; y++)
            {
                if (tileX < 5 || tileX >= Main.maxTilesX - 5 || y < 5)
                {
                    continue;
                }

                Tile tile = Framing.GetTileSafely(tileX, y);
                Tile above = Framing.GetTileSafely(tileX, y - 1);
                bool standable = tile.HasTile && Main.tileSolid[tile.TileType];
                bool blockedAbove = above.HasTile && Main.tileSolid[above.TileType]
                    && !Main.tileSolidTop[above.TileType];
                if (standable && !blockedAbove)
                {
                    return new Vector2(tileX * 16f + 8f, y * 16f);
                }
            }
            return impact;
        }
    }

    /// <summary>
    /// Server-only field builder. Fifteen one-tile columns are dealt center-outward across a
    /// twenty-tile span instead of stamping the whole hazard in one frame.
    /// </summary>
    public class OccultistDestinedDeathField : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        private static readonly int[] ColumnOffsets =
        {
            0, -1, 1, -3, 3, -4, 4, -6, 6, -7, 7, -9, 9, -10, 10,
        };

        private const int ColumnInterval = 2;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = false;
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
            int flame = Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnCenter, Vector2.Zero,
                ModContent.ProjectileType<OccultistDestinedDeathFlameColumn>(), Projectile.damage, 0f,
                Main.myPlayer, heightTiles, columnIndex);
            if (flame < Main.maxProjectiles && Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncProjectile, number: flame);
            }
        }

        private static bool TryFindGroundColumn(int x, int impactY, out int groundY)
        {
            groundY = impactY;
            if (x < 5 || x >= Main.maxTilesX - 5)
            {
                return false;
            }

            for (int y = Math.Max(5, impactY - 5); y <= impactY + 20 && y < Main.maxTilesY - 5; y++)
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

    /// <summary>
    /// One tile-wide, one-to-three tile-tall persistent hazard. It reuses the five-frame
    /// DestinedDeathBlaze art, but owns its seven-second lifetime and collision independently.
    /// </summary>
    public class OccultistDestinedDeathFlameColumn : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DestinedDeathBlaze));

        private const int FrameCount = 5;
        private const int TicksPerFrame = 6;
        private const int Lifetime = 7 * 60;
        private int HeightTiles => Math.Clamp((int)Projectile.ai[0], 1, 3);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 16;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.penetrate = -1;
            Projectile.netImportant = true;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 bottom = Projectile.Bottom;
            Projectile.height = HeightTiles * 16;
            Projectile.Bottom = bottom;
            Projectile.frame = Math.Abs(Projectile.identity * 3 + (int)Projectile.ai[1]) % FrameCount;
            Projectile.frameCounter = Math.Abs(Projectile.identity + (int)Projectile.ai[1]) % TicksPerFrame;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= TicksPerFrame)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % FrameCount;
            }

            if (!Main.dedServ && Main.GameUpdateCount % 3 == (ulong)(Projectile.identity % 3))
            {
                bool blood = (Main.GameUpdateCount + (ulong)Projectile.identity) % 3 != 0;
                Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-6f, 6f), -4f);
                Dust flame = Dust.NewDustPerfect(position, blood ? DustID.Blood : DustID.Wraith,
                    new Vector2(Main.rand.NextFloat(-0.45f, 0.45f),
                        Main.rand.NextFloat(-2.2f, -0.8f) * HeightTiles),
                    blood ? 80 : 155, default, blood ? Main.rand.NextFloat(0.55f, 1.05f)
                        : Main.rand.NextFloat(0.45f, 0.75f));
                flame.noGravity = true;
                flame.noLight = !blood;
                if (!blood)
                {
                    flame.fadeIn = Main.rand.NextFloat(1.05f, 1.35f);
                }
            }

            Lighting.AddLight(Projectile.Center, 0.32f, 0.015f, 0.02f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<DestinedDeath>(), 12 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle frame = new(0, Projectile.frame * frameHeight, texture.Width, frameHeight);

            int age = Lifetime - Projectile.timeLeft;
            float lifetimeFade = MathHelper.Clamp(age / 12f, 0f, 1f)
                * MathHelper.Clamp(Projectile.timeLeft / 24f, 0f, 1f);
            DrawRisingTongue(texture, frame, age, 0, lifetimeFade, -1);
            DrawRisingTongue(texture, frame, age, 36, lifetimeFade * 0.8f, 1);
            return false;
        }

        private void DrawRisingTongue(Texture2D texture, Rectangle frame, int age, int phaseOffset,
            float lifetimeFade, int spinDirection)
        {
            const int RiseCycle = 72;
            float cycle = ((age + phaseOffset + Projectile.identity * 7) % RiseCycle) / (float)RiseCycle;
            float tongueFade = MathF.Sin(cycle * MathHelper.Pi) * lifetimeFade;
            float height = Projectile.height;
            float visualHeight = height * 0.65f;
            float rise = cycle * height * 0.35f;
            float rotation = spinDirection * MathHelper.Lerp(-0.12f, 0.42f, cycle)
                + MathF.Sin(cycle * MathHelper.TwoPi) * 0.08f;
            Vector2 drawPosition = Projectile.Bottom - Main.screenPosition
                + new Vector2(spinDirection * 2.5f, -rise);
            Vector2 scale = new Vector2(0.22f, visualHeight / frame.Height);
            SpriteEffects effects = spinDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.White * tongueFade, rotation,
                new Vector2(frame.Width * 0.5f, frame.Height), scale, effects, 0f);
        }
    }
}
