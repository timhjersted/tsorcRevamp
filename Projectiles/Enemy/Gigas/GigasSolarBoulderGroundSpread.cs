using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Expands either Gigas ground-fire field from its impact tile. The controller has no hitbox;
    /// each terrain-aligned module owns its normal damage and draw. ai[0] = variant, ai[1] = span,
    /// ai[2] = ground-search depth.
    /// </summary>
    class GigasSolarBoulderGroundSpread : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        int Variant => (int)Projectile.ai[0];
        int SpanTiles => Projectile.ai[1] > 0f ? (int)Projectile.ai[1] : GigasConsecratedGround.BoulderSpanTiles;
        int MaxTilesDown => Projectile.ai[2] > 0f ? (int)Projectile.ai[2] : 10;

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = GigasConsecratedGround.BoulderSpanTiles + 2;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = SpanTiles + 2;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            int step = (int)Projectile.localAI[0]++;
            if (step >= SpanTiles || Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
            {
                return;
            }

            // 0, -1, +1, -2, +2 ... grows visibly from the impact rather than stamping the whole
            // terrain-following field at once.
            int offset = step == 0 ? 0 : (step % 2 == 1 ? -(step + 1) / 2 : step / 2);
            int impactTileX = (int)(Projectile.Center.X / 16f);
            int impactTileY = (int)(Projectile.Center.Y / 16f);
            int groundTileY = FindGroundTileY(impactTileX + offset, impactTileY - 3, MaxTilesDown);
            if (groundTileY < 0)
            {
                return;
            }

            float height = GigasConsecratedGround.HeightForVariant(Variant);
            float columnScale = GigasConsecratedGround.ColumnScaleForField(offset, SpanTiles, Variant);
            Vector2 patchCenter = new Vector2((impactTileX + offset) * 16f + 8f, groundTileY * 16f - height * 0.5f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), patchCenter, Vector2.Zero,
                ModContent.ProjectileType<GigasConsecratedGround>(), Projectile.damage, 0f, Main.myPlayer,
                0f, Variant, columnScale);
        }

        static int FindGroundTileY(int tileX, int startTileY, int maxTilesDown)
        {
            if (tileX < 5 || tileX > Main.maxTilesX - 5)
            {
                return -1;
            }
            for (int distance = 0; distance <= maxTilesDown; distance++)
            {
                int tileY = startTileY + distance;
                if (tileY < 5 || tileY >= Main.maxTilesY - 5)
                {
                    continue;
                }
                Tile tile = Main.tile[tileX, tileY];
                if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType])
                {
                    return tileY;
                }
            }
            return -1;
        }
    }
}
