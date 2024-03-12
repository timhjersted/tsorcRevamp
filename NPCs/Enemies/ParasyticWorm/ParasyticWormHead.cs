using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.ParasyticWorm
{
    class ParasyticWormHead : ModNPC
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            AnimationType = 10;
            NPC.netAlways = true;
            NPC.npcSlots = 5;
            NPC.width = 38;
            NPC.height = 32;
            NPC.aiStyle = 6;
            NPC.defense = 18;
            NPC.timeLeft = 750;
            NPC.damage = 45;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.lifeMax = 1500;
            NPC.knockBackResist = 0;
            NPC.lavaImmune = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.value = 3750; // was 400. life / 2 / 2 because worm
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ParasyticWormBanner>();

            bodyTypes = new int[13];
            int bodyID = ModContent.NPCType<ParasyticWormBody>();
            for (int i = 0; i < 13; i++)
            {
                bodyTypes[i] = bodyID;
            }
        }
        int[] bodyTypes;

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            bool nospecialbiome = !spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && !spawnInfo.Player.ZoneHallow && !spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneDungeon; // Not necessary at all to use but needed to make all this work.

            bool sky = nospecialbiome && ((double)spawnInfo.Player.position.Y < Main.worldSurface * 0.44999998807907104);
            bool surface = nospecialbiome && !sky && (spawnInfo.Player.position.Y <= Main.worldSurface);
            bool underground = nospecialbiome && !surface && (spawnInfo.Player.position.Y <= Main.rockLayer);
            bool cavern = nospecialbiome && (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25);
            bool undergroundJungle = (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25) && spawnInfo.Player.ZoneJungle;
            bool undergroundEvil = (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25) && (spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson);
            bool undergroundHoly = (spawnInfo.Player.position.Y >= Main.rockLayer) && (spawnInfo.Player.position.Y <= Main.rockLayer * 25) && spawnInfo.Player.ZoneHallow;

            if (Main.hardMode)
            {
                if (spawnInfo.Player.ZoneUnderworldHeight || undergroundEvil)
                {
                    if (Main.rand.NextBool(200))
                    {
                        return 1;
                    }
                }
            }
            return 0;
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<ParasyticWormHead>(), bodyTypes, ModContent.NPCType<ParasyticWormTail>(), 15, .4f, 8, 0.07f, false, false, false, true, true);
        }

        private static int ClosestSegment(NPC head, params int[] segmentIDs)
        {
            List<int> segmentIDList = new List<int>(segmentIDs);
            Vector2 targetPos = Main.player[head.target].Center;
            int closestSegment = head.whoAmI; //head is default, updates later
            float minDist = 1000000f; //arbitrarily large, updates later
            for (int i = 0; i < Main.npc.Length; i++)
            { //iterate through every NPC
                NPC npc = Main.npc[i];
                if (npc != null && npc.active && segmentIDList.Contains(npc.type))
                { //if the npc is part of the wyvern
                    float targetDist = (npc.Center - targetPos).Length();
                    if (targetDist < minDist)
                    { //if we're closer than the previously closer segment (or closer than 1,000,000 if it's the first iteration, so always)
                        minDist = targetDist; //update minDist. future iterations will compare against the updated value
                        closestSegment = i; //and set closestSegment to the whoAmI of the closest segment
                    }
                }
            }
            return closestSegment; //the whoAmI of the closest segment
        }

        public override bool PreKill()
        {
            //Putting this here so the gore is spawned before the head is moved
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Worm Gore 1").Type, 1f);
            }
            int closestSegmentID = ClosestSegment(NPC, ModContent.NPCType<ParasyticWormBody>(), ModContent.NPCType<ParasyticWormTail>());
            NPC.position = Main.npc[closestSegmentID].position; //teleport the head to the location of the closest segment before running npcloot
            return true;
        }
    }
}