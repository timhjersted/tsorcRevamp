using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using tsorcRevamp.Items.Placeable.BuffStations;

namespace tsorcRevamp.Tiles.BuffStations
{
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
        public override void MouseOver(int i, int j)
        {
            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = ModContent.ItemType<NecromancyAltar>();
        }

        public override bool RightClick(int i, int j)
        {
            Player player = Main.LocalPlayer;
            Main.player[Main.myPlayer].AddBuff(BuffID.Bewitched, 1);
            Main.player[Main.myPlayer].AddBuff(BuffID.WarTable, 1);
            SoundEngine.PlaySound(SoundID.Item4, player.Center);
            return true;
        }
        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
            if (++frameCounter >= 8)
            {
                frameCounter = 0;
                frame = ++frame % 20;
            }
        }
    }
}