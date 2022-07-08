using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class TibianMage : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tibian Mage");
            Main.npcFrameCount[NPC.type] = 26;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 10;
            NPCID.Sets.AttackFrameCount[NPC.type] = 5;
            NPCID.Sets.DangerDetectRange[NPC.type] = 450; // keep this low for melee
            NPCID.Sets.AttackType[NPC.type] = 1; // 0 is throwing, 1 is shooting, 2 is magic, 3 is melee
            NPCID.Sets.AttackTime[NPC.type] = 25;
            NPCID.Sets.AttackAverageChance[NPC.type] = 10;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }

        public static List<string> Names = new List<string> {
            "Asima", "Lea", "Padreia", "Loria", "Lungelen", "Lily", "Sandra", "Tibra", "Astera Tiger"
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
            NPC.defense = 25;
            NPC.lifeMax = 1000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.3f;
            AnimationType = NPCID.Guide;
        }

        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add("Time is a force we sorcerers will master one day.");
            chat.Add("Any sorcerer dedicates their whole life to the study of the arcane arts.");
            chat.Add("Sorry, I only sell spells to sorcerers.");
            chat.Add("I could tell you much about all sorcerer spells, but you won't understand it. Anyway, feel free to ask me.");
            chat.Add("I'll teach you a very seldom spell.");
            chat.Add("Many call themselves a sorcerer, but only a few truly understand what that means.");
            chat.Add("Sorcerers are destructive. Their power lies in destruction and pain.");
            chat.Add("Welcome to our humble guild, wanderer. May I be of any assistance to you?");
            chat.Add("Attraidies visited this town once. The bottle he drank from is still enchanted with some of his power.", 0.1);
            return chat;
        }

        int weaponChoice;
        public override void AI()
        {
            if (Main.rand.NextBool(40) && NPC.frame.Y < 1170)
            // only change weapons if we're not in attack animation frames
            // 1170 is before the start of the first attack frame but after the start of the previous animation frame
            // in other words: arbitrary, but close enough
            {
                weaponChoice = Main.rand.Next(0, 10);
                //Main.NewText(weaponChoice);
            }
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
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.WandOfDarkness>());
            shop.item[nextSlot].shopCustomPrice = 80;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.FarronDart>());
            shop.item[nextSlot].shopCustomPrice = 80;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.GlintstonePebble>());
            shop.item[nextSlot].shopCustomPrice = 5;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ItemCrates.GelCrate>());
            shop.item[nextSlot].shopCustomPrice = 8;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.SpellTome);
            shop.item[nextSlot].shopCustomPrice = 50;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.MagicMirror);
            shop.item[nextSlot].shopCustomPrice = 50;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++; 
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.RedClothHat>());
            shop.item[nextSlot].shopCustomPrice = 50;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.RedClothTunic>());
            shop.item[nextSlot].shopCustomPrice = 100;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.RedClothPants>());
            shop.item[nextSlot].shopCustomPrice = 75;
            shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            nextSlot++;
            if (NPC.downedBoss1)
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.WandOfFire>());
                shop.item[nextSlot].shopCustomPrice = 550;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ItemID.PiggyBank);
                shop.item[nextSlot].shopCustomPrice = 100;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }
            if (Main.hardMode)
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.FlameStrikeScroll>());
                shop.item[nextSlot].shopCustomPrice = 2000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.EnergyStrikeScroll>());
                shop.item[nextSlot].shopCustomPrice = 2000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.DeathStrikeScroll>());
                shop.item[nextSlot].shopCustomPrice = 3000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Accessories.CovetousSilverSerpentRing>());
                shop.item[nextSlot].shopCustomPrice = 3500;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.ManaBomb>());
                shop.item[nextSlot].shopCustomPrice = 300;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }
            if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheRage>()))
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.GreatEnergyBeamScroll>());
                shop.item[nextSlot].shopCustomPrice = 3500;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ForgottenIceBowScroll>());
                shop.item[nextSlot].shopCustomPrice = 5000;
                shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                nextSlot++;
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ForgottenThunderBowScroll>());
                shop.item[nextSlot].shopCustomPrice = 5000;
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
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Mage Gore 1").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Mage Gore 2").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Mage Gore 2").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Mage Gore 3").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Tibian Female Mage Gore 3").Type);
                }
            }
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            if (Main.hardMode)
            {
                if (weaponChoice < 6)
                {
                    damage = 50;
                    knockback = 5f;
                }
                if (weaponChoice >= 6)
                {
                    damage = 20;
                    knockback = 6f;
                }
            }
            else
            {
                if (weaponChoice < 8) //More likely to use Great Soul Arrow Staff
                {
                    damage = 25;
                    knockback = 4f;
                }
                if (weaponChoice >= 8)
                {
                    damage = 10;
                    knockback = 5f;
                }
            }
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 30;
            randExtraCooldown = 30;
        }
        public override void DrawTownAttackGun(ref float scale, ref int item, ref int closeness)
        {
            if (weaponChoice < 8)
            {
                item = ModContent.ItemType<Items.Weapons.Magic.GreatSoulArrowStaff>();
                scale = 1f;
                closeness = 4;
            }
            if (weaponChoice >= 8)
            {
                item = ModContent.ItemType<Items.Weapons.Magic.TheBlackenedFlames>();
                scale = 1f;
                closeness = 4;
            }
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            if (weaponChoice < 8)
            {
                projType = ModContent.ProjectileType<Projectiles.GreatSoulArrow>();
                attackDelay = 8;
            }
            if (weaponChoice >= 8)
            {
                projType = ModContent.ProjectileType<Projectiles.BlackFire>();
                attackDelay = 8;
            }
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            if (weaponChoice < 8)
            {
                multiplier = 10f;
                randomOffset = 0f;
            }
            if (weaponChoice >= 8)
            {
                multiplier = 10f;
                randomOffset = 2f;
                gravityCorrection = 25;
            }

        }

        public override bool CanTownNPCSpawn(int numTownNPCs, int money)
        {
            foreach (Player p in Main.player)
            {
                if (!p.active)
                {
                    continue;
                }
                if (p.statManaMax2 > 21) //was 1, but these days mana starts at 20 on fresh char.
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
