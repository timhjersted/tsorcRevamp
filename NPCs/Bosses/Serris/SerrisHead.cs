using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Serris
{
    [AutoloadBossHead]
    class SerrisHead : ModNPC
    {
        public override void SetDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPC.netAlways = true;
            NPC.npcSlots = 5;
            NPC.width = 38;
            NPC.height = 70;
            NPC.aiStyle = 6;
            NPC.timeLeft = 22750;
            NPC.damage = 80;
            NPC.defense = 999999;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0;
            NPC.lifeMax = 6000;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.behindTiles = true;
            NPC.boss = true;
            Music = 12;
            NPC.value = 300000;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;

            //If one already exists, don't add text to the others despawnhandler (so it doesn't show duplicate messages if you die)
            if (NPC.CountNPCS(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()) > 1)
            {
                despawnHandler = new NPCDespawnHandler(DustID.Firework_Blue);
            }
            else
            {
                despawnHandler = new NPCDespawnHandler("Serris retreats to the depths of its temple...", Color.Cyan, DustID.Firework_Blue);
            }
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Serris");
        }
        int distortionDamage = 90;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage * 1.3 / 2);
            NPC.defense = NPC.defense += 12;
            distortionDamage = distortionDamage / 2;
        }


        bool tailSpawned = false;
        bool speedBoost = false;
        bool timeLock = false;
        int SoundDelay = 0;
        int srs = 0;
        int Previous = 0;
        public static int[] bodyTypes = new int[] { ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>() };
        int projCooldown = 0;
        int nextSegment;
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (!Main.npc[nextSegment].active)
            {
                tailSpawned = false;
            }

            if (Math.Abs(NPC.position.Y - Main.player[NPC.target].position.Y) > 1800 || Math.Abs(NPC.position.Y - Main.player[NPC.target].position.Y) > 3200)
            {
                float angle = Main.rand.Next(0, 360);
                NPC.position.X = Main.player[NPC.target].position.X + (100 * (float)Math.Cos(angle) * 16);
                NPC.position.Y = Main.player[NPC.target].position.Y + (100 * (float)Math.Sin(angle) * 16);
            }

            if ((NPC.life % 1000) != 0 && NPC.life > 1)
            {
                NPC.life -= NPC.life % 1000;
                if (NPC.life <= 0)
                {
                    NPC.life = 1;
                }

                timeLock = false;
                NPC.ai[0] = 2;
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, NPC.Center);
                NPC.netUpdate = true;
            }

            if (NPC.velocity.X < 0f)
            {
                NPC.spriteDirection = -1;
            }
            if (NPC.velocity.X > 0f)
            {
                NPC.spriteDirection = 1;
            }

            int maxBoostedSpeed = 10;
            int maxNormalSpeed = 7;
            if (speedBoost)
            {
                NPC.velocity = Vector2.Clamp(NPC.velocity, new Vector2(-maxBoostedSpeed, -maxBoostedSpeed), new Vector2(maxBoostedSpeed, maxBoostedSpeed));
            }
            else
            {
                NPC.velocity = Vector2.Clamp(NPC.velocity, new Vector2(-maxNormalSpeed, -maxNormalSpeed), new Vector2(maxNormalSpeed, maxNormalSpeed));
            }


            NPC.ai[0]++;
            if (NPC.ai[0] <= 1 || NPC.ai[0] >= 400)
            {
                speedBoost = false;
                timeLock = true;
                SoundDelay = 0;
                NPC.dontTakeDamage = false;
                NPC.damage = 80;
                Main.npc[srs].damage = 80;

                if (!tailSpawned)
                {
                    Previous = NPC.whoAmI;
                    for (int num36 = 0; num36 < 15; num36++)
                    {
                        if (num36 >= 0 && num36 < 14)
                        {
                            srs = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), NPC.whoAmI);
                        }
                        else
                        {
                            srs = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Bosses.Serris.SerrisTail>(), NPC.whoAmI);
                        }
                        if (num36 == 0)
                        {
                            nextSegment = NPC.whoAmI;
                        }
                        Main.npc[srs].realLife = NPC.whoAmI;
                        Main.npc[srs].ai[2] = (float)NPC.whoAmI;
                        Main.npc[srs].ai[1] = (float)Previous;
                        Main.npc[Previous].ai[0] = (float)srs;
                        NetMessage.SendData(23, -1, -1, null, srs, 0f, 0f, 0f, 0);
                        Previous = srs;
                    }
                    tailSpawned = true;
                }
            }
            else if (NPC.ai[0] >= 2)
            {
                NPC.dontTakeDamage = true;
                NPC.position += NPC.velocity * 1.5f;
                speedBoost = true;
                SoundDelay++;
                NPC.damage = 110;
                Main.npc[srs].damage = 110;
                if (SoundDelay > 14)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/SpeedBooster") with { Volume = 1f }, NPC.Center);
                    SoundDelay = 0;
                }
            }
            if (timeLock)
            {
                NPC.ai[0] = 0;
            }

            if (projCooldown > 0)
            {
                projCooldown--;
            }
            else
            {
                //If this isn't the only one left, fire at lower speed to prevent spamming the player to death
                if (NPC.CountNPCS(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()) > 1)
                {
                    projCooldown = 250;
                }
                else
                {
                    projCooldown = 100;
                }
                float speed = 9f;
                Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float distanceFactor = Vector2.Distance(vector8, Main.player[NPC.target].position) / speed;
                float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) / distanceFactor;
                float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) / distanceFactor;
                float angle = (float)Math.Atan2(speedY, speedX);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.GravityDistortion>(), distortionDamage, 0f, Main.myPlayer, 0, speed);
            }

        }

        public override bool CheckActive()
        {
            return false;
        }
        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            Main.npc[srs].active = false;
            Main.npc[Previous].active = false;
            if (!(NPC.CountNPCS(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()) > 1))
            {
                UsefulFunctions.BroadcastText("Serris has transformed!", Color.Cyan);
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)(NPC.position.X + (float)(NPC.width / 2)), (int)(NPC.position.Y + (float)NPC.height), ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>(), 0);
            }
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Serris Gore 1").Type, 1f);
            }
        }
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            NPC.frameCounter += 1.0;
            if (speedBoost)
            {
                if (NPC.frameCounter >= 0 && NPC.frameCounter < 15)
                {
                    NPC.frame.Y = num;
                }
                if (NPC.frameCounter >= 15 && NPC.frameCounter < 30)
                {
                    NPC.frame.Y = num * 2;
                }
                if (NPC.frameCounter >= 30)
                {
                    NPC.frameCounter = 0;
                }
            }
            else
            {
                NPC.frame.Y = 0;
                NPC.frameCounter = 0;
            }
        }
    }
}