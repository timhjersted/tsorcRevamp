using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.PlayerEffects;

public sealed class PlayerItemRotation : ModPlayer
{
    public float? ForcedItemRotation;

    public override void PostUpdate()
    {
        if (ForcedItemRotation.HasValue)
        {
            Player.itemRotation = ForcedItemRotation.Value;

            ForcedItemRotation = null;
        }
    }
}
