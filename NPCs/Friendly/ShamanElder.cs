using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.BossItems;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class ShamanElder : ModNPC
    {
        public static List<string> Names = new List<string>
        {
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Name1"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Name2"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Name3"),
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Name4"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Name5"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Name6"),
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Name7"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Name8")
        };
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Shaman Elder");
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
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.TheSorrow>())))
            {
                NPC.homeTileX = 75;
                NPC.homeTileY = 520;
                NPC.homeless = false;
            }

            for (int i = 0; i < 5; i++)
            {
                if (UsefulFunctions.IsTileReallySolid(new Vector2(NPC.Center.X / 16 + 1, NPC.Center.Y / 16 + i)))
                {
                    return;
                }
                if (i == 4 && NPC.velocity.X > 0) //Then they're about to walk off the fucking cliff lol
                {
                    NPC.velocity.X = 0;
                }
            }
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Quote1"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Quote2"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Quote3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Quote4Part1") + Main.LocalPlayer.name + Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Quote4Part2"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Quote5Part1") + Main.LocalPlayer.name + Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Quote5Part2"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Quote6"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Quote7"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.Quote8"));
            if (!tsorcRevampWorld.SuperHardMode && !tsorcRevampWorld.TheEnd)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.PreAttraidies1"));
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.PreAttraidies2"));
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
                    button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.button2v1");
                }
                else
                {
                    button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.button2v2");
                }
            }
        }

        int chatState = 0;
        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                shopName = "Shop";
                return;
            }
            else
            {
                if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                {
                    //This chain of chat messages is for adventure mode!
                    if (chatState == 0)
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.AdvModeSHMQuote1"); //[c/fcff00:Chaos]
                        chatState = 1;
                        return;
                    }
                    if (chatState == 1)
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.AdvModeSHMQuote2");
                        chatState = 2;
                        return;
                    }
                    if (chatState == 2)
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.AdvModeSHMQuote3");
                        chatState = 3;
                        return;
                    }
                    if (chatState == 3)
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.AdvModeSHMQuote4");
                        chatState = 4;
                        return;
                    }

                    if (chatState == 4)
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.AdvModeSHMQuote5");

                        chatState = 0;
                    }
                }
                else
                {
                    //This chain of chat messages is for sandbox mode!
                    if (chatState == 0)
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.SandboxSHMQuote1");
                        chatState = 1;
                        return;
                    }
                    if (chatState == 1)
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.SandboxSHMQuote2");
                        chatState = 2;
                        return;
                    }
                    if (chatState == 2)
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.SandboxSHMQuote3");
                        chatState = 3;
                        return;
                    }


                    if (chatState == 3)
                    {
                        Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.ShamanElder.SandboxSHMQuote4");

                        chatState = 0;
                    }
                }
            }
        }

        public override void AddShops()
        {
            NPCShop shop = new(NPC.type);

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Summon.OldChainCoif>())
            {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Summon.OldChainArmor>())
            {
                shopCustomPrice = 50,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Summon.OldChainGreaves>())
            {
                shopCustomPrice = 33,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<CosmicWatch>())
            {
                shopCustomPrice = 5,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<BossRematchTome>())
            {
                shopCustomPrice = 5,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });



            shop.Add(new Item(ModContent.ItemType<Items.Accessories.Defensive.CovenantOfArtorias>())
            {
                shopCustomPrice = 4000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.SuperHardMode));


            shop.Register();
        }

        public override void HitEffect(NPC.HitInfo hit)
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

        public override bool CanTownNPCSpawn(int numTownNPCs)/* tModPorter Suggestion: Copy the implementation of NPC.SpawnAllowed_Merchant in vanilla if you to count money, and be sure to set a flag when unlocked, so you don't count every tick. */
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
