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
            NPC.lifeMax = 10000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 1f;
        }


        #region Chat Functionality Stuff

        // chatState is stored in the player's ModPlayer (heraldChatState) so it persists across
        // world re-entries and NPC respawns. State map:
        //   0   = pre-sequence (greeting / tome offer pending)
        //   1-6 = tip sequence (Tip5→Tip10)
        //   7   = ReceiveGift text shown, awaiting acceptance
        //   8   = gift received, idle random tips
        //   20  = tome-offer text showing, awaiting "Receive tome" click

        public override string GetChat()
        {
            Player player = Main.LocalPlayer;
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            // Auto-migrate: players who received the gift before heraldChatState was saved start at 0
            // but should be treated as post-gift (state 8).
            if (modPlayer.ReceivedGift && modPlayer.heraldChatState < 8)
                modPlayer.heraldChatState = 8;

            if (!modPlayer.FirstEncounter)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ashen-one") with { Volume = 0.5f }, NPC.Center);
                modPlayer.FirstEncounter = true;
                return Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.FirstEncounter");
            }

            if (player.HasItem(ModContent.ItemType<Items.EstusFlaskShard>()) && player.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 12)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ashen-one") with { Volume = 0.5f }, NPC.Center);
                return Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.HasShard");
            }

            // Sequential tip states: reopening the dialog shows the tip the player is currently on.
            switch (modPlayer.heraldChatState)
            {
                case 20:
                    return Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.TomeOffer");
                case 1:
                    return Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip5");
                case 2:
                    return Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip6");
                case 3:
                    return Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip7");
                case 4:
                    return Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip8");
                case 5:
                    return Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip9");
                case 6:
                    return Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip10");
                case 7:
                    return Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.ReceiveGift");
            }

            // State 0 (pre-sequence) and state 8+ (post-gift): random tips.
            WeightedRandom<string> chat = new WeightedRandom<string>();
            if (!modPlayer.ReceivedGift)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ashen-one") with { Volume = 0.5f }, NPC.Center);
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.RewardTip"), 4);
            }
            if (!tsorcRevampWorld.SuperHardMode)
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.AttraidiesTip"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip1"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip2"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip4"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.EasterEgg"), 0.05);
            return chat;
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            Player player = Main.LocalPlayer;
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            button = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.button1");

            // Shard reinforcement takes priority regardless of sequence state.
            if (player.HasItem(ModContent.ItemType<Items.EstusFlaskShard>()) && player.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 12)
            {
                button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.button2v3");
                return;
            }

            int s = modPlayer.heraldChatState;

            // Tome-offer step.
            if (s == 20)
            {
                button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.button2v4");
                return;
            }

            // Gift-acceptance step.
            if (s == 7)
            {
                button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.button2v2");
                return;
            }

            // Everything else (pre-sequence, tip steps, post-gift idle).
            button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.button2v1");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            Player player = Main.LocalPlayer;
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();

            if (firstButton)
            {
                Main.playerInventory = true;
                Main.npcChatText = "";
                ModContent.GetInstance<tsorcRevamp>().EmeraldHeraldUserInterface.SetState(new UI.EmeraldHeraldUI());
                return;
            }

            // Shard reinforcement takes priority at any sequence state.
            if (player.HasItem(ModContent.ItemType<Items.EstusFlaskShard>()) && player.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 12)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item37);
                Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.BringShards");
                int shardIndex = player.FindItem(ModContent.ItemType<Items.EstusFlaskShard>());
                if (player.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax < 12)
                {
                    player.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax += 1;
                    player.GetModPlayer<tsorcRevampCeruleanPlayer>().ceruleanChargesMax += 3;
                    if (player.inventory[shardIndex].stack == 1) player.inventory[shardIndex].TurnToAir();
                    else player.inventory[shardIndex].stack--;
                    if (Main.netMode != NetmodeID.Server)
                    {
                        Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.EstusUpgrade") + player.GetModPlayer<tsorcRevampEstusPlayer>().estusChargesMax, Color.OrangeRed);
                        Main.NewText(Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.CeruleanUpgrade") + player.GetModPlayer<tsorcRevampCeruleanPlayer>().ceruleanChargesMax, Color.RoyalBlue);
                    }
                }
                return;
            }

            // Pre-sequence: offer the tome first, then start the tips.
            if (modPlayer.heraldChatState == 0)
            {
                if (!modPlayer.ReceivedHuntingTome)
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.TomeOffer");
                    modPlayer.heraldChatState = 20;
                    return;
                }
                // Already has tome — start the tip sequence.
                Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Tip5");
                modPlayer.heraldChatState = 1;
                return;
            }

            // Tome acceptance.
            if (modPlayer.heraldChatState == 20)
            {
                Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.TomeGiven");
                modPlayer.ReceivedHuntingTome = true;
                player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.BossItems.BossRematchTome>());
                modPlayer.heraldChatState = 1;
                return;
            }

            // Tip sequence steps 1-5: advance one step and show the next tip.
            if (modPlayer.heraldChatState >= 1 && modPlayer.heraldChatState <= 5)
            {
                modPlayer.heraldChatState++;
                string[] tipKeys = { "Tip5", "Tip6", "Tip7", "Tip8", "Tip9", "Tip10" };
                Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald." + tipKeys[modPlayer.heraldChatState - 1]);
                return;
            }

            // Last tip (Tip10, state 6) → transition to gift offer.
            if (modPlayer.heraldChatState == 6)
            {
                Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.ReceiveGift");
                modPlayer.heraldChatState = 7;
                return;
            }

            // Gift acceptance (state 7).
            if (modPlayer.heraldChatState == 7)
            {
                modPlayer.ReceivedGift = true;
                modPlayer.heraldChatState = 8;

                if (modPlayer.BearerOfTheCurse)
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.BotCGift");
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.Potions.MushroomSkewer>(), 10);
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.SoulCoin>(), 100);
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.Potions.Lifegem>(), 10);
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.Potions.StarlightShard>(), 4);
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.AdventurersCard>());
                }
                else if (modPlayer.Unkindled)
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.UnkindledGift");
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.Potions.MushroomSkewer>(), 10);
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.SoulCoin>(), 100);
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.Potions.Lifegem>(), 5);
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.AdventurersCard>());
                }
                else
                {
                    Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Gift");
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.Potions.MushroomSkewer>(), 10);
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.SoulCoin>(), 100);
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ModContent.ItemType<Items.AdventurersCard>());
                }

                if (Main.netMode == NetmodeID.MultiplayerClient)
                    player.QuickSpawnItem(player.GetSource_DropAsItem(), ItemID.WormholePotion, 5);
                return;
            }

            // Post-gift idle (state 8+).
            Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.EmeraldHerald.Luck");
        }


        #endregion


        #region AI and Spawning


        public override void AI()
        {
            NPC.spriteDirection = NPC.direction; //she's technically facing the opposite way she's looking but whatevs

            // Apply StoryTime ambient effect while the local player is in conversation with her.
            if (Main.netMode != NetmodeID.Server && Main.LocalPlayer.talkNPC == NPC.whoAmI)
                Main.LocalPlayer.AddBuff(ModContent.BuffType<Buffs.StoryTime>(), 2);

            if (tsorcRevampWorld.CustomMap) // If it is our custom map
            {
                NPC.velocity.X = 0; // Don't move left or right

                if (Main.dayTime && Main.player[Main.myPlayer].Distance(NPC.Center) > 2500f) // If day and the player is far away
                {
                    //Legacy 2000-space; mapped on the expanded world (identity elsewhere). Without this she'd snap
                    //back to the un-shifted legacy Y every time this fires — the cause of her floating 200 tiles high.
                    NPC.position = ExpandedWorldTransform.MapWorld(new Vector2(4510.5f, 737) * 16); // Stand under structure
                }
                if (!Main.dayTime && Main.player[Main.myPlayer].Distance(NPC.Center) > 2500f) //If night and the player is far away
                {
                    NPC.position = ExpandedWorldTransform.MapWorld(new Vector2(4489.25f, 732) * 16); // Stand by bonfire
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
