using Microsoft.Xna.Framework;
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
            DisplayName.SetDefault("Tibian Archer");
            Main.npcFrameCount[NPC.type] = 26;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 10;
            NPCID.Sets.AttackFrameCount[NPC.type] = 5;
            NPCID.Sets.DangerDetectRange[NPC.type] = 700;
            NPCID.Sets.AttackType[NPC.type] = 1; // 0 is throwing, 1 is shooting, 2 is magic, 3 is melee
            NPCID.Sets.AttackTime[NPC.type] = 40;
            NPCID.Sets.AttackAverageChance[NPC.type] = 10;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }

        public static List<string> Names = new List<string> {
            "Elane", "Legola", "Galuna", "Enalea"
        };

        public override List<string> SetNPCNameList() {
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
            chat.Add("I am the local fletcher. I sell bows, crossbows and ammunition. Do you need anything?");
            chat.Add("Tibia, a green island. It is wonderful to walk into the forests and to hunt with a bow there.");
            chat.Add("I am paladin and fletcher.");
            chat.Add("We are feared warriors and good marksmen.");
            chat.Add("Hello. Would you like to buy some of my wares?");
            chat.Add("Please show respect to Eloise. I don't want to have to hurt you.");
            chat.Add("Amazons and dworcs are a real threat.");
            chat.Add("I'm far from home, but this isn't so bad.");
            return chat;
        }

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
        }

        public override void OnChatButtonClicked(bool firstButton, ref bool shop)
        {
            if (firstButton)
            {
                shop = true;
                return;
            }
        }

        public override void SetupShop(Chest shop, ref int nextSlot)
        {
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ItemCrates.BoltCrate>());
            shop.item[nextSlot].shopCustomPrice = 8;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ItemCrates.ThrowingAxeCrate>());
            shop.item[nextSlot].shopCustomPrice = 8;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ItemCrates.ThrowingSpearCrate>());
            shop.item[nextSlot].shopCustomPrice = 8;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ItemCrates.WoodenArrowCrate>());
            shop.item[nextSlot].shopCustomPrice = 6;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;             
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.Ranged.LeatherHelmet>());
            shop.item[nextSlot].shopCustomPrice = 50;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.Ranged.LeatherArmor>());
            shop.item[nextSlot].shopCustomPrice = 100;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.Ranged.LeatherGreaves>());
            shop.item[nextSlot].shopCustomPrice = 75;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.LesserHealingPotion);
            shop.item[nextSlot].shopCustomPrice = 4;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.Safe);
            shop.item[nextSlot].shopCustomPrice = 250;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ItemCrates.FrostburnArrowCrate>());
            shop.item[nextSlot].shopCustomPrice = 12;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;

            if (NPC.downedBoss1)
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Ranged.OldCrossbow>());
                shop.item[nextSlot].shopCustomPrice = 180;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Ranged.Bows.OldLongbow>());
                shop.item[nextSlot].shopCustomPrice = 350;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ItemCrates.UnholyArrowCrate>());
                shop.item[nextSlot].shopCustomPrice = 25;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;

            }
            if (NPC.downedBoss2)
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ItemCrates.RoyalThrowingSpearCrate>());
                shop.item[nextSlot].shopCustomPrice = 12;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.Gaibon>())))
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ItemCrates.MeteorShotCrate>());
                shop.item[nextSlot].shopCustomPrice = 30;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.JungleWyvern.JungleWyvernHead>())))
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.EnchantedThrowingSpear>());
                shop.item[nextSlot].shopCustomPrice = 2200;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Ammo.PowerBolt>());
                shop.item[nextSlot].shopCustomPrice = 1;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.JestersArrow);
                shop.item[nextSlot].shopCustomPrice = 1;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.HellfireArrow);
                shop.item[nextSlot].shopCustomPrice = 1;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }

            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.TheHunter>())))
            {
                shop.item[nextSlot].SetDefaults(ItemID.HolyArrow);
                shop.item[nextSlot].shopCustomPrice = 1;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }
        }

        public override void HitEffect(int hitDirection, double damage)
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
        public override void DrawTownAttackGun(ref float scale, ref int item, ref int closeness)
        {
            item = ModContent.ItemType<Items.Weapons.Ranged.Bows.ElfinBow>();
            scale = 1f;
            closeness = 20;
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

        public override bool CanTownNPCSpawn(int numTownNPCs, int money)
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
