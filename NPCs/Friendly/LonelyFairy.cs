using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.Items.Weapons.Melee.Axes;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class LonelyFairy : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Lonely Fairy");
            Main.npcFrameCount[NPC.type] = 6;
            NPCID.Sets.HatOffsetY[NPC.type] = 3;
        }

        public static List<string> Names = new List<string> {
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.LonelyFairy.Name1"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.LonelyFairy.Name2"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.LonelyFairy.Name3"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.LonelyFairy.Name4"),
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.LonelyFairy.Name5")
        };

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.noGravity = true;
            NPC.friendly = true;
            NPC.width = 22;
            NPC.height = 36;
            NPC.aiStyle = 7; 
            NPC.damage = 90;
            NPC.defense = 15;
            NPC.lifeMax = 1000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.6f;
            NPC.dontTakeDamage = true;
            NPC.scale = 1.15f;
        }

        #region Frames
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = ((Texture2D)TextureAssets.Npc[NPC.type]).Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X < 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;

            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 8.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }
        #endregion

        #region Chat
        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.LonelyFairy.Quote1"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.LonelyFairy.Quote2"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.LonelyFairy.Quote3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.LonelyFairy.Quote4"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.LonelyFairy.Quote5"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.LonelyFairy.Quote6"));
            return chat;
        }
        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                shopName = "Shop";
                return;
            }
        }
        #endregion

        #region AI
        public override void AI()
        {
            Lighting.AddLight((int)(NPC.position.X / 16), (int)(NPC.position.Y / 16), 0.6f, 0.3f, 0.0f);
        }
        #endregion

        #region Shop
        public override void AddShops()
        {
            NPCShop shop = new(NPC.type);
            shop.Add(new Item(ItemID.LovePotion)
            {
                shopCustomPrice = 10,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.StinkPotion)
            {
                shopCustomPrice = 10,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId,
            });

            shop.Add(new Item(ModContent.ItemType<Items.Potions.GreaterRestorationPotion>())
            {
                shopCustomPrice = 70,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedPlantera);

            shop.Add(new Item(ModContent.ItemType<Items.Potions.SupremeManaPotion>())
            {
                shopCustomPrice = 65,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedPlantera);

            shop.Add(new Item(ItemID.StrangePlant1)
            {
                shopCustomPrice = 35,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.StrangePlant2)
            {
                shopCustomPrice = 35,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.StrangePlant3)
            {
                shopCustomPrice = 35,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.StrangePlant4)
            {
                shopCustomPrice = 35,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.BedazzledNectar)
            {
                shopCustomPrice = 300,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.SoulofLight)
            {
                shopCustomPrice = 40,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.SoulofNight)
            {
                shopCustomPrice = 40,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.SoulofFlight)
            {
                shopCustomPrice = 40,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.WyvernMageDowned"), () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.WyvernMage.WyvernMage>()))));

            shop.Add(new Item(ItemID.SoulofMight)
            {
                shopCustomPrice = 60,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Condition.DownedDestroyer"), () => NPC.downedMechBoss1));

            shop.Add(new Item(ItemID.SoulofFright)
            {
                shopCustomPrice = 60,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Condition.DownedSkeletronPrime"), () => NPC.downedMechBoss3));

            shop.Add(new Item(ItemID.SoulofSight)
            {
                shopCustomPrice = 60,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Condition.DownedTwins"), () => NPC.downedMechBoss2));
            
            shop.Add(new Item(ModContent.ItemType<Items.Materials.CursedSoul>())
            {
                shopCustomPrice = 100,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.AbysmalOolacileSorcererDowned"), () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>()))));

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Shortswords.BarrowBlade>())
            {
                shopCustomPrice = 2000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.SuperHardMode));

            shop.Register();
        }
        #endregion
    }
}