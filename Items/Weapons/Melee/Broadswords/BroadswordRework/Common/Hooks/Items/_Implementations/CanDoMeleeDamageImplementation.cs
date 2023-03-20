using Terraria.ModLoader;
using Hook =  tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items.ICanDoMeleeDamage;

namespace  tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items;

internal sealed class CanDoMeleeDamageImplementation : GlobalItem
{
	public override void Load()
	{
		On.Terraria.Player.ItemCheck_MeleeHitNPCs += (orig, player, item, itemRectangle, originalDamage, knockback) => {
			if (Hook.Invoke(item, player)) {
				orig(player, item, itemRectangle, originalDamage, knockback);
			}
		};
	}
}
