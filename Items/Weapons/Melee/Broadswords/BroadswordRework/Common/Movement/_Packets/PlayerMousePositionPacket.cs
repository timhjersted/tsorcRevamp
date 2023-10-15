using System.IO;
using Terraria;
using Terraria.ID;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Core.Networking;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Movement;

public sealed class PlayerMousePositionPacket : NetPacket
{
    public PlayerMousePositionPacket(Player player)
    {
        var modPlayer = player.GetModPlayer<BroadswordReworkPlayer>();

        Writer.TryWriteSenderPlayer(player);

        Writer.WriteVector2(modPlayer.MouseWorld);
    }

    public override void Read(BinaryReader reader, int sender)
    {
        if (!reader.TryReadSenderPlayer(sender, out var player))
        {
            return;
        }

        var modPlayer = player.GetModPlayer<BroadswordReworkPlayer>();

        modPlayer.MouseWorld = reader.ReadVector2();

        // Resend
        if (Main.netMode == NetmodeID.Server)
        {
            //MultiplayerSystem.SendPacket(new PlayerMousePositionPacket(player), ignoreClient: sender);
        }
    }
}
