using System;
using System.Collections.Generic;

namespace  tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Core.ItemOverhauls;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class ItemAttachmentAttribute : Attribute
{
	public readonly IReadOnlyList<int> ItemIds;

	public ItemAttachmentAttribute(params int[] itemIds)
	{
		ItemIds = Array.AsReadOnly(itemIds);
	}
}
