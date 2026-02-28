using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Weapons.Melee.Axes;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Items.Weapons.Melee.Spears;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class Dwarf : ModNPC
    {
        public static List<string> Names = new List<string> {
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name1"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name2"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name3"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name4"),
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name5"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name6"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name7"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name8"),
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name9"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name10"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name11")
        };

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dwarf");
            Main.npcFrameCount[NPC.type] = 25;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 60;
            NPCID.Sets.AttackType[NPC.type] = 3;
            NPCID.Sets.AttackTime[NPC.type] = 18;
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
            NPC.damage = 50;
            NPC.defense = 45;
            NPC.lifeMax = 300;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.DyeTrader;
        }

        #region Town Spawn
        public override bool CanTownNPCSpawn(int numTownNPCs)/* tModPorter Suggestion: Copy the implementation of NPC.SpawnAllowed_Merchant in vanilla if you to count money, and be sure to set a flag when unlocked, so you don't count every tick. */
        {
            foreach (Player p in Main.player)
            {
                if (!p.active)
                {
                    continue;
                }
                if (p.statDefense > 8)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Chat
        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote1"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote2"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote4"));

            if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) && !NPC.downedMechBoss1)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote5"));
            }
            if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) && NPC.downedMechBoss1)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote6"));
            }
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) && !NPC.downedMechBoss1)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote7"));
            }
            return chat;
        }
        #endregion

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
            button2 = Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.ContractButton");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            if (firstButton)
            {
                shopName = "Shop";
                return;
            }

            Player player = Main.LocalPlayer;

            int givenContract = tsorcRevampWorld.DwarvenContractsGiven;

            if (givenContract >= 10)
            {
                Main.npcChatText = Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.ContractMax");
                return;
            }

            if (!player.HasItem(ModContent.ItemType<DwarvenContract>()))
            {
                WeightedRandom<string> chat = new WeightedRandom<string>();
                Main.npcChatText = Language.GetTextValue($"Mods.tsorcRevamp.NPCs.Dwarf.ContractQuote1");
                Main.npcChatText = Language.GetTextValue($"Mods.tsorcRevamp.NPCs.Dwarf.ContractQuote2");
                Main.npcChatText = Language.GetTextValue($"Mods.tsorcRevamp.NPCs.Dwarf.ContractQuote3");
                return;
            }

            // Consume the Contract
            player.ConsumeItem(ModContent.ItemType<DwarvenContract>());
            givenContract++;
            tsorcRevampWorld.DwarvenContractsGiven = givenContract;

            // Rewards
            switch (givenContract)
            {
                //PHM section
                case 1:  
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldCoin, 1);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<SoulCoin>(), 150); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<OldHalberd>(), 1); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.LifeCrystal, 1);
                    break;

                case 2:  
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldCoin, 5);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<SoulCoin>(), 200);  
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.EndurancePotion, 2);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.LifeCrystal, 2);
                    break;

                case 3:  
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldCoin, 10);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<SoulCoin>(), 100); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<NamelessSoldierSoul>(), 1); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<Humanity>(), 2); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<EternalCrystal>(), 1); 
                    break;

                case 4:  
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldCoin, 15);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<SoulCoin>(), 320); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<LostUndeadSoul>(), 2); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.LuckPotion, 2);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldenDelight, 1);
                    break;

                //HM section
                case 5:  
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldCoin, 20);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<SoulCoin>(), 500); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<ProudKnightSoul>(), 1); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.RagePotion, 1);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.WrathPotion, 1);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<PurgingStone>(), 1); 
                    break;

                case 6:  
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldCoin, 30);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<SoulCoin>(), 1000); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<Humanity>(), 2); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<PurgingStone>(), 1); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<EternalCrystal>(), 1); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.LifeFruit, 2);
                    break;

                case 7:  
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldCoin, 40);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<SoulCoin>(), 1000); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<ProudKnightSoul>(), 1); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldenDelight, 1);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.LuckPotionLesser, 1);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.LuckPotion, 1);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.LuckPotionGreater, 1);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.LifeFruit, 2);
                    break;

                case 8: 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldCoin, 50); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<SoulCoin>(), 1000); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<BraveWarriorSoul>(), 1); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<PurgingStone>(), 2);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<DemonDrugPotion>(), 2); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<ArmorDrugPotion>(), 2);  
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<EternalCrystal>(), 1); 
                    break;

                //SHM section
                case 9: 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldCoin, 75);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<SoulCoin>(), 1250); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<FlameOfTheAbyss>(), 5);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.LifeforcePotion, 2);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.LuckPotionGreater, 2);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<Trinity>());
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<Trinity>());
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<Trinity>());
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<Trinity>());
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<HeroSoul>(), 1); 
                    break;

                case 10: 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.PlatinumCoin, 1);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<SoulCoin>(), 1500); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<HeroSoul>(), 2); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<VaultOfEndlessGreed>());
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<VaultOfEndlessGreed>());
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<VaultOfEndlessGreed>());
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<VaultOfEndlessGreed>());
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<HolyWarElixir>(), 5);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<PurgingStone>(), 3);
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ModContent.ItemType<EternalCrystal>(), 2); 
                    player.QuickSpawnItem(NPC.GetSource_Loot(), ItemID.GoldenDelight, 2);
                    break;
                // After 10 contracts given, no more rewards or anything
            }

            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<DwarvenGuard>());

            Main.npcChatText = givenContract == 10
                ? Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.FinalContractGiven")
                : Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.ContractGiven");
        }

        #region Setup Shop
        public override void AddShops()
        {
            NPCShop shop = new(NPC.type);

            shop.Add(new Item(ItemID.Flipper)
            {
                shopCustomPrice = 100,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.FairyBell)
            {
                shopCustomPrice = 100,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.Silk)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.HealingPotion)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.StuddedLeatherHelmet>())
            {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.StuddedLeatherArmor>())
            {
                shopCustomPrice = 40,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.StuddedLeatherGreaves>())
            {
                shopCustomPrice = 33,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });



            shop.Add(new Item(ItemID.DivingHelmet)
            {
                shopCustomPrice = 100,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEowOrBoc);

            shop.Add(new Item(ItemID.StickyBomb)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEowOrBoc);

            shop.Add(new Item(ItemID.TungstenBar)
            {
                shopCustomPrice = 5,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEowOrBoc);     

            shop.Add(new Item(ItemID.MeteoriteBar)
            {
                shopCustomPrice = 8,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.SlograGaibonDowned"), () => (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Gaibon>())) || tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Slogra>())))));

            shop.Add(new Item(ItemID.HellstoneBar)
            {
                shopCustomPrice = 10,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.Hardmode);

            shop.Add(new Item(ItemID.CobaltBar)
            {
                shopCustomPrice = 15,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.RageDowned"), () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>()))));

            shop.Add(new Item(ItemID.PalladiumBar)
            {
                shopCustomPrice = 16,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.RageDowned"), () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheRage>()))));
            
            shop.Add(new Item(ItemID.MythrilBar)
            {
                shopCustomPrice = 20,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.SorrowDowned"), () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>()))));

            shop.Add(new Item(ItemID.OrichalcumBar)
            {
                shopCustomPrice = 21,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.SorrowDowned"), () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>()))));

            shop.Add(new Item(ItemID.AdamantiteBar)
            {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.HunterDowned"), () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>()))));

            shop.Add(new Item(ItemID.TitaniumBar)
            {
                shopCustomPrice = 26,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.HunterDowned"), () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>()))));

            shop.Add(new Item(ItemID.HallowedBar)
            {
                shopCustomPrice = 35,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedMechBossAny);

            shop.Add(new Item(ItemID.ChlorophyteBar)
            {
                shopCustomPrice = 40,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedMechBossAny);

            shop.Add(new Item(ItemID.ShroomiteBar)
            {
                shopCustomPrice = 60,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedGolem);

            shop.Add(new Item(ItemID.SpectreBar)
            {
                shopCustomPrice = 60,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedGolem);

            shop.Add(new Item(ItemID.LunarBar)
            {
                shopCustomPrice = 80,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedMoonLord);

            //Thorium compatibility section
            if (ModLoader.TryGetMod("ThoriumMod", out Mod thorium))
            {
                shop.Add(new Item(thorium.Find<ModItem>("ThoriumBar").Type)
                {
                    shopCustomPrice = 6,                    
                    shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                }, Condition.DownedEyeOfCthulhu);  

                shop.Add(new Item(thorium.Find<ModItem>("AquaiteBar").Type)
                {
                    shopCustomPrice = 8,                    
                    shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                }, Condition.DownedEowOrBoc);  

                shop.Add(new Item(thorium.Find<ModItem>("aDarksteelAlloy").Type)
                {
                    shopCustomPrice = 10,                    
                    shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                }, Condition.DownedSkeletron);  

                shop.Add(new Item(thorium.Find<ModItem>("IllumiteIngot").Type)
                {
                    shopCustomPrice = 50,                    
                    shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                }, Condition.DownedPlantera);  

                shop.Add(new Item(thorium.Find<ModItem>("Leviathan").Type)
                {
                    shopCustomPrice = 700,                    
                    shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                }, Condition.Hardmode);
            }  

            shop.Add(new Item(ModContent.ItemType<Items.Materials.ImpHead>())
            {
                shopCustomPrice = 6000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.HunterDowned"), () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>()))));

            shop.Register();
        }
        #endregion

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 25;
            knockback = 4f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 30;
            randExtraCooldown = 30;
        }

        public override void DrawTownAttackSwing(ref Texture2D item, ref Rectangle itemFrame, ref int itemSize, ref float scale, ref Vector2 offset)
        {
            item = (Texture2D)TextureAssets.Item[ModContent.ItemType<Items.Weapons.Melee.Hammers.AncientWarhammer>()];
            itemSize = 38;
        }

        public override void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight)
        {
            itemWidth = 38;
            itemHeight = 38;
        }

        public override bool CanGoToStatue(bool toKingStatue)
        {
            return true;
        }
    }
}