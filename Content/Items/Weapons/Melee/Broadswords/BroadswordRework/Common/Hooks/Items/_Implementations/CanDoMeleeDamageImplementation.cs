using Terraria.ModLoader;
using Hook = tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items.ICanDoMeleeDamage;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items._Implementations;

internal sealed class CanDoMeleeDamageImplementation : GlobalItem
{
    public override void Load()
    {
        Terraria.On_Player.ItemCheck_MeleeHitNPCs += (orig, player, item, itemRectangle, originalDamage, knockback) =>
        {
            if (Hook.Invoke(item, player))
            {
                orig(player, item, itemRectangle, originalDamage, knockback);
            }
        };
    }
}
