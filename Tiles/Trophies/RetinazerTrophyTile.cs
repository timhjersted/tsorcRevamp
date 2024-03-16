using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using tsorcRevamp.Items.Placeable.Trophies;

namespace tsorcRevamp.Tiles.Trophies
{
    // Simple 3x3 tile that can be placed on a wall
    public class RetinazerTrophyTile : TrophyTile
    {
        public override int ItemType => ModContent.ItemType<RetinazerTrophy>();
    }
}