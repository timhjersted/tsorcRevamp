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
    [Autoload(false)]
    class Archaeologist : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Archaeologist");
            Main.npcFrameCount[NPC.type] = 26;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 10;
            NPCID.Sets.AttackFrameCount[NPC.type] = 5;
            NPCID.Sets.DangerDetectRange[NPC.type] = 140;
            NPCID.Sets.AttackType[NPC.type] = 1; // 0 is throwing, 1 is shooting, 2 is magic, 3 is melee
            NPCID.Sets.AttackTime[NPC.type] = 25;
            NPCID.Sets.AttackAverageChance[NPC.type] = 10;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }

        public static List<string> Names = new List<string> {
            "Harrison", "Han", "Indie", "Rick"
        };

        public override string TownNPCName()
        {
            string name = Names[Main.rand.Next(Names.Count)]; //pick a random name from the list
            return name;
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
            chat.Add("Remember, X never marks the spot.");
            chat.Add("The report of my undeath was an exaggeration.");
            chat.Add("I *hate* snakes.");
            chat.Add("Seen any fortune or glory lately?");
            chat.Add("Trust me.");
            chat.Add("Ha! You think THESE boulder traps are bad...");
            chat.Add("I have no idea what I'm doing, but I know I'm doing it really, really well.");
            chat.Add("Having tree troubles? Try fire... Or, you know, an axe.");
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
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.MysteriousIdol>());
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.MeteorShot);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.WoodenArrow);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.Torch);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.ManaPotion);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.HealingPotion);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.PurificationPowder);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.NightOwlPotion);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.BottledWater);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.GlowingMushroom);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.FamiliarWig);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.PoisonedKnife);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.Grenade);
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Accessories.CosmicWatch>());
            nextSlot++;
            shop.item[nextSlot].SetDefaults(ItemID.TinkerersWorkshop);
            nextSlot++;
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (base.NPC.life <= 0) //even though npcs are immortal
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Archeologist Head Gore").Type);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Archeologist Arm Gore").Type);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Archeologist Arm Gore").Type);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Archeologist Leg Gore").Type);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Archeologist Leg Gore").Type);
            }
        }

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            if (Main.hardMode)
            {
                damage = 32;
                knockback = 6f;
            }
            else
            {
                damage = 16;
                knockback = 5f;
            }
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 10;
            randExtraCooldown = 20;
        }
        public override void DrawTownAttackGun(ref float scale, ref int item, ref int closeness)
        {
            item = ItemID.RopeCoil;
            scale = .7f;
            closeness = 26;
        }

        public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
        {
            projType = Mod.Find<ModProjectile>("ArcheologistWhip").Type;

            attackDelay = 1;
        }

        public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
        {
            multiplier = 1.5f;
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
                if (p.HasItem(ModContent.ItemType<Items.MysteriousIdol>()))
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
