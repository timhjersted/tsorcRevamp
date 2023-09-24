using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;
using Terraria.GameContent.ItemDropRules;
using Terraria.DataStructures;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Okiku.SecondForm
{
    [AutoloadBossHead]
    public class DarkDragonMask : ModNPC
    {

        public bool DragonSpawned = false;
        public bool DragonDead = false;
        public int TimerRain = 0;
        public int DragonIndex = 0;
        public float TimerSpawn = 0;
        public bool ChannellingDragon = false;
        int randPosX = 0;
        int nextRandPosX = 0;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 7;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }
        public override void SetDefaults()
        {
            NPC.width = 28;
            NPC.height = 44;
            NPC.aiStyle = -1;
            NPC.damage = 1; //curious why this is 1?
            NPC.defense = 36;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lifeMax = 14000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            NPC.npcSlots = 6f;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.DarkDragonMask.DespawnHandler"), Color.DarkMagenta, DustID.PurpleCrystalShard);
        }

        public int ObscureDropDamage = 60;
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            //Basic despawn handler and passive dust
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, 0, 0, 100, Color.White, 1.0f);
            Main.dust[dust].noGravity = true;

            DispelGravitation();

            //Spawn dragon
            if (DragonSpawned == false)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    DragonIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<ShadowDragonHead>(), NPC.whoAmI);
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
                    nextRandPosX = Main.rand.Next(-150, 150);
                    NPC.netUpdate = true;
                }

                //While we're in the "spam obscure drops" phase               
                if (TimerSpawn <= 600)
                {
                    NPC.position.X = Main.player[NPC.target].position.X + randPosX;
                    NPC.position.Y = Main.player[NPC.target].position.Y - 250;

                    TimerRain++;
                    if (TimerRain >= 2)
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, Main.rand.Next(-120, 120) / 10, -7, ModContent.ProjectileType<ObscureDrop>(), ObscureDropDamage, 0f, Main.myPlayer);

                            if (Main.rand.NextBool(4))
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, Main.rand.Next(Main.rand.Next(-160, -120), Main.rand.Next(120, 160)) / 10, -7, ModContent.ProjectileType<ObscureDrop>(), ObscureDropDamage, 0f, Main.myPlayer);
                            }
                        }

                        TimerRain = 0;
                    }
                }

                //While we're in the "channeling dragon" phase
                if (TimerSpawn > 600 && TimerSpawn < 780)
                {
                    NPC.velocity.X = 0;
                    NPC.velocity.Y = 0;
                    ChannellingDragon = true;
                }

                if (TimerSpawn >= 780)
                {
                    ChannellingDragon = false;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        DragonIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<ShadowDragonHead>(), NPC.whoAmI);
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
                    if (newIndex != null)
                    {
                        DragonIndex = newIndex.Value;
                    }
                }

                NPC.Center = Main.npc[DragonIndex].Center;
            }
        }

        public void DispelGravitation()
        {
            //Dispel gravitation buffs
            for (int i = 0; i < 10; i++)
            {
                if (Main.player[NPC.target].buffType[i] == 18)
                {
                    Main.player[NPC.target].buffType[i] = 0;
                    Main.player[NPC.target].buffTime[i] = 0;
                    if (Main.netMode != NetmodeID.Server && Main.myPlayer == NPC.target)
                    {
                        //This one can stay a NewText because it already checks != server and does need to run only for that one player
                        Main.NewText(LangUtils.GetTextValue("NPCs.DarkDragonMask.NoGravitaion"), 150, 150, 150);
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

        public override void OnKill()
        {
            for (int i = 0; i < 50; i++)
            {
                int dustDeath = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.White, 4f);
                Main.dust[dustDeath].noGravity = true;
            }

            if (Main.npc[DragonIndex].type == ModContent.NPCType<ShadowDragonHead>())
            {
                Main.npc[DragonIndex].life = 0;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<ThirdForm.Okiku>(), 0);
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.DarkDragonMask.Laughter"), new Color(175, 75, 255));
            }
        }


        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
        public override void FindFrame(int frameHeight)
        {

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }

            if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2) && !ChannellingDragon)
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

            if (DragonDead)
            {
                if (NPC.alpha > 40) NPC.alpha -= 1;
                if (NPC.alpha < 40) NPC.alpha += 1;
            }
            else
            {
                if (NPC.alpha < 200) NPC.alpha += 1;
                if (NPC.alpha > 200) NPC.alpha -= 1;
            }

            if (ChannellingDragon)
            {
                NPC.spriteDirection = NPC.direction;
                NPC.frameCounter++;
                if (NPC.frameCounter < 8)
                {
                    NPC.frame.Y = num * 3;
                }
                else if (NPC.frameCounter < 16)
                {
                    NPC.frame.Y = num * 4;
                }
                else if (NPC.frameCounter < 24)
                {
                    NPC.frame.Y = num * 5;
                }
                else if (NPC.frameCounter < 32)
                {
                    NPC.frame.Y = num * 4;
                }
                else
                {
                    NPC.frameCounter = 0;
                }
            }

            else
            {
                if (NPC.velocity.X > 1.5f) NPC.frame.Y = num;
                if (NPC.velocity.X < -1.5f) NPC.frame.Y = num * 2;
                if (NPC.velocity.X > -1.5f && NPC.velocity.X < 1.5f) NPC.frame.Y = 0;
            }
        }
    }
}
