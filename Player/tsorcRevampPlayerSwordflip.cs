using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp
{
	public struct SwordflipTimer
	{
		private uint endTime;

		public bool Active => Main.GameUpdateCount < endTime;
		public uint Value
		{
			get => (uint)Math.Max(0, (long)endTime - Main.GameUpdateCount);
			set => endTime = Main.GameUpdateCount + Math.Max(0, value);
		}

		public void Set(uint minValue) => Value = Math.Max(minValue, Value);

		public static implicit operator SwordflipTimer(uint value) => new SwordflipTimer() { Value = value };
		public static implicit operator SwordflipTimer(int value) => new SwordflipTimer() { Value = (uint)value };
	}

	/*public enum PlayerFrames //we already have these in tsorcRevampPlayerDodgeRoll
	{
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
	}*/

	public partial class tsorcRevampPlayer : ModPlayer
	{
		public static float SwordflipTimeMax => 0.37f;
		public static uint SwordflipDefaultCooldown => 90;

		public SwordflipTimer swordflipCooldown;
		public sbyte swordflipDirection;
		public sbyte swordflipDirectionVisual;
		public sbyte wantedSwordflipDir;
		public float swordflipTime;
		public float swordflipStartRot;
		public float swordflipItemRotation;
		public bool isSwordflipping;
		public float wantsSwordflipTimer;
		public bool forceSwordflip;
		public bool noSwordflip;
		//public float rotation;
		//public float? forcedItemRotation;
		//public PlayerFrames? forcedHeadFrame;
		//public PlayerFrames? forcedBodyFrame;
		//public PlayerFrames? forcedLegFrame;
		//public int forcedDirection;

		public void QueueSwordflip(float wantTime, sbyte direction, bool force = false)
		{
			wantsSwordflipTimer = wantTime;
			wantedSwordflipDir = direction;

			if (force)
			{
				swordflipCooldown = 0;
			}
		}

		//public int KeyDirection(Player player) => player.controlLeft ? -1 : player.controlRight ? 1 : 0;
		//public static bool OnGround(Player player) => player.velocity.Y == 0f;
		//public static bool WasOnGround(Player player) => player.oldVelocity.Y == 0f;
		public static float SwordflipStepTowards(float value, float goal, float step)
		{
			if (goal > value)
			{
				value += step;

				if (value > goal)
				{
					return goal;
				}
			}
			else if (goal < value)
			{
				value -= step;

				if (value < goal)
				{
					return goal;
				}
			}

			return value;
		}

		private bool TryStartSwordflip()
		{
			bool isLocal = player.whoAmI == Main.myPlayer;

			//TODO re-enable sword flip once it's fixed
			if (isLocal && wantsSwordflipTimer <= 0f && /*tsorcRevamp.SwordflipKey.JustPressed*/ false && !player.mouseInterface && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent > 40)
			{
				QueueSwordflip(0.25f, (sbyte)KeyDirection(player));
			}

			if (!forceSwordflip)
			{
				//Only initiate dodgerolls locally.
				if (!isLocal)
				{
					return false;
				}

				//Input & cooldown check. The cooldown can be enforced by other actions.
				if (wantsSwordflipTimer <= 0f || swordflipCooldown.Active)
				{
					return false;
				}

				//Don't allow dodging on mounts and during item use.
				if ((player.mount != null && player.mount.Active) /*|| player.itemAnimation > 0*/)
				{
					return false;
				}
			}

			wantsSwordflipTimer = 0f;
			player.grappling[0] = -1;
			player.grapCount = 0;
			for (int p = 0; p < 1000; p++)
			{
				if (Main.projectile[p].active && Main.projectile[p].owner == player.whoAmI && Main.projectile[p].aiStyle == 7)
				{
					Main.projectile[p].Kill();
				}
			}

			player.eocHit = 1;

			isSwordflipping = true;
			player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 40;
			player.immune = true;
			player.immuneTime = 15;
			swordflipStartRot = player.GetModPlayer<tsorcRevampPlayer>().rotation;
			swordflipItemRotation = player.itemRotation;
			swordflipTime = 0f;
			swordflipDirectionVisual = (sbyte)player.direction;
			swordflipDirection = wantedSwordflipDir != 0 ? wantedSwordflipDir : (sbyte)player.direction;
			swordflipCooldown = SwordflipDefaultCooldown;

			if (!isLocal)
			{
				forceSwordflip = false;
			}
			else if (Main.netMode != NetmodeID.SinglePlayer)
			{
				//MultiplayerSystem.SendPacket(new PlayerDodgerollPacket(player));
			}

			return true;
		}
		private void UpdateSwordflip()
		{
			Item item = player.HeldItem;
			wantsSwordflipTimer = SwordflipStepTowards(wantsSwordflipTimer, 0f, (float)1 / 60);
			noSwordflip |= player.mount.Active;

			if (noSwordflip)
			{
				isSwordflipping = false;
				noSwordflip = false;

				return;
			}

			bool onGround = OnGround(player);
			ref float rotation = ref player.GetModPlayer<tsorcRevampPlayer>().rotation;

			//Attempt to initiate a dodgeroll if the player isn't doing one already.
			if (!isSwordflipping && !TryStartSwordflip())
			{
				return;
			}
			//Apply velocity
			if (swordflipTime < SwordflipTimeMax * 0.5f)
			{
				//player.itemAnimation = player.itemAnimationMax - 4;
				float newVelX = (onGround ? 6f : 12f) * swordflipDirection;
				float newVelY = (onGround ? -8f : 0f);

				if (Math.Abs(player.velocity.X) < Math.Abs(newVelX) || Math.Sign(newVelX) != Math.Sign(player.velocity.X))
				{
					player.velocity.X = newVelX;
				}
				if (player.velocity.Y > newVelY /*|| Math.Sign(newVelY) != Math.Sign(player.velocity.Y)*/)
				{
					player.velocity.Y = newVelY;
				}
			}

			player.pulley = false;

			//Apply rotations & direction
			forcedItemRotation = swordflipItemRotation;
			forcedLegFrame = PlayerFrames.Jump;
			forcedDirection = swordflipDirectionVisual;

			rotation = swordflipDirection == 1
				? Math.Min(MathHelper.Pi * 2f, MathHelper.Lerp(swordflipStartRot, MathHelper.TwoPi, swordflipTime / (SwordflipTimeMax * 1f)))
				: Math.Max(-MathHelper.Pi * 2f, MathHelper.Lerp(swordflipStartRot, -MathHelper.TwoPi, swordflipTime / (SwordflipTimeMax * 1f)));

			//Progress the dodgeroll
			swordflipTime += 1f / 60f;

			/*if (swordflipTime == 1)
			{
				player.itemAnimation = player.itemAnimationMax;
				item.noMelee = true;
				item.useAnimation = (int)(40);
			}*/
			if (swordflipTime <= SwordflipTimeMax - 5)
			{
				player.itemAnimation = player.itemAnimationMax;
				//item.noMelee = true;
				//item.useAnimation = (int)(28);
			}
			if (swordflipTime >= SwordflipTimeMax - 5)
			{
				player.itemAnimation = player.itemAnimationMax - 1;
				//item.noMelee = true;
				//item.useAnimation = (int)(28);
			}
			if (swordflipTime >= SwordflipTimeMax)
			{
				player.itemAnimation = player.itemAnimationMax;
				item.noMelee = true;
				item.useAnimation = (int)(28);
			}
			if (swordflipTime >= SwordflipTimeMax * 0.6f)
			{
				player.velocity.X *= 0.9f;
			}
			if (swordflipTime >= SwordflipTimeMax)
			{
				isSwordflipping = false;
				player.eocDash = 0;

				//forceSyncControls = true;
			}
			else
			{
				player.runAcceleration = 0f;
			}
		}
	}
}
