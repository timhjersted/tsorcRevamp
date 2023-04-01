using System;
using Microsoft.Xna.Framework;
using Terraria;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Hooks.Items;
using tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Utilities;

namespace  tsorcRevamp.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Melee;

/// <summary>
/// Quick swing that lasts 1/2 of the use animation time.
/// Affects gameplay.
/// </summary>
public class QuickSlashMeleeAnimation : MeleeAnimation, ICanDoMeleeDamage
{
	public bool IsAttackFlipped { get; set; }
	public bool FlipAttackEachSwing { get; set; }
	public bool AnimateLegs { get; set; }

	public override float GetItemRotation(Player player, Item item)
	{
		float baseAngle;

		if (item.TryGetGlobalItem(out ItemMeleeAttackAiming meleeAiming)) {
			baseAngle = meleeAiming.AttackAngle;
		} else {
			baseAngle = 0f;
		}

		float step = 1f - MathHelper.Clamp(player.itemAnimation / (float)player.itemAnimationMax, 0f, 1f);
		int dir = player.direction * (IsAttackFlipped ? -1 : 1);

		float minValue = baseAngle - MathHelper.PiOver2 * 1.25f;
		float maxValue = baseAngle + MathHelper.PiOver2 * 1.0f;

		if (dir < 0) {
			Utils.Swap(ref minValue, ref maxValue);
		}

		//sword visual rotation
		//T1 is itemAnimation progress percentage, T2 is sword rotation angle at that percentage
		//lerps between consecutive angles
        var animation = new Gradient<float>(
			(0.0f, MathHelper.Lerp(minValue, maxValue, 0.1f)),
			(0.1f, minValue),
			(0.15f, MathHelper.Lerp(minValue, maxValue, 0.125f)),
			(0.151f, MathHelper.Lerp(minValue, maxValue, 0.8f)),
			(0.3f, MathHelper.Lerp(minValue, maxValue, 0.95f)),
			(0.5f, maxValue),
			(0.75f, maxValue),
			(0.9f, MathHelper.Lerp(minValue, maxValue, 0.96f)),
			(1.0f, MathHelper.Lerp(minValue, maxValue, 0.9f))
		);

		return animation.GetValue(step);
	}

	// Direction switching
	public override void UseAnimation(Item item, Player player)
	{
		base.UseAnimation(item, player);

		if (!Enabled || !FlipAttackEachSwing) {
			return;
		}

		if (item.TryGetGlobalItem(out ItemMeleeAttackAiming aiming)) {
			IsAttackFlipped = aiming.AttackId % 2 != 0;
		}
	}

	// Leg framing
	public override void UseItemFrame(Item item, Player player)
	{
		base.UseItemFrame(item, player);

		if (!Enabled || !AnimateLegs) {
			return;
		}

		var aiming = item.GetGlobalItem<ItemMeleeAttackAiming>();

		if (player.velocity.Y == 0f && player.KeyDirection().X == 0f) {
			if (Math.Abs(aiming.AttackDirection.X) > 0.5f) {
				player.legFrame = (IsAttackFlipped ? PlayerFrames.Walk8 : PlayerFrames.Jump).ToRectangle();
			} else {
				player.legFrame = PlayerFrames.Walk13.ToRectangle();
			}
		}
	}

	public bool CanDoMeleeDamage(Item item, Player player)
	{
		if (!Enabled) {
			return true;
		}
        //allows the player to hit multiple targets with one swing
        player.attackCD = 0;
        //in practice, because there is one frame between each hit and the weapon's active period is so restricted, the amount depends on the speed of the weapon
        //in other words, slower weapons have a larger hit window, and thus can hit more targets with each swing, which is exactly what i wanted. convenient
		//something something "programming by coincidence" i literally dont care


        //only deal melee damage when the swing visually occurs
        return (player.itemAnimation >= player.itemAnimationMax * 0.8f) && (player.itemAnimation <= player.itemAnimationMax * 0.9f);
	}
}
