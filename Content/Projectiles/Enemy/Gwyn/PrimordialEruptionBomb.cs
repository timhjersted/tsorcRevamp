using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Gwyn
{
    /// <summary>
    /// Prime Gwyn's Eruption: a lava bomb thrown up by the primordial volcanoes. The projectile sits at its
    /// landing point the whole time, so every machine agrees where it lands. A dust circle marks the exact
    /// blast for 60t, the falling bomb is only drawn on its way down, and it deals damage for 8t on impact.
    /// Sprite: GwynDescentMeteor (4 frames, drawn rotated 180 degrees so its head leads downward).
    /// </summary>
    class PrimordialEruptionBomb : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(GwynDescentMeteor));

        public const float BlastCenterHeight = 24f; // blast centre above the floor line, so it covers standing players
        const float BlastRadius = 56f;
        const int MarkTicks = 60;
        const int FallTicks = 24;
        const int BlastTicks = 8;
        const float FallHeight = 900f;
        const float SpriteScale = 0.25f;
        const int ImpactDustCount = 40;
        const int FloorSearchTiles = 45;
        const int FloorSearchStartAboveTiles = 6;

        int Timer => (int)Projectile.localAI[0];
        bool Falling => Timer >= MarkTicks - FallTicks && Timer < MarkTicks;
        bool Blasting => Timer >= MarkTicks;

        Vector2 FallingBombPosition
        {
            get
            {
                // Squared progress accelerates the drop, and the bomb reaches the mark exactly at MarkTicks.
                float progress = (Timer - (MarkTicks - FallTicks)) / (float)FallTicks;
                float remainingHeight = FallHeight * (1f - progress * progress);
                return Projectile.Center - new Vector2(0f, remainingHeight);
            }
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = (int)(BlastRadius * 2f);
            Projectile.height = (int)(BlastRadius * 2f);
            Projectile.tileCollide = false; // deliberate: it lands at a floor point chosen when it was spawned
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = MarkTicks + BlastTicks;
        }

        public override bool? CanDamage() => Blasting;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float closestX = MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right);
            float closestY = MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom);
            float distance = Vector2.Distance(Projectile.Center, new Vector2(closestX, closestY));
            return distance <= BlastRadius;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;

            if (++Projectile.frameCounter >= 5)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }

            if (Main.dedServ)
            {
                return;
            }

            if (Timer == MarkTicks)
            {
                PlayImpact();
            }
            else if (!Blasting)
            {
                SpawnMarkDust();
            }

            if (Falling)
            {
                SpawnFallDust();
            }
        }

        void SpawnMarkDust()
        {
            // The ring radius equals the damage radius, and it brightens as the landing approaches.
            float progress = Timer / (float)MarkTicks;
            int ringDust = 2 + (int)(progress * 4f);
            for (int i = 0; i < ringDust; i++)
            {
                Vector2 edge = Main.rand.NextVector2CircularEdge(BlastRadius, BlastRadius);
                int dustType = DustID.Torch;
                if (Main.rand.NextBool(3))
                {
                    dustType = DustID.GoldFlame;
                }
                Dust ember = Dust.NewDustPerfect(Projectile.Center + edge, dustType, Vector2.Zero, 60, default,
                    MathHelper.Lerp(0.9f, 1.5f, progress));
                ember.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.8f * progress, 0.3f * progress, 0.05f);
        }

        void SpawnFallDust()
        {
            Vector2 bombPosition = FallingBombPosition;
            for (int i = 0; i < 3; i++)
            {
                Vector2 position = bombPosition + Main.rand.NextVector2Circular(14f, 14f);
                Dust trail = Dust.NewDustPerfect(position, DustID.Torch, new Vector2(0f, -2f), 50, default, 1.5f);
                trail.noGravity = true;
            }
            Lighting.AddLight(bombPosition, 1f, 0.5f, 0.12f);
        }

        void PlayImpact()
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.8f, Pitch = -0.2f }, Projectile.Center);
            UsefulFunctions.ScreenShake(Projectile.Center, 5f, 10);
            for (int i = 0; i < ImpactDustCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(7f, 7f);
                int dustType = DustID.Torch;
                if (Main.rand.NextBool())
                {
                    dustType = DustID.GoldFlame;
                }
                Dust burst = Dust.NewDustPerfect(Projectile.Center, dustType, velocity, 40, default, 1.8f);
                // A third keeps gravity and falls as embers while the blast fades.
                burst.noGravity = i % 3 != 0;
            }
            Lighting.AddLight(Projectile.Center, 1.6f, 0.9f, 0.3f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 5 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!Falling)
            {
                return false; // the mark and blast are dust; only the falling bomb is a sprite
            }

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = new Vector2(texture.Width * 0.5f, frameHeight * 0.5f);
            Vector2 drawPosition = FallingBombPosition - Main.screenPosition;
            Main.EntitySpriteDraw(texture, drawPosition, frame, Color.White, MathHelper.Pi, origin,
                SpriteScale, SpriteEffects.None, 0);
            return false;
        }

        /// <summary>World Y of the first floor at or below startY, or -1 if none is close enough. Searching
        /// from slightly above lets a bomb land on a floor at the player's feet.</summary>
        public static float FindFloorY(float worldX, float startY)
        {
            int tileX = (int)(worldX / 16f);
            int firstTileY = (int)(startY / 16f) - FloorSearchStartAboveTiles;
            if (tileX < 5 || tileX > Main.maxTilesX - 5)
            {
                return -1f;
            }

            int lastTileY = Math.Min(firstTileY + FloorSearchTiles, Main.maxTilesY - 5);
            for (int tileY = Math.Max(5, firstTileY); tileY <= lastTileY; tileY++)
            {
                Tile tile = Main.tile[tileX, tileY];
                Tile tileAbove = Main.tile[tileX, tileY - 1];
                bool solid = tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
                bool openAbove = !tileAbove.HasTile || tileAbove.IsActuated || !Main.tileSolid[tileAbove.TileType];
                if (solid && openAbove)
                {
                    return tileY * 16f;
                }
            }
            return -1f;
        }
    }
}
