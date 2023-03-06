using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Okiku.FirstForm
{
    public class DamnedSoul : ModNPC
    {
        private bool initiate;

        public int TimerHeal;

        public float TimerAnim;

        public override void SetDefaults()
        {
            NPC.alpha = 50;
            NPC.width = 50;
            NPC.height = 50;
            NPC.aiStyle = -1;
            NPC.damage = 40;
            NPC.defense = 18;
            NPC.noGravity = true;
            NPC.boss = true;
            NPC.noTileCollide = true;
            NPC.lifeMax = 10000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            Main.npcFrameCount[NPC.type] = 4;
            despawnHandler = new NPCDespawnHandler(54);
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Damned Soul");
        }

        public int ObscureShotDamage = 30;
        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
        }

        public NPC Attraidies
        {
            get => Main.npc[(int)NPC.ai[1]];
        }
        public DarkShogunMask AttraidiesMask
        {
            get => Attraidies.ModNPC as DarkShogunMask;
        }

        public int ShotTimer
        {
            get => (int)NPC.ai[3];
            set => NPC.ai[3] = value;
        }
        
        List<float> foundIndicies = new List<float>();
        float RotSpeed = 0.015f;
        bool RotDir = false;
        NPCDespawnHandler despawnHandler;        
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (!initiate)
            {
                for (int i = 0; i < 20; i++)
                {
                    int dustIndex = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 200, Color.White, 4f);
                    Main.dust[dustIndex].noGravity = true;
                }

                NPC.ai[3] = -Main.rand.Next(200);
                initiate = true;
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

            if (NPC.realLife != -1)
            {
                NPC.life = Main.npc[NPC.realLife].life;
            }
            if (AttraidiesMask != null)
            {
                if (AttraidiesMask.ShieldBroken)
                {
                    if (RotSpeed < 0.03f) RotSpeed += 0.0003f;
                    NPC.dontTakeDamage = true;
                }
                else
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
                    if (RotSpeed < 0.01f) RotDir = true;
                    NPC.dontTakeDamage = false;
                }
                NPC.scale = (RotSpeed * 200) / 2;

                Vector2 center = new Vector2(120 * RotSpeed * 200);
                center = center.RotatedBy(Attraidies.ai[3] + (NPC.ai[0] * 2 * MathHelper.Pi / 6));
                if (AttraidiesMask.Transform)
                {
                    if (1 - (Attraidies.ai[2] / 300f) > 0.05f)
                    {
                        center *= 1 - (Attraidies.ai[2] / 300f);
                    }
                    else
                    {
                        center *= 0.05f;
                    }
                }
                NPC.Center = Attraidies.Center + center;
                if (Attraidies.life <= 1000)
                {
                    return;
                }
            }



            ShotTimer += 1;
            if (ShotTimer >= 0f)
            {
                if (!AttraidiesMask.Transform)
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
                            Vector2 vel = UsefulFunctions.GenerateTargetingVector(NPC.Center, Main.player[NPC.target].Center, 5);
                            vel += Main.player[NPC.target].velocity / Main.rand.NextFloat(0, 3); //Mildly predictive, with a random strength between 0 and 1/3rd
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<ObscureShot>(), ObscureShotDamage, 0f, Main.myPlayer);
                        }
                    }
                    ShotTimer = -200 - Main.rand.Next(200);
                }
            }

            if (NPC.life > 1000)
            {
                return;
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if (NPC.life - damage <= 1000)
            {
                AttraidiesMask.ShieldBroken = true;
                NPC.life = 1000;
                damage = 0;
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (NPC.life - damage <= 1000)
            {
                AttraidiesMask.ShieldBroken = true;
                if(NPC.realLife != -1)
                {
                    Main.npc[NPC.realLife].life = 1001;
                }
                else
                {
                    NPC.life = 1001;
                }
                damage = 0;
            }

            for(int i = 0; i < Main.maxNPCs; i++)
            {

            }
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
