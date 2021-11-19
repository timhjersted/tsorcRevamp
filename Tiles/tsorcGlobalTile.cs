using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Tiles
{
    public class tsorcGlobalTile : GlobalTile
    {
        public override bool CanExplode(int i, int j, int type)
        {
            if (Main.tile[i, j - 1].type == ModContent.TileType<Tiles.BonfireCheckpoint>())
            {
                return false;
            }
            return true;
        }

        public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
        {
            if (Main.tile[i, j - 1].type == ModContent.TileType<Tiles.BonfireCheckpoint>())
            {
                return false;
            }
            return true;
        }
    }
}
