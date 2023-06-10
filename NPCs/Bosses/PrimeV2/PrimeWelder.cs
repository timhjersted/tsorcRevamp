using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Enemy.DarkCloud;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.ItemDropRules;
using System;

namespace tsorcRevamp.NPCs.Bosses.PrimeV2
{
    [AutoloadBossHead]
    class PrimeWelder : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 2;
            NPCID.Sets.TrailCacheLength[NPC.type] = (int)TRAIL_LENGTH;    //The length of old position to be recorded
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 53;
            NPC.defense = 20;
            NPC.lifeMax = 7500;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = 0;
            NPC.knockBackResist = 0f;
            NPC.timeLeft = 99999;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
        }
        const float TRAIL_LENGTH = 12;

        public int AttackTimer
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        NPC primeHost
        {
            get => Main.npc[(int)NPC.ai[1]];
        }
        public Player Target
        {
            get => Main.player[primeHost.target];
        }

        bool active
        {
            get => ((PrimeV2)primeHost.ModNPC).MoveIndex == 5;
        }

        int phase;
        bool damaged;
        float angle;
        public Vector2 Offset = new Vector2(-810, 250);
        public override void AI()
        {            
            int WeldDamage = 40;
            Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 1.5f);

            if (((PrimeV2)primeHost.ModNPC).aiPaused)
            {
                UsefulFunctions.SmoothHoming(NPC, primeHost.Center + Offset, 0.2f, 50, primeHost.velocity);
                NPC.rotation = MathHelper.PiOver2;
                return;

            }
            if (((PrimeV2)primeHost.ModNPC).Phase == 1)
            {
                Offset = new Vector2(1200, 0);
            }

            NPC.rotation = (Target.Center - NPC.Center).ToRotation() - MathHelper.PiOver2;
            if (!UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.MoltenWeld>()))
            {
                if(Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Prime.MoltenWeld>(), WeldDamage, 0.5f, Main.myPlayer, ai1: NPC.whoAmI);
                }
            }

            if (active)
            {
                UsefulFunctions.SmoothHoming(NPC, Target.Center, 0.3f, 8f, bufferZone: false);
            }
            else
            {
                angle += 0.01f;
                UsefulFunctions.SmoothHoming(NPC, Target.Center + new Vector2(400, 0).RotatedBy(angle), 0.3f, 5f);
                Dust.NewDustPerfect(Target.Center + new Vector2(400, 0).RotatedBy(angle), DustID.ShadowbeamStaff, Main.rand.NextVector2Circular(10, 10));
            }
        }


        public override bool CheckDead()
        {
            if (((PrimeV2)primeHost.ModNPC).Phase == 1)
            {
                return true;
            }
            else
            {
                NPC.life = 1;
                damaged = true;
                NPC.dontTakeDamage = true;
                return false;
            }
        }

        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            UsefulFunctions.EnsureLoaded(ref texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeWelder");
            //Draw metal bones
            //Draw shadow trail (and maybe normal trail?)
            if (active)
            {
                //Draw aura
            }
            if (damaged)
            {
                //Draw damaged version
            }
            else
            {
                //Draw normal version
            }
            return true;
        }

        public override void OnKill()
        {
            //Explosion
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}