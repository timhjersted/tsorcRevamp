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
    class PrimeGatling : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
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
            NPC.defense = 0;
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
        public Player Target
        {
            get => Main.player[primeHost.target];
        }

        bool active
        {
            get => ((PrimeV2)primeHost.ModNPC).MoveIndex == 3;
        }
        int phase
        {
            get => ((PrimeV2)primeHost.ModNPC).Phase;
        }

        bool damaged;


        public Vector2 Offset = new Vector2(604, 200);
        int cooldown;
        public override void AI()
        {
            int LaserDamage = 40;
            Lighting.AddLight(NPC.Center, Color.GreenYellow.ToVector3() * 1.5f);
            UsefulFunctions.SmoothHoming(NPC, primeHost.Center + Offset, 0.1f, 50, primeHost.velocity);

            if (((PrimeV2)primeHost.ModNPC).aiPaused)
            {
                NPC.rotation = MathHelper.PiOver2;
                return;
            }

            if (((PrimeV2)primeHost.ModNPC).Phase == 1)
            {
                Offset = new Vector2(600, 0).RotatedBy(MathHelper.TwoPi / 5f);
            }


            NPC.rotation = (NPC.Center - Target.Center).ToRotation() + MathHelper.PiOver2;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (active)
                {
                    if (!damaged)
                    {
                        if(cooldown <= 15)
                        {
                            cooldown = 15;
                        }
                        //Fire increasingly fast bursts of 3 projectiles
                        if (Main.GameUpdateCount % cooldown == cooldown - 1)
                        {
                            cooldown = (int)(cooldown * 0.965);
                            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, Target.Center, 10f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage, 0.5f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity * 0.8f, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage, 0.5f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity * 0.66f, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage, 0.5f, Main.myPlayer);
                        }
                    }
                    else
                    {
                        //Just spam shots everywhere
                        if (Main.GameUpdateCount % 5 == 0)
                        {
                            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, Target.Center, 6f);
                            velocity = velocity.RotatedBy(Main.rand.NextFloat(-1, 1));
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage, 0.5f, Main.myPlayer);
                        }
                    }
                }
                else
                {
                    if (!damaged)
                    {
                        cooldown = 60;
                        if (Main.GameUpdateCount % 90 == 45)
                        {
                            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, Target.Center, 8.5f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage, 0.5f, Main.myPlayer);
                        }
                    }
                    else
                    {
                        if (Main.GameUpdateCount % 90 == 45)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 7.5f).RotatedBy(Main.rand.NextFloat(-1, 1)), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage, 0.5f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 7.5f).RotatedBy(Main.rand.NextFloat(-1, 1)), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage, 0.5f, Main.myPlayer);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 7.5f).RotatedBy(Main.rand.NextFloat(-1, 1)), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeDeathLaser>(), LaserDamage, 0.5f, Main.myPlayer);
                        }
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