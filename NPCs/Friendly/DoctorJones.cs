using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.Utilities;

namespace tsorcRevamp.NPCs.Friendly
{

    [Autoload(false)]
    class DoctorJones : ModNPC
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Doctor Jones");
            Main.npcFrameCount[NPC.type] = 26;
            //NPCID.Sets.ExtraFramesCount[npc.type] = 10;
            //NPCID.Sets.AttackFrameCount[npc.type] = 5;
            //NPCID.Sets.DangerDetectRange[npc.type] = 140;
            //NPCID.Sets.AttackType[npc.type] = 1; // 0 is throwing, 1 is shooting, 2 is magic, 3 is melee
            //NPCID.Sets.AttackTime[npc.type] = 25;
            //NPCID.Sets.AttackAverageChance[npc.type] = 10;
            //NPCID.Sets.HatOffsetY[npc.type] = 4;
        }

        public override List<string> SetNPCNameList() {
            return Names;
        }

        public static List<string> Names = new List<string> {
			"Harrison", "Han", "Indie", "Rick"
		};

        public override void SetDefaults()
        {
            //npc.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            //npc.aiStyle = ModContent.NPCType<NPCs.Friendly.Archeologist>();
            NPC.aiStyle = 7;
            NPC.damage = 50;
            NPC.defense = 15;
            NPC.lifeMax = 1000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.3f;
            AnimationType = NPCID.Guide;
        }

        public override bool PreAI()
        {
            NPC.Transform(ModContent.NPCType<Archaeologist>());
            return true;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;

            if (spawnInfo.Player.ZoneJungle && !NPC.AnyNPCs(ModContent.NPCType<DoctorJones>()) && !NPC.AnyNPCs(ModContent.NPCType<Archaeologist>()) && Main.rand.NextBool(10))
            {
                UsefulFunctions.BroadcastText("The spirit of adventure is nearby...", 255, 255, 0);
                return 1f;
            }
            return chance;
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
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Archeologist Head Gore").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Archeologist Arm Gore").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Archeologist Arm Gore").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Archeologist Leg Gore").Type);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Archeologist Leg Gore").Type);
                }
            }
        }
    }
}
