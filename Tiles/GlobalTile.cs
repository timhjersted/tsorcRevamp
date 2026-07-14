using Microsoft.Xna.Framework.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.VesselOfSouls;

namespace tsorcRevamp.Tiles
{
    public class tsorcGlobalTile : GlobalTile
    {
        public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
        {
            // Keep platforms as readable arena geometry. This is render-only: collision, framing,
            // world saves, and multiplayer tile state are not changed.
            if (VesselOfSoulsFadeSystem.WorldHidden && !TileID.Sets.Platforms[type])
                return false;

            return base.PreDraw(i, j, type, spriteBatch);
        }

        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !tsorcRevamp.DroppableTiles.Contains(type) && !tsorcRevamp.PlaceAllowedModTiles.Contains(type) && !tsorcRevamp.PlaceAllowed.Contains(type))
            {
                noItem = true;
            }
            base.KillTile(i, j, type, ref fail, ref effectOnly, ref noItem);
        }
    }

    public class VesselVoidGlobalWall : GlobalWall
    {
        public override bool PreDraw(int i, int j, int type, SpriteBatch spriteBatch)
        {
            return !VesselOfSoulsFadeSystem.WorldHidden;
        }
    }
}
