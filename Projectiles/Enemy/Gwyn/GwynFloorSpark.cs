using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///Stage 3 of Gwyn's Spear of the First Sun: electricity racing along the floor away from the
    ///bolt strike. Invisible projectile sold by crackling dust; hugs the terrain (steps up/down
    ///small ledges), dies at tall walls and pits. Jump it. ai[0] = direction (-1/1), ai[1] = tiles
    ///of reach.
    ///</summary>
    class GwynFloorSpark : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float SparkSpeed = 8f;

        int Direction => (int)Projectile.ai[0] >= 0 ? 1 : -1;
        int ReachTiles => (int)Projectile.ai[1] > 0 ? (int)Projectile.ai[1] : 12;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 26;
            Projectile.height = 34;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = 120;
            Projectile.light = 0.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = (int)(ReachTiles * 16f / SparkSpeed) + 4;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            Projectile.position.X += Direction * SparkSpeed;
            if (!SnapToGround())
            {
                Projectile.Kill();
                return;
            }

            //Crackling electricity dancing along the ground
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = new Vector2(Projectile.position.X + Main.rand.NextFloat(Projectile.width), Projectile.position.Y + Projectile.height - 6f);
                int type = Main.rand.NextBool() ? DustID.GoldFlame : DustID.Electric;
                int dust = Dust.NewDust(pos, 4, 4, type, Direction * 1f, Main.rand.NextFloat(-4f, -1f), 40, default, Main.rand.NextFloat(1.3f, 1.9f));
                Main.dust[dust].noGravity = true;
            }
            if (Main.rand.NextBool(2))
            {
                int arc = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Electric, 0f, -2f, 0, default, 1.2f);
                Main.dust[arc].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.7f, 0.6f, 0.25f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 4 * 60);
        }

        ///<summary>Aligns the spark bottom with the ground; false = blocked by a tall wall or a pit.</summary>
        bool SnapToGround()
        {
            int tileX = (int)((Projectile.Center.X + Direction * Projectile.width / 2f) / 16f);
            int tileY = (int)((Projectile.position.Y + Projectile.height - 8f) / 16f);
            if (tileX < 5 || tileX > Main.maxTilesX - 5 || tileY < 5 || tileY > Main.maxTilesY - 10)
            {
                return false;
            }
            int climb = 0;
            while (IsSolid(tileX, tileY) && climb <= 3)
            {
                tileY--;
                climb++;
            }
            if (climb > 3)
            {
                return false;
            }
            int drop = 0;
            while (!IsSolid(tileX, tileY + 1) && drop <= 5)
            {
                tileY++;
                drop++;
            }
            if (drop > 5)
            {
                return false;
            }
            Projectile.position.Y = (tileY + 1) * 16f - Projectile.height;
            return true;
        }

        static bool IsSolid(int x, int y)
        {
            Tile tile = Main.tile[x, y];
            return tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType];
        }
    }
}
