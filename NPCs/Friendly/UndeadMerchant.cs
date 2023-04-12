using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;
using tsorcRevamp.Buffs;

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
        public override List<string> SetNPCNameList() {
            return new List<string> { "Uldred" };
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
            NPC.knockBackResist = 0.5f;
            AnimationType = NPCID.SkeletonMerchant;
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add("Well, now... You seem to have your wits about you, hmm? Then you are a welcome customer! I trade for souls. Everything's for sale! Nee hee hee hee hee!", 1.5);
            chat.Add("I hope you've brought plenty of souls? Nee hee hee hee hee!");
            chat.Add("Oh, there you are. Where have you been hiding? I guessed you'd hopped the twig for certain. Bah, shows what I know! Nee hee hee hee!");
            chat.Add("Oh? Still not popped your clogs?");
            chat.Add("Oh, there you are. Still keeping your marbles all together? Then, go ahead, don't be a nitwit. Never hurts to splurge when your days are numbered! Nee hee hee hee hee!");
            chat.Add("Eh? I'm not here to chit-chat. We talk business, or we talk nothing at all.");
            chat.Add("[c/ffbf00:If our paths cross again, I may have new items for sale. Don't ask where I got'em!]");

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

        public override void ModifyActiveShop(string shopName, Item[] items)
        {/*
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.CharcoalPineResin>());
            shop.item[nextSlot].shopCustomPrice = 20;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Throwing.Firebomb>());
            shop.item[nextSlot].shopCustomPrice = 5;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.BloodredMossClump>());
            shop.item[nextSlot].shopCustomPrice = 4;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Potions.Lifegem>());
            shop.item[nextSlot].shopCustomPrice = 20;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Potions.GlowingMushroomSkewer>());
            shop.item[nextSlot].shopCustomPrice = 5;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Potions.HealingElixir>());
            shop.item[nextSlot].shopCustomPrice = 30;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Potions.GreenBlossom>());
            shop.item[nextSlot].shopCustomPrice = 30;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.BattlePotion);
            shop.item[nextSlot].shopCustomPrice = 30;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.WormholePotion);
            shop.item[nextSlot].shopCustomPrice = 20;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Accessories.Defensive.IronShield>());
            shop.item[nextSlot].shopCustomPrice = 200;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.HollowSoldierHelmet>());
            shop.item[nextSlot].shopCustomPrice = 100;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.HollowSoldierBreastplate>());
            shop.item[nextSlot].shopCustomPrice = 100;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.HollowSoldierWaistcloth>());
            shop.item[nextSlot].shopCustomPrice = 100;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;

            if (NPC.downedBoss1) //EoC
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ItemCrates.ThrowingKnifeCrate>());
                shop.item[nextSlot].shopCustomPrice = 10;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }

            if (NPC.downedBoss2) //EoW/BoC(?)
            {
                shop.item[nextSlot].SetDefaults(ItemID.VilePowder);
                shop.item[nextSlot].shopCustomPrice = 1;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }

            if (Main.hardMode)
            {
                shop.item[nextSlot].SetDefaults(ItemID.SlimySaddle);
                shop.item[nextSlot].shopCustomPrice = 1000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Potions.RadiantLifegem>());
                shop.item[nextSlot].shopCustomPrice = 60;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }

            if (NPC.downedMechBoss1) //The Destroyer
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Throwing.BlackFirebomb>());
                shop.item[nextSlot].shopCustomPrice = 50;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                
            }

            if (NPC.downedMechBoss3) //Skeletron Prime
            {
                shop.item[nextSlot].SetDefaults(ItemID.QueenSlimeMountSaddle);
                shop.item[nextSlot].shopCustomPrice = 2000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }

                if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition((ModContent.NPCType<Bosses.Okiku.FinalForm.Attraidies>()))) || tsorcRevampWorld.SuperHardMode /*just in case)
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.PurgingStone>());
                shop.item[nextSlot].shopCustomPrice = 10000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }

            if (Main.bloodMoon)
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.MaskOfTheChild>());
                shop.item[nextSlot].shopCustomPrice = 1000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.MaskOfTheFather>());
                shop.item[nextSlot].shopCustomPrice = 1000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.MaskOfTheMother>());
                shop.item[nextSlot].shopCustomPrice = 1000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }*/
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
                damage = 60;
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
            projType = ModContent.ProjectileType<Projectiles.FirebombProj>();
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
            int type = ModContent.ItemType<Items.SoulCoin>();

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active)
                {
                    continue;
                }

                if (NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3 || player.CountItem(type, 150) >= 150 || player.HasBuff(ModContent.BuffType<Bonfire>()))
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
