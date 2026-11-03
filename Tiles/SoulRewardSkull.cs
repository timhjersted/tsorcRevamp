using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.ConsumableSoul;

namespace tsorcRevamp.Tiles
{
    // These skulls use the existing art but carry their reward in the tile type, not a world coordinate.
    public abstract class SoulRewardSkull : SoulSkullR
    {
        public override string Texture => "tsorcRevamp/Tiles/SoulSkullR";

        protected abstract int RewardItemType { get; }

        public override void MouseOver(int i, int j)
        {
            if (Main.tile[i, j].TileFrameX >= 36)
                return;

            Player player = Main.LocalPlayer;
            player.noThrow = 2;
            player.cursorItemIconEnabled = true;
            player.cursorItemIconID = RewardItemType;
        }

        public override void MouseOverFar(int i, int j) => MouseOver(i, j);

        public override bool RightClick(int i, int j)
        {
            if (Main.tile[i, j].TileFrameX >= 36)
                return false;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                ModPacket packet = Mod.GetPacket();
                packet.Write(tsorcPacketID.OpenSoulRewardSkull);
                packet.Write((short)i);
                packet.Write((short)j);
                packet.Send();
            }
            else if (!TryOpen(i, j, Main.myPlayer))
            {
                return false;
            }

            SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 0.35f, Pitch = 0.3f }, new Vector2(i, j) * 16f);
            return true;
        }

        // Only the server (or single player) changes the tile and creates the reward.
        public static bool TryOpen(int i, int j, int playerIndex)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || playerIndex < 0 || playerIndex >= Main.maxPlayers
                || i < 1 || j < 1 || i >= Main.maxTilesX - 1 || j >= Main.maxTilesY - 1)
                return false;

            Player player = Main.player[playerIndex];
            if (!player.active || player.dead || Vector2.DistanceSquared(player.Center, new Vector2(i * 16 + 8, j * 16 + 8)) > 12 * 16 * 12 * 16)
                return false;

            Tile clicked = Main.tile[i, j];
            int proudType = ModContent.TileType<ProudKnightSoulSkull>();
            int braveType = ModContent.TileType<BraveWarriorSoulSkull>();
            if (!clicked.HasTile || clicked.TileFrameX >= 36 || (clicked.TileType != proudType && clicked.TileType != braveType))
                return false;

            int originX = i - clicked.TileFrameX / 18 % 2;
            int originY = j - clicked.TileFrameY / 18 % 2;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Tile part = Main.tile[originX + x, originY + y];
                    if (!part.HasTile || part.TileType != clicked.TileType || part.TileFrameX != x * 18 || part.TileFrameY != y * 18)
                        return false;
                }
            }

            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    Main.tile[originX + x, originY + y].TileFrameX += 36;

            int reward = clicked.TileType == proudType ? ModContent.ItemType<ProudKnightSoul>() : ModContent.ItemType<BraveWarriorSoul>();
            int itemIndex = Item.NewItem(new EntitySource_Misc("Soul Skull"), new Rectangle(originX * 16, originY * 16, 32, 32), reward);
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendTileSquare(-1, i, j, 5);
                NetMessage.SendData(MessageID.SyncItem, number: itemIndex);
            }
            return true;
        }
    }

    public class ProudKnightSoulSkull : SoulRewardSkull
    {
        protected override int RewardItemType => ModContent.ItemType<ProudKnightSoul>();
    }

    public class BraveWarriorSoulSkull : SoulRewardSkull
    {
        protected override int RewardItemType => ModContent.ItemType<BraveWarriorSoul>();
    }
}
