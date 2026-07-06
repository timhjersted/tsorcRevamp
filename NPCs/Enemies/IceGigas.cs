using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
    // Sprite by Omnir, from Omnir's Nostalgia Pack: https://forums.terraria.org/index.php?threads/omnirs-nostalgia-pack.11875/
    public class IceGigas : ModNPC
    {
        float customAi1;
        int movedTowards = 0;
        int num94 = 0;
        int num95 = 0;
        int noJump = 0;
        int OTimeLeft = 2000;
        bool walkAndShoot = true;

        bool canDrown = true;
        int drownTimerMax = 3500;
        int drownTimer = 3500;
        int drowningRisk = 2000;

        float npcAcSPD = 0.5f; //How fast they accelerate.
        float npcSPD = 0.5f; //Max speed (Phase2: 50% slower, was 1.0f)

        float npcEnrAcSPD = .6f; //How fast they accelerate.
        float npcEnrSPD = 0.7f; //Max speed (Phase2: 50% slower, was 1.4f)

        bool tooBig = true;
        bool lavaJumping = false;
        bool thruWalls = true;
        int oNPCNoReach = 0;
        bool phaseThruWalls = false;
        bool oPhasing1 = false;
        bool oPhasing2 = false;
        bool oDigSound = false;
        int oAtt = 50;
        int oDef = 13;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Ice Gigas");
            Main.npcFrameCount[NPC.type] = 16;
        }

        public override void SetDefaults()
        {
            NPC.width = 52;
            NPC.height = 110;
            NPC.damage = 50;
            NPC.defense = 13;
            NPC.lifeMax = 750;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.value = 2000f;
            NPC.npcSlots = 100;
            NPC.scale = 1f;
            NPC.knockBackResist = 0.1f;
            AnimationType = 28; // Zombie frame structure

            // Phase 2: SmartFighter4AI movement + beast levers (mirrors Gigas). minSurfaceWidth (in AI)
            // keeps it off 1-tile ledges; NavSearchRadius enables A*; MaxJumpPower modestly above the 8 default
            // for a strong jump (NPC.gravity is read-only, so no true heavy "weighty" fall). Tune to taste.
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.NavSearchRadius = 60; // larger window so A* can find valid flat ledges above/across to jump to (mirrors Gigas)
            g.MaxJumpPower = 9f;
            g.MaxJumpBoost = 5f;
            // On-hit evasion: lumber back to reset spacing, or telegraph a hyper-armored charge back in.
            EvasiveProfile.HeavyBeast(g);
            // Phase 1 (beast positioner): never stand still — oscillate in a large band when it can't path; wander
            // off if it can't reach you AND you stop hitting it for ~10s. Tune the band to taste.
            g.KiteRangeMin = 0f;
            g.KiteRangeMax = 30f;
            g.KiteLooseness = 0.3f;
            g.PatrolMode = NPCs.PatrolMode.Wander;
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        /*
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // Spawn disabled for now
            return 0f;
        }
        */

        public void teleport(bool pre)
        {
            if (Main.netMode != 2)
            {
                SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                for (int m = 0; m < 25; m++)
                {
                    int dustID = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, 0, 0, 100, Color.White, 2f);
                    Main.dust[dustID].noGravity = true;
                    Main.dust[dustID].velocity = new Vector2(MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()), MathHelper.Lerp(-1f, 1f, (float)Main.rand.NextDouble()));
                    Main.dust[dustID].velocity *= 7f;
                }
            }
        }

        public override void AI()  //  warrior ai
        {
            bool enraged = (NPC.life < (float)NPC.lifeMax * .2f);  //  speed up at low life
            int shotRate = enraged ? 100 : 70;
            float accel = enraged ? npcEnrAcSPD : npcAcSPD;  //  how fast it can speed up
            float topSpeed = enraged ? npcEnrSPD : npcSPD;  //  max walking speed, also affects jump length
            
            tsorcRevampAIs.FighterAI(NPC, topSpeed: topSpeed, acceleration: accel, canTeleport: false, minSurfaceWidth: 3, canWalkBackwards: true); // ~3.25-tile footprint → require 3 flat tiles
            
            Vector2 angle = Main.player[NPC.target].Center - NPC.Center;
            angle.Y = angle.Y - (Math.Abs(angle.X) * .1f);
            angle.X += (float)Main.rand.Next(-20, 21);
            angle.Y += (float)Main.rand.Next(-20, 21);
            angle.Normalize();
            if (NPC.lavaWet) NPC.velocity.Y -= 2;
            float distance = NPC.Distance(Main.player[NPC.target].Center);
            
            #region shoot and walk
            if (Main.netMode != 1 && !Main.player[NPC.target].dead)
            {
                if (NPC.justHit)
                    NPC.ai[2] = 0f; // reset throw countdown when hit

                #region Charge
                if (NPC.velocity.Y == 0f && Main.rand.Next(550) == 1)
                {
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 7) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 7) * -1;
                    NPC.ai[1] = 1f;
                    NPC.netUpdate = true;
                }
                #endregion
            }
            #endregion
            if (NPC.velocity.Y == 0f && Main.rand.Next(550) == 1)
            {
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                NPC.velocity.X = (float)(Math.Cos(rotation) * 7) * -1;
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 7) * -1;
                NPC.localAI[3] = 1f;
                NPC.netUpdate = true;
            }
        }

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("IceGigasGore1").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("IceGigasGore2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("IceGigasGore3").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("IceGigasGore2").Type, 1.1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("IceGigasGore3").Type, 1.1f);
            }
            // Drops commented out
        }
    }
}
