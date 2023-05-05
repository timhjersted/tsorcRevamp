using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using tsorcRevamp.Items.Placeable.BuffStations;

namespace tsorcRevamp.Tiles.BuffStations
{
    // Simple 3x3 tile that can be placed on a wall
    public class NecromancyAltarTile : ModTile
    {
        public override void SetStaticDefaults()
        {
            Main.tileFrameImportant[Type] = true;
            Main.tileLavaDeath[Type] = false;

            TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
            TileObjectData.addTile(Type);

            AddMapEntry(new Color(169, 169, 169), Language.GetText("Necromancy Altar"));
            DustType = DustID.Bone;
        }

        public override void KillMultiTile(int i, int j, int frameX, int frameY)
        {
            Item.NewItem(new EntitySource_TileBreak(i, j), i * 16, j * 16, 32, 32, ModContent.ItemType<NecromancyAltar>());
        }

        public override bool RightClick(int i, int j)
        {
            Main.player[Main.myPlayer].AddBuff(BuffID.Bewitched, 1);
            Main.player[Main.myPlayer].AddBuff(BuffID.WarTable, 1);
            return true;
        }
    }
}