using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class EmeraldHerald : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Emerald Herald");
            Main.npcFrameCount[NPC.type] = 6;
        }

        public override List<string> SetNPCNameList()
        {
            List<string> list = new List<string>();
            list.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Name1"));
            return list;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 46;
            NPC.aiStyle = -1;
            NPC.damage = 50;
            NPC.defense = 9999;
            NPC.lifeMax = 1000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 1f;
        }


        #region Chat Functionality Stuff


        public override string GetChat()
        {
            Player player = Main.LocalPlayer;
            WeightedRandom<string> chat = new WeightedRandom<string>();

            if (!player.GetModPlayer<tsorcRevampPlayer>().FirstEncounter)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ashen-one") with { Volume = 0.5f }, NPC.Center);

                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.FirstEncounter"));
                player.GetModPlayer<tsorcRevampPlayer>().FirstEncounter = true;
            }

            else
            {
                if (Main.LocalPlayer.HasItem(ModContent.ItemType<Items.EstusFlaskShard>()) && Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 6)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ashen-one") with { Volume = 0.5f }, NPC.Center);
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.HasShard"));
                }
                else
                {
                    if (!player.GetModPlayer<tsorcRevampPlayer>().ReceivedGift)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ashen-one") with { Volume = 0.5f }, NPC.Center);
                        chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.RewardTip"), 4);
                    }
                    if (!tsorcRevampWorld.SuperHardMode)
                    {
                        chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.AttradiesTip"));
                    }
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip1"));
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip2"));
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip3"));
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip4"));
                    chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.EasterEgg"), 0.05); // Easter egg. A classic DS2 meme. Rare dialogue.
                }
            }

            return chat;
        }

        int chatState = 0;

        public override void SetChatButtons(ref string button, ref string button2)
        {
            Player player = Main.LocalPlayer;

            button = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.button1");

            if (chatState == 0 || chatState == 1 || chatState == 2 || chatState == 3 || chatState == 4 || chatState == 5 || chatState == 6 || chatState == 7) { button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.button2v1"); }
            if (chatState == 8 || chatState == 9) { button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.button2v2"); }
            if (Main.LocalPlayer.HasItem(ModContent.ItemType<Items.EstusFlaskShard>()) && Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 6) { button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.button2v3"); }

        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            Player player = Main.LocalPlayer;

            if (firstButton)
            {
                Main.playerInventory = true;
                Main.npcChatText = "";
                ModContent.GetInstance<tsorcRevamp>().EmeraldHeraldUserInterface.SetState(new UI.EmeraldHeraldUI());
                //shop = false; // no shop
                return;
            }
            else
            {
                if (Main.LocalPlayer.HasItem(ModContent.ItemType<Items.EstusFlaskShard>()) && Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 6)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item37); // Reforge/Anvil sound
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.BringShards");
                    int ShardItemIndex = Main.LocalPlayer.FindItem(ModContent.ItemType<Items.EstusFlaskShard>());

                    if (Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 6)
                    {
                        Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax += 1;
                        Main.LocalPlayer.GetModPlayer<tsorcRevampCeruleanPlayer>().ceruleanChargesMax += 2;
                        if (Main.LocalPlayer.inventory[ShardItemIndex].stack == 1) { Main.LocalPlayer.inventory[ShardItemIndex].TurnToAir(); }
                        else Main.LocalPlayer.inventory[ShardItemIndex].stack--;

                        if (Main.netMode != NetmodeID.Server)
                        {
                            Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.EstusUpgrade") + Main.LocalPlayer.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax, Color.OrangeRed);
                            Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.CeruleanUpgrade") + Main.LocalPlayer.GetModPlayer<tsorcRevampCeruleanPlayer>().ceruleanChargesMax, Color.RoyalBlue);
                        }
                    }
                    return;
                }
                if (chatState == 0) //if you click while in state 0 (greeting)
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip5"); //show this text
                    chatState = 1; //move to state 1
                    return;
                }
                if (chatState == 1) //if you click while on the first page of text
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip6");
                    chatState = 2; //move to state 2
                    return;
                }
                if (chatState == 2) //if you click while on the second page of text
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip7");
                    chatState = 3; //move to state 3, etc
                    return;
                }
                if (chatState == 3)
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip8");
                    chatState = 5;
                    return;
                }
                /*if (chatState == 4)
				{
					Main.npcChatText = "Don't move vanilla NPC's like the Wizard, Mechanic or Goblin Tinkerer until you've found them in game. Modded NPCs may be moved.";
					chatState = 5;
					return;
				}*/
                if (chatState == 5)
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip9");
                    chatState = 6;
                    return;
                }
                if (chatState == 6)
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip10");
                    chatState = 7;
                    return;
                }

                if (chatState == 7)
                {

                    if (!player.GetModPlayer<tsorcRevampPlayer>().ReceivedGift)
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.ReceiveGift");
                        if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse) { chatState = 9; }
                        else { chatState = 8; }
                    }
                    else
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Luck");
                        chatState = 0;
                    }
                    return;
                }
                if (chatState == 8)
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Gift");
                    player.GetModPlayer<tsorcRevampPlayer>().ReceivedGift = true;
                    Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_DropAsItem(), ModContent.ItemType<Items.Potions.MushroomSkewer>(), 10);
                    Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_DropAsItem(), ModContent.ItemType<Items.SoulCoin>(), 100);

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_DropAsItem(), ItemID.WormholePotion, 5);
                    }
                    chatState = 0;
                    return;
                }
                if (chatState == 9)
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.BotCGift");
                    player.GetModPlayer<tsorcRevampPlayer>().ReceivedGift = true;
                    Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_DropAsItem(), ModContent.ItemType<Items.Potions.MushroomSkewer>(), 10);
                    Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_DropAsItem(), ModContent.ItemType<Items.SoulCoin>(), 100);
                    Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_DropAsItem(), ModContent.ItemType<Items.Potions.Lifegem>(), 10);
                    Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_DropAsItem(), ModContent.ItemType<Items.Potions.StarlightShard>(), 4);

                    if (Main.netMode == NetmodeID.MultiplayerClient)
                    {
                        Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_DropAsItem(), ItemID.WormholePotion, 5);
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
            NPC.spriteDirection = NPC.direction; //she's technically facing the opposite way she's looking but whatevs

            if (tsorcRevampWorld.CustomMap) // If it is our custom map
            {
                NPC.velocity.X = 0; // Don't move left or right

                if (Main.dayTime && Main.player[Main.myPlayer].Distance(NPC.Center) > 2500f) // If day and the player is far away
                {
                    NPC.position = new Vector2(4510.5f * 16, 737 * 16); // Stand under structure
                }
                if (!Main.dayTime && Main.player[Main.myPlayer].Distance(NPC.Center) > 2500f) //If night and the player is far away
                {
                    NPC.position = new Vector2(4489.25f * 16, 732 * 16); // Stand by bonfire
                }

                if (NPC.position.X > 4505f * 16) // If standing under structure
                {
                    NPC.direction = -1;
                }
                if (NPC.position.X < 4505f * 16) // If standing by bonfire
                {
                    NPC.direction = 1;
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

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color lightColor)
        {
            Texture2D eyeTexture = (Texture2D)Mod.Assets.Request<Texture2D>("NPCs/Friendly/EmeraldHerald_Eye");
            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle myrectangle = eyeTexture.Frame(1, 4, 0, eyeFrame);
            if (NPC.spriteDirection == -1)
            {
                spriteBatch.Draw(eyeTexture, NPC.Center - Main.screenPosition, myrectangle, lightColor, NPC.rotation, new Vector2(-3, 11), NPC.scale, effects, 0f);
            }
            else
            {
                spriteBatch.Draw(eyeTexture, NPC.Center - Main.screenPosition, myrectangle, lightColor, NPC.rotation, new Vector2(7, 11), NPC.scale, effects, 0f);
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
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
                else if (idleTimer < 60)
                {
                    NPC.frame.Y = Frame_Wind_1 * frameHeight;
                }
                else if (idleTimer < 90)
                {
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
                else if (idleTimer < 120)
                {
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
                else if (idleTimer < 150)
                {
                    NPC.frame.Y = Frame_Wind_1 * frameHeight;
                }
                else if (idleTimer < 180)
                {
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
                else if (idleTimer < 210)
                {
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
                else if (idleTimer < 240)
                {
                    NPC.frame.Y = Frame_Wind_1 * frameHeight;
                }
                else if (idleTimer < 270)
                {
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
                else if (idleTimer < 300)
                {
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
                else if (idleTimer < 330)
                {
                    NPC.frame.Y = Frame_Wind_1 * frameHeight;
                }
                else if (idleTimer < 360)
                {
                    NPC.frame.Y = Frame_Wind_2 * frameHeight;
                }
                else if (idleTimer < 390)
                {
                    NPC.frame.Y = Frame_Wind_1 * frameHeight;
                }
                else if (idleTimer < 420)
                {
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
                else if (idleTimer < 450)
                {
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
                else if (idleTimer < 480)
                {
                    NPC.frame.Y = Frame_Wind_1 * frameHeight;
                }
                else if (idleTimer < 510)
                {
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
                else if (idleTimer < 540)
                {
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
                else if (idleTimer < 570)
                {
                    NPC.frame.Y = Frame_Wind_1 * frameHeight;
                }
                else if (idleTimer < 600)
                {
                    NPC.frame.Y = Frame_Idle * frameHeight;
                }
            }

            if (idleTimer >= 600)
            {
                NPC.frameCounter += Main.rand.Next(0, 4);

                if (NPC.frameCounter < 30)
                {
                    NPC.frame.Y = Frame_Wind_1 * frameHeight;
                }
                else if (NPC.frameCounter < 60)
                {
                    NPC.frame.Y = Frame_Wind_2 * frameHeight;
                }
                else if (NPC.frameCounter < 90)
                {
                    NPC.frame.Y = Frame_Wind_3 * frameHeight;
                }
                else if (NPC.frameCounter < 120)
                {
                    NPC.frame.Y = Frame_Wind_5 * frameHeight;
                }
                else if (NPC.frameCounter < 150)
                {
                    NPC.frame.Y = Frame_Wind_4 * frameHeight;
                }
                else if (NPC.frameCounter < 180)
                {
                    NPC.frame.Y = Frame_Wind_3 * frameHeight;
                }
                else if (NPC.frameCounter < 210)
                {
                    NPC.frame.Y = Frame_Wind_2 * frameHeight;
                }
                else if (NPC.frameCounter < 240)
                {
                    NPC.frame.Y = Frame_Wind_1 * frameHeight;
                }
                else
                {
                    idleTimer = 0;
                    NPC.frameCounter = 0;
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
