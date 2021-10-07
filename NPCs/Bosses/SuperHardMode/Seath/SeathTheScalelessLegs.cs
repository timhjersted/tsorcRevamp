using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Seath
{
    class SeathTheScalelessLegs : ModNPC
    {
        public override void SetDefaults()
        {
            npc.netAlways = true;
            npc.npcSlots = 2;
            npc.width = 32;
            npc.height = 32;
            drawOffsetY = 60;
            npc.aiStyle = 6;
            npc.knockBackResist = 0;
            npc.timeLeft = 22500;
            npc.damage = 80;
            npc.defense = 60;
            npc.HitSound = SoundID.NPCHit7;
            npc.DeathSound = SoundID.NPCDeath8;
            npc.lifeMax = 75000;
            music = 12;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
        }


        int breathDamage = 33;
        int flameRainDamage = 27;
        int meteorDamage = 33;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            npc.lifeMax = (int)(npc.lifeMax / 2);
            npc.damage = (int)(npc.damage / 2);
            breathDamage = (int)(breathDamage / 2);
            flameRainDamage = (int)(flameRainDamage / 2);
            meteorDamage = (int)(meteorDamage / 2);
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Seath the Scaleless");
        }
        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }
        public override void AI()
        {
            if (NPC.AnyNPCs(ModContent.NPCType<PrimordialCrystal>()))
            {
                npc.dontTakeDamage = true;
            }
            else
            {
                npc.dontTakeDamage = false;
            }
            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.behindTiles = true;
            int[] bodyTypes = new int[] { ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessLegs>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessLegs>(), ModContent.NPCType<SeathTheScalelessBody>(), ModContent.NPCType<SeathTheScalelessBody2>(), ModContent.NPCType<SeathTheScalelessBody3>() };
            tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<SeathTheScalelessHead>(), bodyTypes, ModContent.NPCType<SeathTheScalelessTail>(), 17, 6f, 10f, 0.17f, true, false);

            if (!Main.npc[(int)npc.ai[1]].active)
            {
                npc.active = false;
            }
        }

        public override bool CheckActive()
        {
            return false;
        }
        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
            SeathTheScalelessHead.SetImmune(projectile, npc);
        }

        public override void NPCLoot()
        {

            //npc.netUpdate = true;
            if (npc.life <= 0)
            {
                Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Seath the Scaleless Legs Gore"), 1f);

            }
        }
    }
}