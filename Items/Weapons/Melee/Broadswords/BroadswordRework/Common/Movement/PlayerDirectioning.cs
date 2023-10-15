using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Movement;

public sealed class BroadswordReworkPlayer : ModPlayer
{
    private const int MouseWorldSyncFrequency = 12;

    private Vector2 lastSyncedMouseWorld;

    public int ForcedDirection { get; set; }
    public Vector2 MouseWorld { get; set; }

    public override void UpdateDead()
    {
        var heldItem = Player.HeldItem;

        if (heldItem?.IsAir == false)
        {
            IHoldItemWhileDead.Invoke(heldItem, Player);
        }
    }

    public override void PreUpdate()
        => UpdateMouseWorld(true);

    public override void PostUpdate()
        => SetDirection();

    public override bool PreItemCheck()
    {
        SetDirection();

        return true;
    }

    public void SetDirection()
        => UpdateMouseWorld(false);

    private void UpdateMouseWorld(bool resetForcedDirection)
    {
        if (!Main.dedServ && Main.gameMenu)
        {
            Player.direction = 1;

            return;
        }

        if (Player.IsLocal() && Main.hasFocus)
        {
            MouseWorld = Main.MouseWorld;

            if (Main.netMode == NetmodeID.MultiplayerClient && Main.GameUpdateCount % MouseWorldSyncFrequency == 0 && lastSyncedMouseWorld != MouseWorld)
            {
                //MultiplayerSystem.SendPacket(new PlayerMousePositionPacket(Player));

                lastSyncedMouseWorld = MouseWorld;
            }
        }
    }
}
