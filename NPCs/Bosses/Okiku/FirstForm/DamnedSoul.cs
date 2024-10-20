using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.NPCs.Bosses.Okiku.FirstForm
{
    public class DamnedSoul : ModNPC
    {
        private bool initiate;

        public int TimerHeal;

        public float TimerAnim;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 4;
        }
        public override void SetDefaults()
        {
            NPC.alpha = 50;
            NPC.width = 50;
            NPC.height = 50;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 18;
            NPC.noGravity = true;
            NPC.boss = true;
            NPC.noTileCollide = true;
            NPC.lifeMax = 17250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            despawnHandler = new NPCDespawnHandler(54);
        }


        public int ObscureShotDamage = 30;

        float smoothPercent;
        public NPC Attraidies
        {
            get => Main.npc[(int)NPC.ai[1]];
        }
        public DarkShogunMask AttraidiesMask
        {
            get => Attraidies.ModNPC as DarkShogunMask;
        }
        public float ImmuneTimer
        {
            get => NPC.ai[3];
            set => NPC.ai[3] = value;
        }

        public int ShotTimer;

        List<float> foundIndicies = new List<float>();
        float RotSpeed = 0.015f;
        int rotationTimer = 0;
        bool RotDir = false;
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            rotationTimer++;
            if (!initiate)
            {
                for (int i = 0; i < 20; i++)
                {
                    int dustIndex = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 200, Color.White, 4f);
                    Main.dust[dustIndex].noGravity = true;
                }

                if (NPC.ai[2] != 0)
                {
                    NPC.realLife = (int)NPC.ai[2];
                    NPC.ai[2] = 0;
                }

                ShotTimer = -Main.rand.Next(200);
                NPC.netUpdate = true;
                initiate = true;
            }

            if(!NPC.AnyNPCs(ModContent.NPCType<AttraidiesShield>()) && NPC.ai[0] != -1)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 550, 20);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 520, 60);
                }

                NPC.active = false;
            }


            TimerAnim += 1f;
            if (TimerAnim > 10f)
            {
                if (Main.rand.NextBool(2))
                {
                    NPC.spriteDirection *= -1;
                }
                TimerAnim = 0f;
            }

            
            int dust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 62, 0f, 0f, 100, Color.White);
            Main.dust[dust].noGravity = true;

            if (NPC.ai[0] > -1)
            {
                if (RotDir == true)
                {
                    RotSpeed += 0.00005f;
                }
                if (RotDir == false)
                {
                    RotSpeed -= 0.00005f;
                }
                if (RotSpeed > 0.02f) RotDir = false;
                if (RotSpeed < -0.01f) RotDir = true;

                if(smoothPercent < 1)
                {
                    smoothPercent += 0.05f;
                }                

                NPC.Center = Attraidies.Center + new Vector2(400 + 50 * (float)Math.Sin(rotationTimer  / 50f), 0).RotatedBy(MathHelper.TwoPi * NPC.ai[0] / 6).RotatedBy(RotSpeed * rotationTimer * 0.1f) * UsefulFunctions.EasingCurve(smoothPercent);
                NPC.scale = 1f + Math.Abs(RotSpeed * 10) * 5;

                //Projectiles
                ShotTimer += 1;
                if (ShotTimer >= 0f)
                {
                    if (!AttraidiesMask.Transforming)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (!AttraidiesMask.ShieldBroken)
                            {
                                float speed = 0.5f;
                                Vector2 position = new Vector2(NPC.position.X + (float)(NPC.width / 2), NPC.position.Y + (float)(NPC.height / 2));
                                float rotation2 = (float)Math.Atan2(position.Y - (Main.player[NPC.target].position.Y + (float)Main.player[NPC.target].height * 0.5f), position.X - (Main.player[NPC.target].position.X + (float)Main.player[NPC.target].width * 0.5f));
                                rotation2 += (float)(Main.rand.Next(-50, 50) / 100);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), position.X, position.Y, (float)(Math.Cos(rotation2) * (double)speed * -1.0), (float)(Math.Sin(rotation2) * (double)speed * -1.0), ModContent.ProjectileType<ObscureShot>(), ObscureShotDamage, 0f, Main.myPlayer);
                            }
                            else
                            {
                                Vector2 vel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 5);
                                vel += Main.player[NPC.target].velocity / Main.rand.NextFloat(0, 3); //Mildly predictive, with a random strength between 0 and 1/3rd
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<ObscureShot>(), ObscureShotDamage, 0f, Main.myPlayer);
                            }
                        }
                        ShotTimer = -200 - Main.rand.Next(200);
                    }
                }
            }
            else
            {
                NPC.scale = 3;
                NPC.dontTakeDamage = true;
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
        }

        public override bool CheckDead()
        {
            return false;
        }

        public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
        {
            return false;
        }

        public override void FindFrame(int frameHeight)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            num++;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 8.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }
    }
}
