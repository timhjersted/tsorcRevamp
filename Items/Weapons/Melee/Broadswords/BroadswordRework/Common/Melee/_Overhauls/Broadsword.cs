using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Core.ItemComponents;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Core.ItemOverhauls;

namespace  tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee;

public partial class Broadsword : ItemOverhaul
{
	public override bool ShouldApplyItemOverhaul(Item item)
	{
		// Broadswords always swing, deal melee damage, don't have channeling, and are visible
		if (item.useStyle != ItemUseStyleID.Swing || item.noMelee || item.channel || item.noUseGraphic) {
			return false;
		}

		// let's only exclude pickaxes
		if (item.pick > 0 || item.createTile >= TileID.Dirt || item.createWall >= 0) {
			return false;
		}

		if (item.DamageType != DamageClass.Melee) {
			return false;
		}

		return true;
	}

	public override void SetDefaults(Item item)
	{
		base.SetDefaults(item);

		// Components
		item.EnableComponent<ItemMeleeAttackAiming>();

		// Animation
		item.EnableComponent<QuickSlashMeleeAnimation>(c => {
			c.FlipAttackEachSwing = true;
			c.AnimateLegs = true;
		});
	}
}
