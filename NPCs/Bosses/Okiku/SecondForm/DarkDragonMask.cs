using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Okiku.SecondForm {
    [AutoloadBossHead]
    public class DarkDragonMask : ModNPC {

        public bool DragonSpawned = false;
        public bool DragonDead = false;
        public int TimerRain = 0;
        public int DragonIndex = 0;
        public float TimerSpawn = 0;
        public bool ChannellingDragon = false;
        int randPosX = 0;
        int nextRandPosX = 0;

        public override void SetDefaults() {
            npc.width = 28;
            npc.height = 44;
            npc.aiStyle = -1;
            npc.damage = 1;
            npc.defense = 15;
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.lifeMax = 14000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.knockBackResist = 0f;
            npc.npcSlots = 6f;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Confused] = true;
            despawnHandler = new NPCDespawnHandler("You've been slain at the hand of Attraidies...", Color.DarkMagenta, DustID.PurpleCrystalShard);
        }

        public override void SetStaticDefaults() {
            Main.npcFrameCount[npc.type] = 7;
            DisplayName.SetDefault("Attraidies");
        }

        public int ObscureDropDamage = 50;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) {
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            //Basic despawn handler and passive dust
            despawnHandler.TargetAndDespawn(npc.whoAmI);            
            int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, 0, 0, 100, Color.White, 1.0f);
            Main.dust[dust].noGravity = true;

            DispelGravitation();

            //Spawn dragon
            if (DragonSpawned == false) {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    DragonIndex = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<ShadowDragonHead>(), npc.whoAmI);
                    Main.npc[DragonIndex].velocity.Y = -10;
                }
                DragonSpawned = true;
            }           
            if (!NPC.AnyNPCs(ModContent.NPCType<ShadowDragonHead>()))
            {
                DragonDead = true;
                TimerSpawn++;
                if (TimerSpawn == 1) //teleport above player right after dragon is killed
                {
                    randPosX = nextRandPosX;
                    nextRandPosX = Main.rand.Next(-250, 250);
                    npc.netUpdate = true;                    
                }

                //While we're in the "spam obscure drops" phase               
                if (TimerSpawn <= 600)
                {
                    npc.position.X = Main.player[npc.target].position.X + randPosX;
                    npc.position.Y = Main.player[npc.target].position.Y - 300;

                    TimerRain++;
                    if (TimerRain >= 2)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.Center.X, npc.Center.Y, Main.rand.Next(-120, 120) / 10, -7, ModContent.ProjectileType<ObscureDrop>(), ObscureDropDamage, 0f, Main.myPlayer);

                            if (Main.rand.Next(4) == 0)
                            {
                                Projectile.NewProjectile(npc.Center.X, npc.Center.Y, Main.rand.Next(Main.rand.Next(-160, -120), Main.rand.Next(120, 160)) / 10, -7, ModContent.ProjectileType<ObscureDrop>(), ObscureDropDamage, 0f, Main.myPlayer);
                            }
                        }

                        TimerRain = 0;
                    }                    
                }
                
                //While we're in the "channeling dragon" phase
                if (TimerSpawn > 600 && TimerSpawn < 780)
                {
                    npc.velocity.X = 0;
                    npc.velocity.Y = 0;
                    ChannellingDragon = true;
                }

                if (TimerSpawn >= 780)
                {
                    ChannellingDragon = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        DragonIndex = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<ShadowDragonHead>(), npc.whoAmI);
                        Main.npc[DragonIndex].velocity.Y = -10;
                    }
                    TimerSpawn = 0;
                }
            }
            else
            {
                DragonDead = false;
                //If we can't find the dragon, relocate it.
                if (!Main.npc[DragonIndex].active || (Main.npc[DragonIndex].type != ModContent.NPCType<ShadowDragonHead>()))
                {
                    int? newIndex = UsefulFunctions.GetFirstNPC(ModContent.NPCType<ShadowDragonHead>());
                    if(newIndex != null)
                    {
                        DragonIndex = newIndex.Value;
                    }
                }

                npc.Center = Main.npc[DragonIndex].Center;                
            }
        }

        public void DispelGravitation()
        {
            //Dispel gravitation buffs
            for (int i = 0; i < 10; i++)
            {
                if (Main.player[npc.target].buffType[i] == 18)
                {
                    Main.player[npc.target].buffType[i] = 0;
                    Main.player[npc.target].buffTime[i] = 0;
                    if (Main.netMode != NetmodeID.Server && Main.myPlayer == npc.target)
                    {
                        Main.NewText("What a horrible night to have your Gravitation buff dispelled...", 150, 150, 150);
                    }
                    break;
                }
            }
        }
        public override bool CheckActive()
        {
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(nextRandPosX);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            nextRandPosX = reader.ReadInt32();
        }

        public override void NPCLoot()
        {            
            for (int i = 0; i < 50; i++)
            {
                int dustDeath = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 54, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, Color.White, 4f);
                Main.dust[dustDeath].noGravity = true;
            }

            if(Main.npc[DragonIndex].type == ModContent.NPCType<ShadowDragonHead>()){
                Main.npc[DragonIndex].life = 0;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, ModContent.NPCType<ThirdForm.Okiku>(), 0);
                UsefulFunctions.ServerText("??????????????????? A booming laughter echoes all around you!", new Color(175, 75, 255));
            }
        }


        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
        public override void FindFrame(int frameHeight) {

            int num = 1;
            if (!Main.dedServ)
            {
                num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
            }

            if ((npc.velocity.X > -2 && npc.velocity.X < 2) && (npc.velocity.Y > -2 && npc.velocity.Y < 2) && !ChannellingDragon)
            {
                npc.frameCounter = 0;
                npc.frame.Y = 0;
                if (npc.position.X > Main.player[npc.target].position.X)
                {
                    npc.spriteDirection = -1;
                }
                else
                {
                    npc.spriteDirection = 1;
                }
            }

            if (DragonDead)
            {
                if (npc.alpha > 40) npc.alpha -= 1;
                if (npc.alpha < 40) npc.alpha += 1;
            }
            else
            {
                if (npc.alpha < 200) npc.alpha += 1;
                if (npc.alpha > 200) npc.alpha -= 1;
            }

            if (ChannellingDragon)
            {
                npc.spriteDirection = npc.direction;
                npc.frameCounter++;
                if (npc.frameCounter < 8)
                {
                    npc.frame.Y = num * 3;
                }
                else if (npc.frameCounter < 16)
                {
                    npc.frame.Y = num * 4;
                }
                else if (npc.frameCounter < 24)
                {
                    npc.frame.Y = num * 5;
                }
                else if (npc.frameCounter < 32)
                {
                    npc.frame.Y = num * 4;
                }
                else
                {
                    npc.frameCounter = 0;
                }
            }

            else
            {
                if (npc.velocity.X > 1.5f) npc.frame.Y = num;
                if (npc.velocity.X < -1.5f) npc.frame.Y = num * 2;
                if (npc.velocity.X > -1.5f && npc.velocity.X < 1.5f) npc.frame.Y = 0;
            }
        }
    }
}
