using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace tsorcRevamp
{
	// This class stores necessary player info for our custom resource (stamina) that governs the usage of special abilities such as dodge rolls, spins, dashes etc.
	public class tsorcRevampStaminaPlayer : ModPlayer
	{
		public static tsorcRevampStaminaPlayer ModPlayer(Player player)
		{
			return player.GetModPlayer<tsorcRevampStaminaPlayer>();
		}


		// Here we include a custom resource, similar to mana or health.
		// Creating some variables to define the current value of our Stamina resource as well as the current maximum value. We also include a temporary max value, as well as some variables to handle the natural regeneration of this resource.
		public float staminaResourceCurrent;
		public const float DefaultStaminaResourceMax = 100;
		public float staminaResourceMax;
		public float staminaResourceMax2;
		public float staminaResourceRegenRate;
		public float staminaResourceGainMult = 1;
		public float staminaResourceGain;
		internal float staminaResourceRegenTimer = 0f;
		public static readonly Color RestoreStaminaResource = new Color(20, 100, 20); // We can use this for CombatText, if you create an item that replenishes exampleResourceCurrent.

        /*
		In order to make the Example Resource example straightforward, several things have been left out that would be needed for a fully functional resource similar to mana and health. 
		Here are additional things you might need to implement if you intend to make a custom resource:
		- Multiplayer Syncing: The current example doesn't require MP code, but pretty much any additional functionality will require this. ModPlayer.SendClientChanges and clientClone will be necessary, as well as SyncPlayer if you allow the user to increase exampleResourceMax.
		- Save/Load and increased max resource: You'll need to implement Save/Load to remember increases to your exampleResourceMax cap.
		- Resouce replenishment item: Use GlobalNPC.NPCLoot to drop the item. ModItem.OnPickup and ModItem.ItemSpace will allow it to behave like Mana Star or Heart. Use code similar to Player.HealEffect to spawn (and sync) a colored number suitable to your resource.
		*/

        public override TagCompound Save()
        {
			return new TagCompound
			{

			{"staminaResourceMax", staminaResourceMax },

			};
		}

		public override void Load(TagCompound tag)
		{

			staminaResourceMax = tag.GetFloat("staminaResourceMax");

		}

		public override void Initialize()
		{
			staminaResourceMax = DefaultStaminaResourceMax;
			staminaResourceCurrent = staminaResourceMax;
		}

		public override void ResetEffects()
		{
			ResetVariables();
		}

		public override void UpdateDead()
		{
			ResetVariables();
		}

		private void ResetVariables()
		{
			if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
			{
				staminaResourceRegenRate = 1f;
			}

			else 
			{
				staminaResourceRegenRate = 2f; // Bearer of the Curse regains stamina at 2x speed
			}

			staminaResourceGainMult = 1f;

			staminaResourceMax2 = staminaResourceMax;
		}

		public override void PostUpdateMiscEffects()
		{
			UpdateResource();
		}

		// Lets do all our logic for the custom resource here, such as limiting it, increasing it and so on.
		private void UpdateResource()
		{

			//Main.NewText("Stamina: " + staminaResourceCurrent);
			//Main.NewText("Stamina regen rate: " + staminaResourceRegenRate);
			//Main.NewText("Stamina regen gain mult: " + staminaResourceGainMult);
			//Main.NewText("Timer: " + staminaResourceRegenTimer);

			staminaResourceGain = staminaResourceGainMult * staminaResourceRegenRate; //Apply our multiplier to our base regen rate

			// For our resource lets make it regen slowly over time to keep it simple, let's use exampleResourceRegenTimer to count up to whatever value we want, then increase currentResource.
			staminaResourceRegenTimer++; //Increase it by 60 per second, or 1 per tick.

			// A simple timer that goes up to 3, increases the exampleResourceCurrent by 1 and then resets back to 0.
			if (player.whoAmI == Main.myPlayer)
			{
				//no stamina regen during a roll/swordspin/using an item, for balance? Yes
				if (!player.GetModPlayer<tsorcRevampPlayer>().isDodging && !player.GetModPlayer<tsorcRevampPlayer>().isSwordflipping)
				{
					if (!player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
					{
						if (staminaResourceRegenTimer > 3)
						{
							staminaResourceCurrent += staminaResourceGain;
							staminaResourceRegenTimer = 0;
						}
					}
					else if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.itemAnimation == 0) //Bearer of the Curse doesn't regen stamina while using items
                    {
						if (staminaResourceRegenTimer > 3)
						{
							staminaResourceCurrent += staminaResourceGain;
							staminaResourceRegenTimer = 0;
						}
					}
				}
			}
			

			// Limit exampleResourceCurrent from going over the limit imposed by exampleResourceMax.
			staminaResourceCurrent = Utils.Clamp(staminaResourceCurrent, 0, staminaResourceMax2);
		}
	}
}
