using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Weapons.Magic;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class ShamanElder : ModNPC
    {
        public static List<string> Names = new List<string> {
            "Alo", "Dakota", "Esadowa", "Kai", "Koda", "Lonato", "Micah", "Taregan"
        };
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Shaman Elder");
            Main.npcFrameCount[NPC.type] = 25;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 10;
            NPCID.Sets.AttackFrameCount[NPC.type] = 5;
            NPCID.Sets.DangerDetectRange[NPC.type] = 600;
            NPCID.Sets.AttackType[NPC.type] = 2; //magic
            NPCID.Sets.AttackTime[NPC.type] = 22;
            NPCID.Sets.AttackAverageChance[NPC.type] = 30;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }

        public override List<string> SetNPCNameList()
        {
            return Names;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = 7;
            NPC.damage = 90;
            NPC.defense = 15;
            NPC.lifeMax = 1000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.Guide;

        }

        public override void PostAI()
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Bosses.TheSorrow>()))
            {
                NPC.homeTileX = 75;
                NPC.homeTileY = 522;
                NPC.homeless = false;
            }

            for(int i = 0; i < 5; i++)
            {
                if(UsefulFunctions.IsTileReallySolid(new Vector2(NPC.Center.X / 16 + 1, NPC.Center.Y / 16 + i))){
                    return;
                }
                if(i == 4 && NPC.velocity.X > 0) //Then they're about to walk off the fucking cliff lol
                {
                    NPC.velocity.X = 0;
                }
            }
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add("Man and animal once lived in harmony, until one day a particular tribe grew a sickness of the mind. This tribe, known as the Takers, came to dominate the world, exterminating all other ways of being...");
            chat.Add("Arapaho once told me that all plants are our brothers and sisters. They talk to us and if we listen, we can hear them.");
            chat.Add("I am an animist, like the indigenous tribe who once lived in these lands were, before they were wiped out by the Takers.");
            chat.Add("You must never forget " + Main.LocalPlayer.name + ", you are not separate from nature. You are one with the whole universe.");
            chat.Add("The world is not a pyramid, " + Main.LocalPlayer.name + ", nor is man the top of it. The world is a web, and every strand of the web is connected.");
            chat.Add("Civilized man has grown a great sickness of the mind -- thinks he is superior to all creation. Thinks the world was made for him!");
            chat.Add("Apache said it is better to have less thunder in the mouth and more lightning in the hand.");
            chat.Add("Tuscarora once said they are not dead who live in the hearts they leave behind.");
            if (!tsorcRevampWorld.SuperHardMode && !tsorcRevampWorld.TheEnd)
            {
                chat.Add("If you are able to defeat Attraidies, come and find me. I will have something for you...");
            }
            return chat;
        }
        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
            if (tsorcRevampWorld.SuperHardMode)
            {
                if (chatState == 0)
                {
                    button2 = "Ask about The Abyss";
                }
                else
                {
                    button2 = "Continue...";
                }
            }
        }

        int chatState = 0;
        public override void OnChatButtonClicked(bool firstButton, ref bool shop)
        {
            if (firstButton)
            {
                shop = true;
                return;
            }
            else
            {
                if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                {
                    //This chain of chat messages is for adventure mode!
                    if (chatState == 0)
                    {
                        Main.npcChatText = "Red, it is as I feared. By killing Attraidies, a portal from the Abyss was opened, unleashing even more oppressive forces upon the world." +
                                        "\nThe ancenstors tell me that the portal can be closed, but you must first defeat the 3 Elemental Fiends: one of Water, one of Earth, and one of Fire." +
                                        "\nYou must also defeat 5 more guardians of the Abyss:" +
                                        "\nArtorias, Chaos, The Blight, The Wyvern Mage Shadow, and Seath the Scaleless.";
                        /*
                         Main.npcChatText = "Red, it is as I feared. By killing Attraidies, a portal from the Abyss was opened, unleashing even more oppressive forces upon the world." +
                                        "\nThe ancenstors tell me that the portal can be closed, but you must first defeat the 3 Elemental Fiends: one of Water, one of Earth and one of Fire." +
                                        "\nYou must also defeat 5 more guardians of the Abyss:" +
                                        "\n[c/ffbf00:Artorias], [c/fcff00:Chaos], [c/00ffd4:The Blight], [c/aa00ff:The Wyvern Mage Shadow], and [c/18ffe2:Seath the Scaleless]." +
                                        "\nMy heart despairs for you, Red. It will not be easy. But if you succeed, you will have the strength" +
                                        "\nto face the final guardian. To the ancenstors, he was known as [c/ff6618:Gwyn, Lord of Cinder]. ";
                         * */
                        chatState = 1;
                        return;
                    }
                    if (chatState == 1)
                    {
                        Main.npcChatText = "My heart despairs for you, Red. It will not be easy. But if you succeed, you will have the strength " +
                                        "to face the final guardian. To the ancenstors, he was known as Gwyn, Lord of Cinder." +
                                        "\nGwyn's old tome is buried somewhere beneath the Western sea, but he will surely kill you " +
                                        "if you have not yet gathered the strength obtained from the other guardians of the Abyss.";


                        chatState = 2;
                        return;
                    }
                    if (chatState == 2)
                    {
                        Main.npcChatText = "Have you seen the Lihzahrd Gates scattered across this world? They will lead you towards your goal." +
                                        "\nI would start with the one deep inside the Great Chasm, which leads to the Old One's Tree." +
                                        "\nThere is another, to the East of Elengad's Desert Ruins. The rest, I'm sure you will find. The ancestors will help guide you.";
                        /*
                        Main.npcChatText = "Gwyn's old tome is buried somewhere beneath the Western sea, but he will surely kill you" +
                                        "\nif you have not yet gathered the strength obtained from the other guardians of the Abyss." +
                                        "\nHave you seen the Lihzahrd Gates scattered across this world? They will lead you towards your goal." +
                                        "\nI would start with the one deep inside the Great Chasm, which leads to the Old One's Tree." +
                                        "\nThere is another, to the East of Elengad's Desert Ruins. The rest, I'm sure you will find. The ancestors will help guide you.";
                        */

                        chatState = 3;
                        return;
                    }
                    if (chatState == 3)
                    {
                        Main.npcChatText = "There is one thing you should know about Artorias, and another dark being that now stalks these lands," +
                                    "\nknown as the Witchking." +
                                    "\nBoth The Witchking and Artorias are protected by dark spells, making them practically invincible, but I have heard that " +
                                    "Fire Fiend Marilith and certain Phantoms that roam the skies are rumored to carry blades of fierce magic." +
                                    "\nSuch a blade may just be strong enough to shatter their protection...";
                        /*
                        Main.npcChatText = "There is one thing you should know about [c/ffbf00:Artorias], and another dark being that now stalks these lands," +
                                    "\nknown as the [c/383838:Witchking]." +
                                    "\nBoth The [c/383838:Witchking] and [c/ffbf00:Artorias] are protected by dark spells, making them practically invincible, but I have heard that" +
                                    "\n[c/cffffa:Fire Fiend Marilith] and certain [c/cffffa:Phantoms] that roam the skies are rumored to carry blades of fierce magic." +
                                    "\nSuch a blade may just be strong enough to shatter their protection...";
                        */
                        chatState = 4;
                        return;
                    }

                    if (chatState == 4)
                    {
                        Main.npcChatText = "Good luck, Red.";

                        chatState = 0;
                    }
                }
                else
                {
                    //This chain of chat messages is for sandbox mode!
                    if (chatState == 0)
                    {
                        Main.npcChatText = "To close the seal to the Abyss, you must defeat the 6 lords of The Abyss:" +
                                        "\n[c/ffbf00:Artorias], [c/00ffd4:The Blight], [c/aa00ff:The Wyvern Mage Shadow], " +
                                        "\n[c/fcff00:Chaos], and [c/18ffe2:Seath the Scaleless]." +
                                        "\nWith a lord soul from each of these" +
                                        "\nbeings you will be able to summon the final lord - " +
                                        "\n[c/ff6618:Gwyn, Lord of Cinder].";
                        chatState = 1;
                        return;
                    }
                    if (chatState == 1)
                    {
                        Main.npcChatText = "To craft the summoning item for each " +
                                        "lord, you will need to return to eight familiar places " +
                                        "and collect a unique item from an enemy you will find there: " +
                                        "[c/424bf5:The Western Ocean], [c/888888:The Underground], [c/b942f5:The Corruption], " +
                                        "\n[c/42f56c:The Jungle], [c/6642f5:The Dungeon], [c/eb4034:The Underworld], and [c/42f2f5:The Eastern Ocean].";
                        chatState = 2;
                        return;
                    }
                    if (chatState == 2)
                    {
                        Main.npcChatText = "Defeating [c/ffbf00:Artorias], however, will not be possible without a little knowledge." +
                                    //"Without it I fear you may stand little chance against these terrors... " +
                                    "\nTo find him, you must seek out the [c/383838:Witchking] and restore the strange ring he drops." +
                                    "\nHe will sometimes appear at night, and more often deeper\nunderground, especially in dungeons." +
                                    "\nThe most assured way to find him, however, is to enter the Abyss yourself using the Covanent of Artorias ring.";
                        chatState = 3;
                        return;
                    }


                    if (chatState == 3)
                    {
                        Main.npcChatText = "Both The [c/383838:Witchking] and [c/ffbf00:Artorias] are protected by dark spells, but I have heard that" +
                                    "\n[c/cffffa:Fire Fiend Marilith] and certain [c/cffffa:Phantoms] that roam the skies are rumored to carry blades of fierce magic. " +
                                    "\nSuch a blade may just be strong enough to shatter their protection...";

                        chatState = 0;
                    }
                }
            }
        }

        public override void SetupShop(Chest shop, ref int nextSlot)
        {
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<CosmicWatch>());
            shop.item[nextSlot].shopCustomPrice = 150;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            if (Main.hardMode)
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Summon.NullSpriteStaff>());
                shop.item[nextSlot].shopCustomPrice = 4000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }
            if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Bosses.Okiku.FinalForm.Attraidies>()) || tsorcRevampWorld.SuperHardMode /*just in case*/)
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<CovenantOfArtorias>());
                shop.item[nextSlot].shopCustomPrice = 4000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            }
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (base.NPC.life <= 0)
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Shaman Elder Gore 1").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Knight Gore 2").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Knight Gore 2").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Knight Gore 3").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Knight Gore 3").Type);
                }
            }
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {

            damage = 50;
            knockback = 2f;
            if (Main.hardMode)
            {
                damage = 120;
                knockback = 5f;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                damage = 250;
                knockback = 12f;
            }
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 180;
            randExtraCooldown = 60;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = ModContent.ProjectileType<Projectiles.ShamanBolt>();
            attackDelay = 5;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 2f;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs, int money)
        {
            foreach (Player p in Main.player)
            {
                if (!p.active)
                {
                    continue;
                }
                if (p.statManaMax2 > 60)
                { //is this the best idea? not everyone is going to mindlessly eat every mana crystal they find - was 160, lowered to 80, should probably switch to max life though
                    return true;
                }
            }
            return false;
        }

        public override bool CanGoToStatue(bool toKingStatue)
        {
            return true;
        }
    }
}
