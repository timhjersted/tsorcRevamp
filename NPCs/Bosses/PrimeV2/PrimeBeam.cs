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
    class PrimeBeam : ModNPC
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
            get => ((PrimeV2)primeHost.ModNPC).MoveIndex == 0;
        }

        int phase
        {
            get => ((PrimeV2)primeHost.ModNPC).Phase;
        }

        bool damaged;

        float rotationTarget;
        float rotationSpeed;
        bool counterClockwise;

        public Vector2 Offset = new Vector2(-604, 120);
        public int cooldown = 60;
        public override void AI()
        {
            
            int BeamDamage = 40;
            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 1.5f);
            UsefulFunctions.SmoothHoming(NPC, primeHost.Center + Offset, 0.1f, 50, primeHost.velocity);
            rotationSpeed = 0.03f;

            if(((PrimeV2)primeHost.ModNPC).aiPaused)
            {
                NPC.rotation = MathHelper.PiOver2;
                return;
            }

            if (((PrimeV2)primeHost.ModNPC).Phase == 1)
            {
                Offset = new Vector2(600, 0).RotatedBy(2 * MathHelper.TwoPi / 5f);
            }

            Vector2 targetRotation = rotationTarget.ToRotationVector2();
            Vector2 currentRotation = NPC.rotation.ToRotationVector2();
            Vector2 nextRotationVector = Vector2.Lerp(currentRotation, targetRotation, rotationSpeed);
            NPC.rotation = nextRotationVector.ToRotation();

            if (active)
            {
                if (phase == 0)
                {
                    //Sweep back and forth across the player
                    if (!UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeBeam>()))
                    {
                        counterClockwise = true;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeBeam>(), BeamDamage, 0.5f, Main.myPlayer, ai1: NPC.whoAmI);
                    }

                    if (counterClockwise)
                    {
                        rotationTarget = MathHelper.Pi + (Target.Center - NPC.Center).ToRotation() + MathHelper.Pi / 2f;
                    }
                    else
                    {
                        rotationTarget = MathHelper.Pi + (Target.Center - NPC.Center).ToRotation() - MathHelper.Pi / 2f;
                    }

                    if (Math.Abs(rotationTarget - NPC.rotation) < 0.5f)
                    {
                        counterClockwise = !counterClockwise;
                    }
                }
                else
                {
                    NPC.rotation = (Target.Center - NPC.Center).ToRotation();
                    //Spam stationary wide beams around the target player
                    if (Main.GameUpdateCount % 60 == 0)
                    {
                        NPC.rotation = (NPC.Center - Target.Center).ToRotation() + Main.rand.NextFloat(-0.5f, 0.5f);
                        //NewProjectile FastBeam, telegraph time 30f, duration 15f
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeRapidBeam>(), BeamDamage, 0.5f, Main.myPlayer, ai1: NPC.whoAmI);
                    }
                }
            }
            else
            {
                if (damaged)
                {
                    //Spam stationary wide beams around the target player
                    if (Main.GameUpdateCount % 300 == 0)
                    {
                        NPC.rotation = (NPC.Center - Target.Center).ToRotation() + Main.rand.NextFloat(-0.5f, 0.5f);
                        //NewProjectile FastBeam, telegraph time 30f, duration 15f
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeRapidBeam>(), BeamDamage, 0.5f, Main.myPlayer);
                    }
                }
                else
                {
                    if (Main.GameUpdateCount % 300 > 239)
                    {
                        rotationTarget = (Target.Center - NPC.Center).ToRotation() - MathHelper.PiOver2;
                        NPC.rotation = (Target.Center - NPC.Center).ToRotation() - MathHelper.PiOver2;
                    }
                    if (Main.GameUpdateCount % 300 == 299)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 1), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeRapidBeam>(), BeamDamage, 0.5f, Main.myPlayer, ai1: NPC.whoAmI);
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