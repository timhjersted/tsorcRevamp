using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class OmnirsEland : ModNPC
    {
        float customAi1;
        int OTimeLeft = 2000;
        bool walkAndShoot = false;

        bool canDrown = false;
        int drownTimerMax = 3500;
        int drownTimer = 3500;
        int drowningRisk = 2000;

        float npcAcSPD = 0.7f; //How fast they accelerate.
        float npcSPD = 2.3f; //Max speed

        bool tooBig = false;
        bool lavaJumping = false;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Eland");
            Main.npcFrameCount[NPC.type] = 15;
        }

        public override void SetDefaults()
        {
            NPC.lifeMax = 44;
            NPC.damage = 22;
            NPC.defense = 6;
            NPC.knockBackResist = 0.4f;
            NPC.width = 30;
            NPC.height = 40;
            NPC.aiStyle = 3; // Fighter/Zombie AI
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.value = 75;
            AnimationType = 21; // Skeleton/Zombie animation structure
        }

        /*
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            // Spawn disabled for now
            return 0f;
        }
        */

        public override void AI()
        {
            NPC.noGravity = false;
            NPC.spriteDirection = NPC.direction;

            #region shoot and walk
            if (walkAndShoot && Main.netMode != 1 && !Main.player[NPC.target].dead)
            {
            }
            #endregion

            #region drown
            if (canDrown)
            {
                if (!NPC.wet)
                {
                    NPC.TargetClosest(true);
                    drownTimer = drownTimerMax;
                }
                if (NPC.wet)
                {
                    drownTimer--;
                }
                if (NPC.wet && drownTimer > drowningRisk)
                {
                    NPC.TargetClosest(true);
                }
                else if (NPC.wet && drownTimer <= drowningRisk)
                {
                    NPC.TargetClosest(false);
                    if (NPC.timeLeft > 10)
                    {
                        NPC.timeLeft = 10;
                    }
                    NPC.directionY = -1;
                    if (NPC.velocity.Y > 0f)
                    {
                        NPC.direction = 1;
                    }
                    NPC.direction = -1;
                    if (NPC.velocity.X > 0f)
                    {
                        NPC.direction = 1;
                    }
                }
                if (drownTimer <= 0)
                {
                    NPC.life--;
                    if (NPC.life <= 0)
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath1, NPC.Center);
                        OnKill();
                        NPC.netUpdate = true;
                    }
                }
            }
            #endregion

            #region Too Big and Lava Jumping
            if (tooBig)
            {
                if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction < 0))
                {
                    NPC.velocity.Y -= 8f;
                    NPC.velocity.X -= npcSPD;
                }
                else if (NPC.velocity.Y == 0f && (NPC.velocity.X == 0f && NPC.direction > 0))
                {
                    NPC.velocity.Y -= 8f;
                    NPC.velocity.X += npcSPD;
                }
            }
            if (lavaJumping)
            {
                if (NPC.lavaWet)
                {
                    NPC.velocity.Y -= 2;
                }
            }
            #endregion
        }

        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsElandGore1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsElandGore2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsElandGore3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsElandGore2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, NPC.velocity, Mod.Find<ModGore>("OmnirsElandGore3").Type, 1f);
            }
            // Drops commented out
        }
    }
}
