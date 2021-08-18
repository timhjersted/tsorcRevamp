using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage
{
    [AutoloadBossHead]
    class GhostDragonHead : ModNPC
    {
        public override void SetDefaults()
        {
            npc.netAlways = true;
            npc.npcSlots = 1;
            npc.width = 45;
            npc.height = 45;
            npc.aiStyle = 6;
            npc.knockBackResist = 0;
            npc.timeLeft = 22750;
            npc.damage = 115;
            npc.defense = 120;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath10;
            npc.lifeMax = 760000;
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.value = 660000;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
            bossBag = ModContent.ItemType<Items.BossBags.GhostWyvernBag>();
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Ghost Wyvern");
        }
        public override void AI()
        {

            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.behindTiles = true;
            int[] bodyTypes = new int[] { ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonLegs>(), ModContent.NPCType<GhostDragonBody>(), ModContent.NPCType<GhostDragonBody2>(), ModContent.NPCType<GhostDragonBody3>() };
            tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<GhostDragonHead>(), bodyTypes, ModContent.NPCType<GhostDragonTail>(), 23, -2f, 15f, 0.23f, true, false);

            //this makes the head always stay in the same position even when it flips upside down
            if (npc.velocity.X < 0f) { npc.spriteDirection = 1; }
            else  //both -1 is correct
            if (npc.velocity.X > 0f) { npc.spriteDirection = -1; }

            //Color color = new Color();
            //int dust = Dust.NewDust(new Vector2((float) //npc.position.X, (float) npc.position.Y), npc.width, npc.height, 62, 0, 0, 100, color, 1.0f);
            //Main.dust[dust].noGravity = true;
        }


        public override void NPCLoot()
        {
            Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));            
            int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, 0, 0, 100, Color.White, 5.0f);
            Main.dust[dust].noGravity = true;

            //Only drop the loot if the mage is already dead. If it's not, then he will drop it instead.
            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()))
            {
                if (Main.expertMode)
                {
                    npc.DropBossBags();
                }
                else
                {
                    Item.NewItem(npc.getRect(), ModContent.ItemType<Items.GhostWyvernSoul>(), 8);
                }
            } else
            {

                Main.NewText("The souls of " + npc.GivenOrTypeName + " have been released!", 175, 255, 75);
                tsorcRevampWorld.Slain[ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()] = 1;
            }
        }
    }
}