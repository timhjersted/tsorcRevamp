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
    class PrimeSiege : ModNPC
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
            get => ((PrimeV2)primeHost.ModNPC).MoveIndex == 4;
        }
        int phase
        {
            get => ((PrimeV2)primeHost.ModNPC).Phase;
        }

        bool damaged;

        float rotationTarget;
        float rotationSpeed;
        float rotationOffset = MathHelper.PiOver4;

        public Vector2 Offset = new Vector2(200, 70);
        public override void AI()
        {
            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 1.5f);
            UsefulFunctions.SmoothHoming(NPC, primeHost.Center + Offset, 0.1f, 50, primeHost.velocity);
            rotationSpeed = 0.03f;

            if (((PrimeV2)primeHost.ModNPC).aiPaused)
            {
                NPC.rotation = MathHelper.PiOver2;
                return;
            }
            if (((PrimeV2)primeHost.ModNPC).Phase == 1)
            {
                Offset = new Vector2(1200, 0).RotatedBy(4 * MathHelper.TwoPi / 5f);
            }

            rotationTarget = rotationOffset  + MathHelper.Pi;
            if (!damaged)
            {
                rotationTarget += (NPC.Center - Target.Center).ToRotation() + MathHelper.PiOver2;
            }

            if(damaged && active)
            {
                rotationTarget += (NPC.Center - Target.Center).ToRotation() + MathHelper.PiOver2 - MathHelper.Pi;

            }
            Vector2 targetRotation = rotationTarget.ToRotationVector2();
            Vector2 currentRotation = NPC.rotation.ToRotationVector2();
            Vector2 nextRotationVector = Vector2.Lerp(currentRotation, targetRotation, rotationSpeed);
            NPC.rotation = nextRotationVector.ToRotation();

            float rotationDiff = Math.Abs(rotationTarget - NPC.rotation);
            if (rotationDiff < 0.4f || Math.Abs(rotationDiff - MathHelper.TwoPi) < 0.4f)
            {
                rotationOffset *= -1;
            }

            if (active)
            {
                if (!damaged)
                {
                    if ((Main.GameUpdateCount % 90) % 10 == 0 && Main.GameUpdateCount % 90 <= 20)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Projectiles.Enemy.Prime.HullBreachMissile>(), ai0: Target.whoAmI);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    }
                }
                else
                {
                    if (Main.GameUpdateCount % 30 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Projectiles.Enemy.Prime.HullBreachMissile>(), ai0: Target.whoAmI, ai1: 1);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    }
                }
            }
            else
            {
                if (!damaged)
                {
                    if (Main.GameUpdateCount % 450 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Projectiles.Enemy.Prime.HullBreachMissile>(), ai0: Target.whoAmI);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    }
                }
                else
                {
                    if (Main.GameUpdateCount % 60 == 0)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<Projectiles.Enemy.Prime.HullBreachMissile>(), ai0: Target.whoAmI, ai1: 2);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    }
                }
            }
        }

        public override bool CheckDead()
        {
            if (((PrimeV2)primeHost.ModNPC).dying)
            {
                return true;
            }
            else
            {
                NPC.life = 1;
                damaged = true;
                rotationOffset = MathHelper.Pi / 3f;
                rotationSpeed = 0.05f;
                NPC.dontTakeDamage = true;
                return false;
            }
        }

        public static Texture2D texture;
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