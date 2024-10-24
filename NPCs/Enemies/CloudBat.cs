﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class CloudBat : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = Main.npcFrameCount[NPCID.GiantBat];
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.CaveBat);
            AnimationType = NPCID.GiantBat;
            AIType = NPCID.CaveBat;
            NPC.lifeMax = 100;
            NPC.damage = 65;
            NPC.knockBackResist = .55f;
            NPC.value = 500; // was 35 with 35 life
            NPC.defense = 20;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.CloudBatBanner>();
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (Main.hardMode)
            {
                chance = Terraria.ModLoader.Utilities.SpawnCondition.Sky.Chance * 0.2f;
            }
            return chance;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            if (Main.rand.NextBool(8))
            {
                target.AddBuff(BuffID.Confused, 10 * 60, true);
            }
        }
        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 15; i++)
            {
                int DustType = 16;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.04f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.04f;
                dust.scale *= .8f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 30; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 16, Main.rand.Next(-2, 2), Main.rand.Next(-2, 2), 70, default(Color), .8f);
                }
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("Cloud Bat Gore").Type, 1f);
                }
            }
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.Cloud, 1, 10, 20));
        }
    }
}