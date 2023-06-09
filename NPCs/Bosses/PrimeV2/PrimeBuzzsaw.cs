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
    class PrimeBuzzsaw : ModNPC
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
            NPC.lifeMax = 15000;
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
        PrimeV2 Prime
        {
            get => ((PrimeV2)primeHost.ModNPC);
        }

        public Player Target
        {
            get => Main.player[primeHost.target];
        }

        bool active
        {
            get => ((PrimeV2)primeHost.ModNPC).MoveIndex == 2;
        }
        int phase
        {
            get => ((PrimeV2)primeHost.ModNPC).Phase;
        }

        bool damaged;

        bool movingLeft;
        bool seekingPlayer;
        Vector2 drawOffset = Vector2.Zero;

        public Vector2 Offset = new Vector2(600, 200);
        public override void AI()
        {
            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 1.5f);
            int SawDamage = 60;
            if (Prime.aiPaused)
            {
                UsefulFunctions.SmoothHoming(NPC, primeHost.Center + Offset, 0.1f, 50, primeHost.velocity);
                NPC.rotation = MathHelper.PiOver2;
                return;
            }

            if (active && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.velocity *= 0.95f;
                drawOffset = Main.rand.NextVector2CircularEdge(1, 1);

                if (Prime.MoveTimer == 160)
                {
                    if (!damaged)
                    {
                        //Fire a bouncing saw
                        if (!UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>()))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 25f), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>(), SawDamage, 0.5f, Main.myPlayer, ai1: NPC.whoAmI);
                        }
                    }
                    else
                    {
                        //Fire a damaged slower bouncing saw that spawns shards of metal on impacts
                        if (!UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>()))
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 15f), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>(), SawDamage, 0.5f, Main.myPlayer, 1, NPC.whoAmI);
                        }
                    }
                }
            }
            else
            {
                //Simply sweep left and right, stopping to realign with the player after each pass
                if(Math.Abs(NPC.Center.X - Target.Center.X) > 700)
                {
                    seekingPlayer = true;
                }

                if (seekingPlayer)
                {
                    NPC.Center = new Vector2(NPC.Center.X, MathHelper.Lerp(NPC.Center.Y, Target.Center.Y, 0.05f));
                    if(Math.Abs(NPC.Center.Y - Target.Center.Y) < 10)
                    {
                        seekingPlayer = false;
                        movingLeft = NPC.Center.X > Target.Center.X;
                    }
                }
                

                if(!seekingPlayer)
                {
                    NPC.velocity = new Vector2(7, 0);

                    if (movingLeft)
                    {
                        NPC.velocity.X = -7;
                    }
                }
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

        static Texture2D texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/SuperHardMode/DarkCloud");
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            //Draw metal bones
            //Draw shadow trail (and maybe normal trail?)
            //Use the drawOffset to make it vibrate before firing
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