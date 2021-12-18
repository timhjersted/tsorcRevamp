using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp {
	public struct Timer {
		private uint endTime;

		public bool Active => Main.GameUpdateCount < endTime;
		public uint Value {
			get => (uint)Math.Max(0, (long)endTime - Main.GameUpdateCount);
			set => endTime = Main.GameUpdateCount + Math.Max(0, value);
		}

		public void Set(uint minValue) => Value = Math.Max(minValue, Value);

		public static implicit operator Timer(uint value) => new Timer() { Value = value };
		public static implicit operator Timer(int value) => new Timer() { Value = (uint)value };
	}

	public enum PlayerFrames {
		Idle,
		Use1,
		Use2,
		Use3,
		Use4,
		Jump,
		Walk1,
		Walk2,
		Walk3,
		Walk4,
		Walk5,
		Walk6,
		Walk7,
		Walk8,
		Walk9,
		Walk10,
		Walk11,
		Walk12,
		Walk13,
		Walk14,
		Count
	}
	public partial class tsorcRevampPlayer : ModPlayer {
		public static float DodgeTimeMax => 0.37f;
		public static uint DodgeDefaultCooldown => 30;

		public Timer dodgeCooldown;
		public sbyte dodgeDirection;
		public sbyte dodgeDirectionVisual;
		public sbyte wantedDodgerollDir;
		public float dodgeTime;
		public float dodgeStartRot;
		public float dodgeItemRotation;
		public bool isDodging;
		public float wantsDodgerollTimer;
		public bool forceDodgeroll;
		public bool noDodge;
		public float rotation;
		public float? forcedItemRotation;
		public PlayerFrames? forcedHeadFrame;
		public PlayerFrames? forcedBodyFrame;
		public PlayerFrames? forcedLegFrame;
		public int forcedDirection;

		public override bool PreItemCheck() {
			UpdateDodging();
			UpdateSwordflip();

			//Stop umbrella and other things from working
			if (isDodging && player.HeldItem.type == ItemID.Umbrella) {
				return false;
			}

			Item item = player.HeldItem;

			if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
			{
				// Stamina drain for most (hopefully) swords and spears
				if (item.damage >= 1 && item.melee && player.itemAnimation == player.itemAnimationMax - 1 && item.pick == 0)
				{
					player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= (item.useAnimation * player.meleeSpeed * .8f);
				}

				// Stamina drain for pickaxes. They take you down to 30 stamina but keep working infinitely to allow for a roll or a hit or 2 on an enemy in self defence when mining. Pickaxe damage halved in GlobalItem to prevent usage as weapon.
				if (item.damage >= 1 && item.melee && player.itemAnimation == player.itemAnimationMax - 1 && item.pick != 0 && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 30)
				{
					player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= (item.useAnimation * player.meleeSpeed * .2f);
				}

				// Stamina drain for flails and yoyos
				if (item.damage >= 1 && item.useStyle == ItemUseStyleID.HoldingOut && item.melee && player.itemAnimation != 0
					&& (item.type != ItemID.Spear && item.type != ItemID.Trident && item.type != ItemID.TheRottedFork && item.type != ItemID.Swordfish && item.type != ItemID.DarkLance
					&& item.type != ItemID.CobaltNaginata && item.type != ItemID.PalladiumPike && item.type != ItemID.MythrilHalberd && item.type != ItemID.OrichalcumHalberd
					&& item.type != ItemID.AdamantiteGlaive && item.type != ItemID.TitaniumTrident && item.type != ItemID.Gungnir && item.type != ItemID.ChlorophytePartisan
					&& /*item.type != ItemID.MonkStaffT1 &&*/ item.type != ItemID.MonkStaffT2 && /*item.type != ItemID.MonkStaffT3 &&*/ item.type != ItemID.MushroomSpear
					&& item.type != ItemID.ObsidianSwordfish && item.type != ItemID.NorthPole && item.type != ModContent.ItemType<Items.Weapons.Melee.CopperSpear>()
					&& item.type != ModContent.ItemType<Items.Weapons.Melee.IronSpear>() && item.type != ModContent.ItemType<Items.Weapons.Melee.SilverSpear>()
					&& item.type != ModContent.ItemType<Items.Weapons.Melee.GoldSpear>() && item.type != ModContent.ItemType<Items.Weapons.Melee.ForgottenPearlSpear>()
					&& item.type != ModContent.ItemType<Items.Weapons.Melee.HiRyuuSpear>() && item.type != ModContent.ItemType<Items.Weapons.Melee.AncientDragonLance>()
					&& item.type != ModContent.ItemType<Items.Weapons.Melee.AncientBloodLance>() && item.type != ModContent.ItemType<Items.Weapons.Melee.AncientHolyLance>()
					&& item.type != ModContent.ItemType<Items.Weapons.Melee.CelestialLance>() && item.type != ModContent.ItemType<Items.Weapons.Melee.DragoonLance>()
					&& item.type != ModContent.ItemType<Items.Weapons.Melee.ForgottenRadiantLance>() && item.type != ModContent.ItemType<Items.Weapons.Melee.SupremeDragoonLance>()
					&& item.type != ModContent.ItemType<Items.Weapons.Melee.ForgottenImpHalberd>() && item.type != ModContent.ItemType<Items.Weapons.Melee.OldHalberd>()
					&& item.type != ModContent.ItemType<Items.Weapons.Melee.OrcishHalberd>() && item.type != ModContent.ItemType<Items.Weapons.Melee.ReforgedOldHalberd>()
					&& item.type != ModContent.ItemType<Items.Weapons.Melee.ForgottenPolearm>()))
				{
					player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 0.6f; // Drain .6 stamina/tick
				}

				// Ranged
				if (item.damage >= 1 && item.ranged && player.itemAnimation == player.itemAnimationMax - 1)
				{
					player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= (item.useAnimation * .8f);
				}

				// Magic & Throwing
				if (item.damage >= 1 && (item.magic || item.thrown) && player.itemAnimation == player.itemAnimationMax - 1)
				{
					player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= (item.useAnimation * .8f);
				}

				// Summoner
				if (item.damage >= 1 && item.summon && player.itemAnimation == player.itemAnimationMax - 1)
				{
					player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= (item.useAnimation * 1f);
				}

				// Classless? Just in case? 
				if (item.damage >= 1 && (!item.melee && !item.ranged && !item.magic && !item.summon && !item.thrown) && player.itemAnimation == player.itemAnimationMax - 1)
				{
					player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= (item.useAnimation * 0.8f);
				}
			}

			return true;
		}

        public override void PostItemCheck()
        {
			Item item = player.HeldItem;

			/*if (item.damage >= 1 && item.melee && player.itemAnimation == player.itemAnimationMax - 1 && player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
			{
				player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= (item.useAnimation * player.meleeSpeed * .75f) + 2; // The +2 is to balance out incredibly fast things being too efficient
			}*/

			base.PostItemCheck();
        }
        public void QueueDodgeroll(float wantTime, sbyte direction, bool force = false) {
			wantsDodgerollTimer = wantTime;
			wantedDodgerollDir = direction;

			if (force) {
				dodgeCooldown = 0;
			}
		}

		public int KeyDirection(Player player) => player.controlLeft ? -1 : player.controlRight ? 1 : 0;
		public static bool OnGround(Player player) => player.velocity.Y == 0f;
		public static bool WasOnGround(Player player) => player.oldVelocity.Y == 0f;
		public static float StepTowards(float value, float goal, float step) {
			if (goal > value) {
				value += step;

				if (value > goal) {
					return goal;
				}
			}
			else if (goal < value) {
				value -= step;

				if (value < goal) {
					return goal;
				}
			}

			return value;
		}

		private bool TryStartDodgeroll() {
			bool isLocal = player.whoAmI == Main.myPlayer;

			if (isLocal && wantsDodgerollTimer <= 0f && tsorcRevamp.DodgerollKey.JustPressed && !player.mouseInterface && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 30) {
				QueueDodgeroll(0.25f, (sbyte)KeyDirection(player));
			}

			if (!forceDodgeroll) {
				//Only initiate dodgerolls locally.
				if (!isLocal) {
					return false;
				}

				//Input & cooldown check. The cooldown can be enforced by other actions.
				if (wantsDodgerollTimer <= 0f || dodgeCooldown.Active) {
					return false;
				}

				//Don't allow dodging on mounts and during item use.
				if ((player.mount != null && player.mount.Active) || player.itemAnimation > 0) {
					return false;
				}
			}

			wantsDodgerollTimer = 0f;
			player.grappling[0] = -1;
			player.grapCount = 0;
			for (int p = 0; p < 1000; p++) {
				if (Main.projectile[p].active && Main.projectile[p].owner == player.whoAmI && Main.projectile[p].aiStyle == 7) {
					Main.projectile[p].Kill();
				}
			}

			player.eocHit = 1;

			isDodging = true;
			//only subtract stamina on a successful roll
			player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 30;
			player.immune = true;
			player.immuneTime = 15;
			dodgeStartRot = player.GetModPlayer<tsorcRevampPlayer>().rotation;
			dodgeItemRotation = player.itemRotation;
			dodgeTime = 0f;
			dodgeDirectionVisual = (sbyte)player.direction;
			dodgeDirection = wantedDodgerollDir != 0 ? wantedDodgerollDir : (sbyte)player.direction;
			dodgeCooldown = DodgeDefaultCooldown;

			if (!isLocal) {
				forceDodgeroll = false;
			}
			else if (Main.netMode != NetmodeID.SinglePlayer) {
				ModPacket rollPacket = ModContent.GetInstance<tsorcRevamp>().GetPacket();
				rollPacket.Write((byte)tsorcPacketID.SyncPlayerDodgeroll);
				rollPacket.Write(false);
				rollPacket.Write((byte)player.whoAmI);
				rollPacket.Write(wantedDodgerollDir);
				rollPacket.WriteVector2(player.velocity);
				rollPacket.Send();
			}

			return true;
		}
		private void UpdateDodging() {
			wantsDodgerollTimer = StepTowards(wantsDodgerollTimer, 0f, (float)1 / 60);

			noDodge |= player.mount.Active;

			if (noDodge) {
				isDodging = false;
				noDodge = false;

				return;
			}

			bool onGround = OnGround(player);
			ref float rotation = ref player.GetModPlayer<tsorcRevampPlayer>().rotation;

			//Attempt to initiate a dodgeroll if the player isn't doing one already.
			if (!isDodging && !TryStartDodgeroll()) {
				return;
			}
			//Apply velocity
			if (dodgeTime < DodgeTimeMax * 0.5f) {
				float newVelX = (onGround ? 6f : 4f) * dodgeDirection;

				if (Math.Abs(player.velocity.X) < Math.Abs(newVelX) || Math.Sign(newVelX) != Math.Sign(player.velocity.X)) {
					player.velocity.X = newVelX;
				}

			}

			player.pulley = false;

			//Apply rotations & direction
			forcedItemRotation = dodgeItemRotation;
			forcedLegFrame = PlayerFrames.Jump;
			forcedDirection = dodgeDirectionVisual;

			rotation = dodgeDirection == 1
				? Math.Min(MathHelper.Pi * 2f, MathHelper.Lerp(dodgeStartRot, MathHelper.TwoPi, dodgeTime / (DodgeTimeMax * 1f)))
				: Math.Max(-MathHelper.Pi * 2f, MathHelper.Lerp(dodgeStartRot, -MathHelper.TwoPi, dodgeTime / (DodgeTimeMax * 1f)));
			  //Progress the dodgeroll
			dodgeTime += 1f / 60f;

			if (dodgeTime >= DodgeTimeMax * 0.6f) {
				player.velocity.X *= 0.9f;
            }

			if (dodgeTime >= DodgeTimeMax) {
				isDodging = false;
				player.eocDash = 0;

				//forceSyncControls = true;
			}
			else {
				player.runAcceleration = 0f;
			}
		}
	}
}
