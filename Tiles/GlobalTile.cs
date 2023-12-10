using Terraria.ModLoader;

namespace tsorcRevamp.Tiles
{
    public class tsorcGlobalTile : GlobalTile
    {
        public override void KillTile(int i, int j, int type, ref bool fail, ref bool effectOnly, ref bool noItem)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && !tsorcRevamp.DroppableTiles.Contains(type) && !tsorcRevamp.PlaceAllowedModTiles.Contains(type) && !tsorcRevamp.PlaceAllowed.Contains(type))
            {
                noItem = true;
            }
            base.KillTile(i, j, type, ref fail, ref effectOnly, ref noItem);
        }
    }
}