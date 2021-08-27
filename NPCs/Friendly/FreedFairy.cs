using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Weapons.Magic;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;
using System.Collections.Generic;

namespace tsorcRevamp.NPCs.Friendly
{
	[AutoloadHead]
	class FreedFairy : ModNPC
	{
		public override bool Autoload(ref string name) => true;

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Freed Fairy");
			Main.npcFrameCount[npc.type] = 6;
			//NPCID.Sets.HatOffsetY[npc.type] = 4;
		}
		public override string TownNPCName()
		{
			return "Freed Fairy";
		}
		public override void SetDefaults()
		{
			npc.townNPC = true;
			npc.noGravity = true;
			npc.friendly = true;
			npc.width = 18;
			npc.height = 18;
			npc.aiStyle = 7;
			npc.damage = 90;
			npc.defense = 15;
			npc.lifeMax = 1000;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.knockBackResist = 0.5f;
			npc.dontTakeDamage = true;
			animationType = NPCID.Guide;

		}

		#region Frames
		public override void FindFrame(int currentFrame)
		{
			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
			}
			if (npc.velocity.X < 0)
			{
				npc.spriteDirection = -1;
			}
			else
			{
				npc.spriteDirection = 1;
			}
			npc.rotation = npc.velocity.X * 0.08f;
			npc.frameCounter += 1.0;
			if (npc.frameCounter >= 8.0)
			{
				npc.frame.Y = npc.frame.Y + num;
				npc.frameCounter = 0.0;
			}
			if (npc.frame.Y >= num * Main.npcFrameCount[npc.type])
			{
				npc.frame.Y = 0;
			}
		}
		#endregion

		#region Chat
		public override string GetChat()
		{
			return "Thank you for freeing me! Have the Oxyale I recovered from the bottom of the sping";
		}
		#endregion
		public override void SetChatButtons(ref string button, ref string button2)
		{
			button = Language.GetTextValue("Accept");
		}

		bool droppedOxyale = false;
		public override void OnChatButtonClicked(bool firstButton, ref bool shop)
        {
			if (!droppedOxyale)
			{
				//Drop one for each player
				for (int i = 0; i < Main.ActivePlayersCount; i++)
				{
					Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.Oxyale>());
				}
				droppedOxyale = true;
			}
		}

		#region AI
		public override void AI()
		{
			//The despawning has to be done in AI(), because spawning dusts from within OnChatButtonClicked doesn't work specifically when autopause is on.
			//I don't know why. I probably don't want to know why. This is as simple as workarounds get, though.
			if (droppedOxyale)
            {
				npc.active = false;
				for (int i = 0; i < 20; i++)
				{
					Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, DustID.BlueFairy, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3));
				}
				return;
			}
			npc.TargetClosest(true);

			this.npc.directionY = -1;
				if (this.npc.direction == 0)
				{
					this.npc.direction = 1;
				}
				
				if (Main.player[npc.target].active && Main.player[npc.target].talkNPC == this.npc.whoAmI)
				{
					if (this.npc.ai[0] != 0f)
					{
						this.npc.netUpdate = true;
					}
					this.npc.ai[0] = 0f;
					this.npc.ai[1] = 300f;
					this.npc.ai[2] = 100f;
					if (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) < this.npc.position.X + (float)(this.npc.width / 2))
					{
						this.npc.direction = -1;
					}
					else
					{
						this.npc.direction = 1;
					}
				}
				
				if (npc.ai[2] >= 0f)
				{
					int num258 = 16;
					bool flag26 = false;
					bool flag27 = false;
					if (npc.position.X > npc.ai[0] - (float)num258 && npc.position.X < npc.ai[0] + (float)num258)
					{
						flag26 = true;
					}
					else
					{
						if ((npc.velocity.X < 0f && npc.direction > 0) || (npc.velocity.X > 0f && npc.direction < 0))
						{
							flag26 = true;
						}
					}
					num258 += 24;
					if (npc.position.Y > npc.ai[1] - (float)num258 && npc.position.Y < npc.ai[1] + (float)num258)
					{
						flag27 = true;
					}
					if (flag26 && flag27)
					{
						npc.ai[2] += 1f;						
						if (npc.ai[2] >= 60f)
						{
							npc.ai[2] = -200f;
							npc.direction *= -1;
							npc.velocity.X = npc.velocity.X * -1f;
							npc.collideX = false;
						}
					}
					else
					{
						npc.ai[0] = npc.position.X;
						npc.ai[1] = npc.position.Y;
						npc.ai[2] = 0f;
					}
					npc.TargetClosest(true);
				}
				else
				{
					npc.ai[2] += 1f;
					if (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) > npc.position.X + (float)(npc.width / 2))
					{
						npc.direction = -1;
					}
					else
					{
						npc.direction = 1;
					}
				}
				int num259 = (int)((npc.position.X + (float)(npc.width / 2)) / 16f) + npc.direction * 2;
				int num260 = (int)((npc.position.Y + (float)npc.height) / 16f);
				if (npc.position.Y > Main.player[npc.target].position.Y)
				{
					npc.velocity.Y -= .2f;
					if (npc.velocity.Y < -1.8f)
					{
						npc.velocity.Y = -1.8f;
					}
				}
				if (npc.position.Y < Main.player[npc.target].position.Y)
				{
					npc.velocity.Y += .2f;
					if (npc.velocity.Y > 1.8f)
					{
						npc.velocity.Y = 1.8f;
					}
				}
				if (npc.collideX)
				{
					npc.velocity.X = npc.oldVelocity.X * -0.4f;
					if (npc.direction == -1 && npc.velocity.X > 0f && npc.velocity.X < 1f)
					{
						npc.velocity.X = 1f;
					}
					if (npc.direction == 1 && npc.velocity.X < 0f && npc.velocity.X > -1f)
					{
						npc.velocity.X = -1f;
					}
				}
				if (npc.collideY)
				{
					npc.velocity.Y = npc.oldVelocity.Y * -0.25f;
					if (npc.velocity.Y > 0f && npc.velocity.Y < 1f)
					{
						npc.velocity.Y = 1f;
					}
					if (npc.velocity.Y < 0f && npc.velocity.Y > -1f)
					{
						npc.velocity.Y = -1f;
					}
				}
				float num270 = 2.5f;
				if (npc.direction == -1 && npc.velocity.X > -num270)
				{
					npc.velocity.X = npc.velocity.X - 0.1f;
					if (npc.velocity.X > num270)
					{
						npc.velocity.X = npc.velocity.X - 0.1f;
					}
					else
					{
						if (npc.velocity.X > 0f)
						{
							npc.velocity.X = npc.velocity.X + 0.05f;
						}
					}
					if (npc.velocity.X < -num270)
					{
						npc.velocity.X = -num270;
					}
				}
				else
				{
					if (npc.direction == 1 && npc.velocity.X < num270)
					{
						npc.velocity.X = npc.velocity.X + 0.1f;
						if (npc.velocity.X < -num270)
						{
							npc.velocity.X = npc.velocity.X + 0.1f;
						}
						else
						{
							if (npc.velocity.X < 0f)
							{
								npc.velocity.X = npc.velocity.X - 0.05f;
							}
						}
						if (npc.velocity.X > num270)
						{
							npc.velocity.X = num270;
						}
					}
				}
			if (npc.directionY == -1 && (double)npc.velocity.Y > -2.5)
			{
				npc.velocity.Y = npc.velocity.Y - 0.04f;
				if ((double)npc.velocity.Y > 2.5)
				{
					npc.velocity.Y = npc.velocity.Y - 0.05f;
				}
				else
				{
					if (npc.velocity.Y > 0f)
					{
						npc.velocity.Y = npc.velocity.Y + 0.03f;
					}
				}
				if ((double)npc.velocity.Y < -2.5)
				{
					npc.velocity.Y = -2.5f;
				}
			}
			else
			{
				if (npc.directionY == 1 && (double)npc.velocity.Y < 2.5)
				{
					npc.velocity.Y = npc.velocity.Y + 0.04f;
					if ((double)npc.velocity.Y < -2.5)
					{
						npc.velocity.Y = npc.velocity.Y + 0.05f;
					}
					else
					{
						if (npc.velocity.Y < 0f)
						{
							npc.velocity.Y = npc.velocity.Y - 0.03f;
						}
					}
					if ((double)npc.velocity.Y > 2.5)
					{
						npc.velocity.Y = 2.5f;
					}
				}
			}
						
			Lighting.AddLight((int)npc.position.X / 16, (int)npc.position.Y / 16, 0.6f, 0.3f, 0.0f);
			return;
		}
		#endregion
	}
}