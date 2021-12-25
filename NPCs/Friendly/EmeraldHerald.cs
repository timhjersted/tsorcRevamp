using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace tsorcRevamp.NPCs.Friendly
{
	[AutoloadHead]
	class EmeraldHerald : ModNPC
	{
		public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Emerald Herald");
			Main.npcFrameCount[npc.type] = 6;
		}

		public override string TownNPCName()
		{
			return "Jade";
		}

		public override void SetDefaults()
		{
			npc.townNPC = true;
			npc.friendly = true;
			npc.width = 18;
			npc.height = 46;
			npc.aiStyle = -1;
			npc.damage = 50;
			npc.defense = 9999;
			npc.lifeMax = 1000;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.knockBackResist = 1f;
		}


		#region Chat Functionality Stuff


		public override string GetChat()
		{
			Player player = Main.LocalPlayer;
			WeightedRandom<string> chat = new WeightedRandom<string>();

			if (!player.GetModPlayer<tsorcRevampPlayer>().FirstEncounter)
			{
				chat.Add("Are you the one I was sent to warn? My name is Jade, I am not of this world. I was sent here to warn you of the dangers posed by Attraidies, the Mindflayer King." +
					"\nHe has grown mighty in power and seeks to destroy not only your world, but also mine and many others.");
				player.GetModPlayer<tsorcRevampPlayer>().FirstEncounter = true;
			}

			else
			{
				if (Main.LocalPlayer.HasItem(ModContent.ItemType<Items.EstusFlaskShard>()) && Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 5)
				{
					chat.Add("Is that a shard you've found? Here, let me see it." + "\nSo that I may help you. To see light, to see hope… However faint it might be…");
				}
				else
				{
					if (!player.GetModPlayer<tsorcRevampPlayer>().ReceivedGift)
					{
						chat.Add("Listen to everything I have to say and I may give you a reward.", 4);
					}
					if (!tsorcRevampWorld.SuperHardMode)
					{
						chat.Add("Seek misery. For misery will lead you to greater, stronger souls. You will never defeat Attraidies with a soul so frail and palid.");
					}
					chat.Add("Forge your souls in the flames of sacred altars and make their power your own.");
					chat.Add("If you ever chance upon a weapon befallen the terrible curse of poor craftsmanship, bring it to me and I shall bless it.");
					chat.Add("I hope you have a keen eye, for this is a land brimming with secrets...");
					chat.Add("The near-constant use of potions will prove vital on your journey, especially when attempting to retrieve lost souls.");
					chat.Add("Bearer... Seek... Seek... Lest...", 0.05); // Easter egg. A classic DS2 meme. Rare dialogue.
				}
			}
			
			return chat;
		}

		int chatState = 0;

		public override void SetChatButtons(ref string button, ref string button2)
		{
			Player player = Main.LocalPlayer;

			button = "Bless";

			if (chatState == 0 || chatState == 1 || chatState == 2 || chatState == 3 || chatState == 4 || chatState == 5 || chatState == 6 || chatState == 7) { button2 = "Seek knowledge..."; }
			if (chatState == 8 || chatState == 9) { button2 = "Recieve gift"; }
			if (Main.LocalPlayer.HasItem(ModContent.ItemType<Items.EstusFlaskShard>()) && Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 5) { button2 = "Give Shard"; }

		}

		public override void OnChatButtonClicked(bool firstButton, ref bool shop)
		{
			Player player = Main.LocalPlayer;

			if (firstButton)
			{
				Main.playerInventory = true;
				Main.npcChatText = "";
				ModContent.GetInstance<tsorcRevamp>().EmeraldHeraldUserInterface.SetState(new UI.EmeraldHeraldUI());
				shop = false; // no shop
				return;
			}
            else
            {
				if (Main.LocalPlayer.HasItem(ModContent.ItemType<Items.EstusFlaskShard>()) && Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 5)
				{
					Main.PlaySound(SoundID.Item37); // Reforge/Anvil sound
					Main.npcChatText = $"If you happen to find another Estus Flask Shard, bring it to me. So that I may ease your burden.";
					int ShardItemIndex = Main.LocalPlayer.FindItem(ModContent.ItemType<Items.EstusFlaskShard>());

					if (Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 5) 
					{ 
						Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax += 1;
						if (Main.LocalPlayer.inventory[ShardItemIndex].stack == 1) { Main.LocalPlayer.inventory[ShardItemIndex].TurnToAir(); }
						else Main.LocalPlayer.inventory[ShardItemIndex].stack--;
						Main.NewText("Estus Flask size increased! Max charges:" + Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax, Color.OrangeRed);
					}
					return;
				}
				if (chatState == 0) //if you click while in state 0 (greeting)
				{
					Main.npcChatText = "A pickaxe or sticky bomb can be used to break blocks that are 1 width wide. Stone gates need switches to open. If in doubt, give it a whack."; //show this text
					chatState = 1; //move to state 1
					return;
				}
				if (chatState == 1) //if you click while on the first page of text
				{
					Main.npcChatText = "Dark souls have special properties. On death, you will drop all your souls and have one chance to recover them. Die before this and they are gone for good."; 
					chatState = 2; //move to state 2
					return;
				}
				if (chatState == 2) //if you click while on the second page of text
				{
					Main.npcChatText = "On your journey you will encounter unlit bonfires, light them and enjoy the peaceful respite they provide. You can use them as checkpoints and storage.";
					chatState = 3; //move to state 3, etc
					return;
				}
				if (chatState == 3)
				{
					Main.npcChatText = "You can stash excess ammo, weapons and armor in the safes and piggy banks found at save points and bonfires.";
					chatState = 4;
					return;
				}
				if (chatState == 4)
				{
					Main.npcChatText = "Don't move vanilla NPC's like the Wizard, Mechanic or Goblin Tinkerer until you've found them in game. Modded NPCs may be moved.";
					chatState = 5;
					return;
				}
				if (chatState == 5)
				{
					Main.npcChatText = "Stronger pickaxes can be used to open gates of strong ore.";
					chatState = 6;
					return;
				}
				if (chatState == 6)
				{
					Main.npcChatText = "And finally... I'm not sure what it means, but I was told to tell you to read something called 'The Game Manual' for more information. I get the feeling you know where to find it.";
					chatState = 7;
					return;
				}

				if (chatState == 7)
				{

					if (!player.GetModPlayer<tsorcRevampPlayer>().ReceivedGift)
					{
						Main.npcChatText = "Take this with you. May it ease your journey.";
						if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) { chatState = 9; }
						else { chatState = 8; }
					}
					else 
					{
						Main.npcChatText = "Good luck.";
						chatState = 0;
					}
						return;
				}
				if (chatState == 8)
				{
					Main.npcChatText = "If you ever need more, you may roast some over the flames of a bonfire. Farewell.";
					player.GetModPlayer<tsorcRevampPlayer>().ReceivedGift = true;
					Main.LocalPlayer.QuickSpawnItem(ModContent.ItemType<Items.Potions.MushroomSkewer>(), 10);
					Main.LocalPlayer.QuickSpawnItem(ModContent.ItemType<Items.SoulShekel>(), 100);

					if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
						Main.LocalPlayer.QuickSpawnItem(ItemID.WormholePotion, 5);
					}
					chatState = 0;
					return;
				}
				if (chatState == 9)
				{
					Main.npcChatText = "Bearer of the Curse, the Estus Flask will no doubt prove" + "\nto be invaluable on your journey. Farewell.";
					player.GetModPlayer<tsorcRevampPlayer>().ReceivedGift = true;
					Main.LocalPlayer.QuickSpawnItem(ModContent.ItemType<Items.Potions.MushroomSkewer>(), 10);
					Main.LocalPlayer.QuickSpawnItem(ModContent.ItemType<Items.SoulShekel>(), 100);
					Main.LocalPlayer.QuickSpawnItem(ModContent.ItemType<Items.Potions.Lifegem>(), 10);
					Main.NewText("Estus Flask acquired! Don't forget to assign it a hotkey in Controls > Mod Controls. The Estus Flask is a reusable healing item that can be refilled at bonfires", Color.OrangeRed);

					if (Main.netMode == NetmodeID.MultiplayerClient)
					{
						Main.LocalPlayer.QuickSpawnItem(ItemID.WormholePotion, 5);
					}
					chatState = 0;
					return;
				}
			}
		}


        #endregion


        #region AI and Spawning


        public override void AI()
		{
			npc.spriteDirection = npc.direction; //she's technically facing the opposite way she's looking but whatevs

			if (Main.worldID == VariousConstants.CUSTOM_MAP_WORLD_ID) // If it is our custom map
			{
				npc.velocity.X = 0; // Don't move left or right

				if (Main.dayTime && Main.player[Main.myPlayer].Distance(npc.Center) > 2500f) // If day and the player is far away
				{
					npc.position = new Vector2(4510.5f * 16, 737 * 16); // Stand under structure
				}
				if (!Main.dayTime && Main.player[Main.myPlayer].Distance(npc.Center) > 2500f) //If night and the player is far away
				{
					npc.position = new Vector2(4489.25f * 16, 732 * 16); // Stand by bonfire
				}

				if (npc.position.X > 4505f * 16) // If standing under structure
				{
					npc.direction = -1;
				}
				if (npc.position.X < 4505f * 16) // If standing by bonfire
				{
					npc.direction = 1;
				}
			}
		}


        //NO SPAWN CODE HERE, SHE SPAWNS ON WORLD ENTRY. See tsorcRevampPlayer OnEnterWorld


        #endregion


        #region Drawing and Animation

        //Emerald Herald Anim

        private const int Frame_Idle = 0;
		private const int Frame_Wind_1 = 1; //Minimum wind
		private const int Frame_Wind_2 = 2;
		private const int Frame_Wind_3 = 3;
		private const int Frame_Wind_4 = 4; //Use only on wind out
		private const int Frame_Wind_5 = 5; //Peak wind


		//Emerald Herald Eye Anim

		private const int Frame_Closed = 0;
		private const int Frame_Half_Open = 1;
		private const int Frame_Fully_Open = 2;
		private const int Frame_Fully_Open_Glint = 3;

		int eyeFrame;
		int eyeTimer;
		int idleTimer;

		public override void PostDraw(SpriteBatch spriteBatch, Color lightColor)
        {
			Texture2D eyeTexture = mod.GetTexture("NPCs/Friendly/EmeraldHerald_Eye");
			SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
			Rectangle myrectangle = eyeTexture.Frame(1, 4, 0, eyeFrame);
			if (npc.spriteDirection == -1)
			{
				spriteBatch.Draw(eyeTexture, npc.Center - Main.screenPosition, myrectangle, lightColor, npc.rotation, new Vector2(-3, 11), npc.scale, effects, 0f);
			}
			else
			{
				spriteBatch.Draw(eyeTexture, npc.Center - Main.screenPosition, myrectangle, lightColor, npc.rotation, new Vector2(7, 11), npc.scale, effects, 0f);
			}
		}

		public override void FindFrame(int frameHeight)
		{
			// Main Texture Logic

			idleTimer += Main.rand.Next(0, 4);

			if (idleTimer < 600)
			{
				if (idleTimer < 30)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
				else if (idleTimer < 60)
				{
					npc.frame.Y = Frame_Wind_1 * frameHeight;
				}
				else if (idleTimer < 90)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
				else if (idleTimer < 120)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
				else if (idleTimer < 150)
				{
					npc.frame.Y = Frame_Wind_1 * frameHeight;
				}
				else if (idleTimer < 180)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
				else if (idleTimer < 210)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
				else if (idleTimer < 240)
				{
					npc.frame.Y = Frame_Wind_1 * frameHeight;
				}
				else if (idleTimer < 270)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
				else if (idleTimer < 300)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
				else if (idleTimer < 330)
				{
					npc.frame.Y = Frame_Wind_1 * frameHeight;
				}
				else if (idleTimer < 360)
				{
					npc.frame.Y = Frame_Wind_2 * frameHeight;
				}
				else if (idleTimer < 390)
				{
					npc.frame.Y = Frame_Wind_1 * frameHeight;
				}
				else if (idleTimer < 420)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
				else if (idleTimer < 450)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
				else if (idleTimer < 480)
				{
					npc.frame.Y = Frame_Wind_1 * frameHeight;
				}
				else if (idleTimer < 510)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
				else if (idleTimer < 540)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
				else if (idleTimer < 570)
				{
					npc.frame.Y = Frame_Wind_1 * frameHeight;
				}
				else if (idleTimer < 600)
				{
					npc.frame.Y = Frame_Idle * frameHeight;
				}
			}

			if (idleTimer >= 600)
			{
				npc.frameCounter += Main.rand.Next(0, 4);

				if (npc.frameCounter < 30)
				{
					npc.frame.Y = Frame_Wind_1 * frameHeight;
				}
				else if (npc.frameCounter < 60)
				{
					npc.frame.Y = Frame_Wind_2 * frameHeight;
				}
				else if (npc.frameCounter < 90)
				{
					npc.frame.Y = Frame_Wind_3 * frameHeight;
				}
				else if (npc.frameCounter < 120)
				{
					npc.frame.Y = Frame_Wind_5 * frameHeight;
				}
				else if (npc.frameCounter < 150)
				{
					npc.frame.Y = Frame_Wind_4 * frameHeight;
				}
				else if (npc.frameCounter < 180)
				{
					npc.frame.Y = Frame_Wind_3 * frameHeight;
				}
				else if (npc.frameCounter < 210)
				{
					npc.frame.Y = Frame_Wind_2 * frameHeight;
				}
				else if (npc.frameCounter < 240)
				{
					npc.frame.Y = Frame_Wind_1 * frameHeight;
				}
				else
				{
					idleTimer = 0;
					npc.frameCounter = 0;
				}
			}



			// Eye Texture Logic

			eyeTimer += Main.rand.Next(0, 4);

			if (!Main.dayTime) //Eyes closed more time than open
			{
				if (eyeTimer < 1000)
				{
					eyeFrame = Frame_Closed * 1;
				}
				else if (eyeTimer < 2000)
				{
					eyeFrame = Frame_Half_Open * 1;
				}
				else if (eyeTimer < 2300)
				{
					eyeFrame = Frame_Fully_Open * 1;
				}
				else if (eyeTimer < 2600)
				{
					eyeFrame = Frame_Fully_Open_Glint * 1;
				}
				else if (eyeTimer < 2900)
				{
					eyeFrame = Frame_Fully_Open * 1;
				}
				else if (eyeTimer < 3900)
				{
					eyeFrame = Frame_Half_Open * 1;
				}
				else if (eyeTimer < 4900)
				{
					eyeFrame = Frame_Closed * 1;
				}
				else
				{
					eyeTimer = 0;
				}
			}

			if (Main.dayTime) // Eyes open more time than closed
            {
				if (eyeTimer < 200)
				{
					eyeFrame = Frame_Closed * 1;
				}
				else if (eyeTimer < 800)
				{
					eyeFrame = Frame_Half_Open * 1;
				}
				else if (eyeTimer < 1800)
				{
					eyeFrame = Frame_Fully_Open * 1;
				}
				else if (eyeTimer < 2200)
				{
					eyeFrame = Frame_Fully_Open_Glint * 1;
				}
				else if (eyeTimer < 3200)
				{
					eyeFrame = Frame_Fully_Open * 1;
				}
				else if (eyeTimer < 3600)
				{
					eyeFrame = Frame_Half_Open * 1;
				}
				else if (eyeTimer < 3800)
				{
					eyeFrame = Frame_Closed * 1;
				}
				else
				{
					eyeTimer = 0;
				}
			}
		}

		#endregion


	}
}
