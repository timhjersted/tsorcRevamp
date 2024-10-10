using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class TibianArcher : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Tibian Archer");
            Main.npcFrameCount[NPC.type] = 26;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 10;
            NPCID.Sets.AttackFrameCount[NPC.type] = 5;
            NPCID.Sets.DangerDetectRange[NPC.type] = 700;
            NPCID.Sets.AttackType[NPC.type] = 1; // 0 is throwing, 1 is shooting, 2 is magic, 3 is melee
            NPCID.Sets.AttackTime[NPC.type] = 40;
            NPCID.Sets.AttackAverageChance[NPC.type] = 10;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }

        public static List<string> Names = new List<string>
        {
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Name1"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Name2"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Name3"),
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Name4")
        };

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
            NPC.defense = 15;
            NPC.lifeMax = 1000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.3f;
            AnimationType = NPCID.Guide;
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Quote1"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Quote2"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Quote3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Quote4"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Quote5"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Quote6"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Quote7"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.TibianArcher.Quote8"));
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

        public override void AddShops()
        {
            NPCShop shop = new(NPC.type);
            shop.Add(new Item(ModContent.ItemType<Items.ItemCrates.BoltCrate>())
            {
                shopCustomPrice = 8,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.ItemCrates.WoodenArrowCrate>())
            {
                shopCustomPrice = 6,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Ranged.LeatherHelmet>())
            {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Ranged.LeatherArmor>())
            {
                shopCustomPrice = 50,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.Ranged.LeatherGreaves>())
            {
                shopCustomPrice = 33,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.LesserHealingPotion)
            {
                shopCustomPrice = 4,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.Safe)
            {
                shopCustomPrice = 250,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Ranged.Specialist.Crossbow>())
            {
                shopCustomPrice = 75,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.ItemCrates.FrostburnArrowCrate>())
            {
                shopCustomPrice = 12,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            },
            Condition.DownedEyeOfCthulhu);

            shop.Add(new Item(ModContent.ItemType<Items.ItemCrates.UnholyArrowCrate>())
            {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            },
            Condition.DownedEyeOfCthulhu);



            shop.Add(new Item(ModContent.ItemType<Items.Accessories.Ranged.InfinityEdge>())
            {
                shopCustomPrice = 1300,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            },
            Condition.DownedEyeOfCthulhu);



            shop.Add(new Item(ModContent.ItemType<Items.ItemCrates.RoyalThrowingSpearCrate>())
            {
                shopCustomPrice = 12,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            },
            Condition.DownedEowOrBoc);



            shop.Add(new Item(ModContent.ItemType<Items.ItemCrates.MeteorShotCrate>())
            {
                shopCustomPrice = 30,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Gaibon>()))));



            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Magic.EnchantedThrowingSpear>())
            {
                shopCustomPrice = 2200,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.JungleWyvern.JungleWyvernHead>()))));

            shop.Add(new Item(ModContent.ItemType<Items.Ammo.PowerBolt>())
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.JungleWyvern.JungleWyvernHead>()))));

            shop.Add(new Item(ItemID.JestersArrow)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.JungleWyvern.JungleWyvernHead>()))));

            shop.Add(new Item(ItemID.HellfireArrow)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.JungleWyvern.JungleWyvernHead>()))));

            shop.Add(new Item(ItemID.HolyArrow)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.TheSorrow>()))));

            shop.Add(new Item(ItemID.ChlorophyteArrow)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedMechBossAny);

            shop.Register();
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (base.NPC.life <= 0) //even though npcs are immortal
            {
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Archer Gore 1").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Archer Gore 2").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Archer Gore 2").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Archer Gore 3").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Archer Gore 3").Type);
                }
            }
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            if (Main.hardMode)
            {
                damage = 44;
                knockback = 6f;
            }
            else
            {
                damage = 22;
                knockback = 5f;
            }
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 5;
            randExtraCooldown = 20;
        }
        public override void DrawTownAttackGun(ref Texture2D item, ref Rectangle itemFrame, ref float scale, ref int horizontalHoldoutOffset)/* tModPorter Note: closeness is now horizontalHoldoutOffset, use 'horizontalHoldoutOffset = Main.DrawPlayerItemPos(1f, itemtype) - originalClosenessValue' to adjust to the change. See docs for how to use hook with an item type. */
        {
            //item = ModContent.ItemType<Items.Weapons.Ranged.Bows.ElfinBow>();
            scale = 1f;
            horizontalHoldoutOffset = 20;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = ProjectileID.HellfireArrow;
            attackDelay = 35;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 20f;
            gravityCorrection = 25f;
            randomOffset = 0f;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)/* tModPorter Suggestion: Copy the implementation of NPC.SpawnAllowed_Merchant in vanilla if you to count money, and be sure to set a flag when unlocked, so you don't count every tick. */
        {
            foreach (Player p in Main.player)
            {
                if (!p.active)
                {
                    continue;
                }
                if (p.statDefense > 0)
                {
                    return true;
                }
            }
            return false;
        }

        public override bool CanGoToStatue(bool toQueenStatue)
        {
            return true;
        }
    }
}
