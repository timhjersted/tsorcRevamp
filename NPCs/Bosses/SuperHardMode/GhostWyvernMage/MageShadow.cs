using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage
{

    class MageShadow : ModNPC
    {
        int frozenSawDamage = 50;
        int lightningDamage = 60;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            AnimationType = 29;
            NPC.aiStyle = 0;
            NPC.damage = 0;
            NPC.defense = 56;
            NPC.height = 44;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 500;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.alpha = 249;
            NPC.lavaImmune = true;
            NPC.value = 0;
            NPC.width = 28;
            NPC.knockBackResist = 0f;
        }
        int Timer2 = -Main.rand.Next(200);
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.Frostburn, 1 * 60 + 30, false);
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 300 * 60, false);
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 300 * 60, false);
        }


        #region AI

        public override void AI()
        {

            //Proximity Debuffs
            Player player = Main.player[NPC.target];
            if (NPC.Distance(player.Center) < 300)
            {
               
                player.AddBuff(BuffID.Darkness, 300, false);

            }
            if (NPC.Distance(player.Center) < 100)
            {
                player.AddBuff(BuffID.CursedInferno, 30, false);
                //player.AddBuff(BuffID.Obstructed, 60, false);
                player.AddBuff(BuffID.ManaSickness, 120, false);
            }

            NPC.netUpdate = false;
            NPC.ai[0]++; // Timer Scythe

            NPC.ai[1]++; // Timer Teleport


            // npc.ai[2]++; // Shots

            if (NPC.life < NPC.lifeMax / 3)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, Type: DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y, 150, Color.Blue, 1f);
                Main.dust[dust].noGravity = true;
            }


            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (NPC.ai[0] >= 12 && NPC.ai[2] < 3)
                {
                    float num48 = 3f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    int type = ModContent.ProjectileType<Projectiles.Enemy.FrozenSawII>();
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, frozenSawDamage, 0f, Main.myPlayer);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    NPC.ai[0] = 0;
                    NPC.ai[2]++;
                    //Timer2 = 0;
                }
            }


            if (NPC.ai[1] >= 10)
            {
                NPC.velocity.X *= 0.77f;
                NPC.velocity.Y *= 0.27f;
            }

            if ((NPC.ai[1] >= 300 && NPC.life > NPC.lifeMax / 3) || (NPC.ai[1] >= 200 && NPC.life <= NPC.lifeMax / 3))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                for (int num36 = 0; num36 < 10; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 4f);
                    Main.dust[dust].noGravity = false;
                }
                NPC.ai[3] = 0;//(float)(Main.rand.Next(360) * (Math.PI / 180));
                NPC.ai[2] = 0;
                NPC.ai[1] = 0;
                

                Player Pt = Main.player[NPC.target];
                Vector2 NC = NPC.position + new Vector2(NPC.width / 2, NPC.height / 2);
                Vector2 PtC = Pt.position + new Vector2(Pt.width / 2, Pt.height / 2);
                NPC.position.X = Pt.position.X + (float)((600 * Math.Cos(NPC.ai[3])) * -1);
                NPC.position.Y = Pt.position.Y - 45 + (float)((30 * Math.Sin(NPC.ai[3])) * -1);

                float MinDIST = 600f;
                float MaxDIST = 900f;
                Vector2 Diff = NPC.position - Pt.position;
                if (Diff.Length() > MaxDIST)
                {
                    Diff *= MaxDIST / Diff.Length();
                }
                if (Diff.Length() < MinDIST)
                {
                    Diff *= MinDIST / Diff.Length();
                }
                NPC.position = Pt.position + Diff;

                NC = NPC.position + new Vector2(NPC.width / 2, NPC.height / 2);

                float rotation = (float)Math.Atan2(NC.Y - PtC.Y, NC.X - PtC.X);
                NPC.velocity.X = (float)(Math.Cos(rotation) * 13) * -1;
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 13) * -1;

            }

            
            Timer2++;
            if (Timer2 >= 120)
            {


                bool clearSpace = true;
                for (int i = 0; i < 15; i++)
                {
                    if (UsefulFunctions.IsTileReallySolid((int)NPC.Center.X / 16, ((int)NPC.Center.Y / 16) - i))
                    {
                        clearSpace = false;
                    }
                }

                if (clearSpace)
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);


                    speed.Y += Main.rand.NextFloat(-2f, -6f);
                    //speed += Main.rand.NextVector2Circular(-10, -8);
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            if (Main.rand.NextBool(4))
                            {
                                int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, 80, 0f, Main.myPlayer);
                                //ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>()
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                            }
                        }
                        
                        if (Timer2 >= 400)
                        {
                            Timer2 = -100;

                            if (Main.rand.NextBool(2))
                            {
                                if (Main.netMode != NetmodeID.MultiplayerClient)
                                {
                                    float num48 = 2f;
                                    Vector2 vector9 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 220 + (NPC.height / 2));
                                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
                                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                                    {
                                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                                        num51 = num48 / num51;
                                        speedX *= num51;
                                        speedY *= num51;
                                        int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Ball>();//44;//0x37; //14;
                                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, speedX, speedY, type, lightningDamage, 0f, Main.myPlayer);
                                        Main.projectile[num54].timeLeft = 250;
                                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item25, NPC.Center);
                                        NPC.ai[3] = 0;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Timer2 = -100;
                }

            }
            

            NPC.ai[3]++;

            if (NPC.ai[3] >= 50) //how often the crystal attack can happen in frames per second
            {
                


            }

            NPC.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (NPC.ai[3] >= 800)
            {
                if (NPC.ai[1] == 0) NPC.ai[1] = 1;
                else NPC.ai[1] = 0;
            }
        }

        #endregion

        public override void FindFrame(int currentFrame)
        {

            if ((NPC.velocity.X > -9 && NPC.velocity.X < 9) && (NPC.velocity.Y > -9 && NPC.velocity.Y < 9))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
                if (NPC.position.X > Main.player[NPC.target].position.X)
                {
                    NPC.spriteDirection = -1;
                }
                else
                {
                    NPC.spriteDirection = 1;
                }
            }

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
            else
            {
                NPC.frameCounter += 1.0;
            }
            if (NPC.frameCounter >= 1.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        #region Gore
        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            for (int num36 = 0; num36 < 50; num36++)
            {
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height * 4, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height * 4, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height * 4, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height * 4, DustID.PurpleTorch, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                }
            }
        }
        #endregion

        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            GhostDragonHead.GhostEffect(NPC, spriteBatch, ref texture, 0.9f);
            return true;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            GhostDragonHead.GhostEffect(NPC, spriteBatch, ref texture, 1f);
        }

    }
}