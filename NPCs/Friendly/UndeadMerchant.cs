using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;
using tsorcRevamp.Buffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Accessories.Defensive.Shields;
using tsorcRevamp.Content.Items.Armor;
using tsorcRevamp.Content.Items.ItemCrates;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.RadiantLifegem;
using tsorcRevamp.Content.Items.Weapons.Classless;
using tsorcRevamp.Content.Projectiles;
using GreenBlossom = tsorcRevamp.Content.Items.Potions.GreenBlossom;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class UndeadMerchant : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Undead Merchant");
            Main.npcFrameCount[NPC.type] = 26;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 200;
            NPCID.Sets.AttackType[NPC.type] = 0;
            NPCID.Sets.AttackTime[NPC.type] = 18;
            NPCID.Sets.AttackAverageChance[NPC.type] = 10;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }
        public override List<string> SetNPCNameList()
        {
            return new List<string> { Language.GetTextValue("Mods.tsorcRevamp.NPCs.UndeadMerchant.Name1") };
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
            NPC.lifeMax = 7500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.SkeletonMerchant;
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.UndeadMerchant.Quote1"), 1.5);
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.UndeadMerchant.Quote2"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.UndeadMerchant.Quote3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.UndeadMerchant.Quote4"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.UndeadMerchant.Quote5"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.UndeadMerchant.Quote6"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.UndeadMerchant.Quote7"));

            return chat;
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            shopName = "Shop";
            return;
        }

        public override void AddShops()
        {
            NPCShop shop = new(NPC.type);
            shop.Add(new Item(ModContent.ItemType<CharcoalPineResin>())
            {
                shopCustomPrice = 100,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<OilPot>())
            {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Firebomb>())
            {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<BloodredMossClump>())
            {
                shopCustomPrice = 20,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<GlowingMushroomSkewer>())
            {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<HealingElixir>())
            {
                shopCustomPrice = 150,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<GreenBlossom>())
            {
                shopCustomPrice = 50,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.BattlePotion)
            {
                shopCustomPrice = 150,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.WormholePotion)
            {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<IronShield>())
            {
                shopCustomPrice = 1000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<HollowSoldierHelmet>())
            {
                shopCustomPrice = 1125,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<HollowSoldierBreastplate>())
            {
                shopCustomPrice = 1875,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<HollowSoldierWaistcloth>())
            {
                shopCustomPrice = 1500,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });



            shop.Add(new Item(ModContent.ItemType<ThrowingKnifeCrate>())
            {
                shopCustomPrice = 50,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEyeOfCthulhu);



            shop.Add(new Item(ItemID.VilePowder)
            {
                shopCustomPrice = 5,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEowOrBoc);



            shop.Add(new Item(ItemID.SlimySaddle)
            {
                shopCustomPrice = 5000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.Hardmode);

            shop.Add(new Item(ModContent.ItemType<RadiantLifegemItem>())
            {
                shopCustomPrice = 300,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.Hardmode);



            shop.Add(new Item(ModContent.ItemType<BlackFirebomb>())
            {
                shopCustomPrice = 250,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedDestroyer);



            shop.Add(new Item(ItemID.QueenSlimeMountSaddle)
            {
                shopCustomPrice = 10000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedSkeletronPrime);



            shop.Add(new Item(ModContent.ItemType<PurgingStone>())
            {
                shopCustomPrice = 50000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition("", () => tsorcRevampWorld.SuperHardMode));



            shop.Add(new Item(ModContent.ItemType<MaskOfTheChild>())
            {
                shopCustomPrice = 5000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.BloodMoon);

            shop.Add(new Item(ModContent.ItemType<MaskOfTheFather>())
            {
                shopCustomPrice = 5000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.BloodMoon);

            shop.Add(new Item(ModContent.ItemType<MaskOfTheMother>())
            {
                shopCustomPrice = 5000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.BloodMoon);

            shop.Register();
        }
        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            if (Main.hardMode)
            {
                damage = 100;
                knockback = 7f;
            }
            else
            {
                damage = 50;
                knockback = 5f;
            }
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 50;
            randExtraCooldown = 30;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = ModContent.ProjectileType<FirebombProj>();
            attackDelay = 5;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 6.5f;
            gravityCorrection = 30f;
            randomOffset = 0f;
        }

        public override bool CanTownNPCSpawn(int numTownNPCs)/* tModPorter Suggestion: Copy the implementation of NPC.SpawnAllowed_Merchant in vanilla if you to count money, and be sure to set a flag when unlocked, so you don't count every tick. */
        {
            // Moves in once any player carries 750 Dark Souls (the old 150 Soul Coins). Souls live in the soul slot,
            // which reaches the server through SyncSoulSlot, so this check also works where it runs: the server.
            const int SoulsToMoveIn = 750;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active)
                {
                    continue;
                }

                Item soulSlotItem = player.GetModPlayer<tsorcRevampPlayer>().SoulSlot.Item;
                bool hasEnoughSouls = !soulSlotItem.IsAir && soulSlotItem.stack >= SoulsToMoveIn;

                if (NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3 || hasEnoughSouls || player.HasBuff(ModContent.BuffType<Bonfire>()))
                {
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
