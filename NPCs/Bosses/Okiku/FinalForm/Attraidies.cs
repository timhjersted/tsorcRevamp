using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Okiku.FinalForm {
    [AutoloadBossHead]
    class Attraidies : ModNPC {

        float customspawn1;

        public override string Texture => "tsorcRevamp/NPCs/Bosses/Okiku/FirstForm/DarkShogunMask";
        public override void SetDefaults() {
            npc.aiStyle = 0;
            npc.npcSlots = 200;
            npc.damage = 70;
            npc.defense = 25;
            npc.height = 44;
            npc.timeLeft = 22500;
            music = 12;
            npc.lifeMax = 50000;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath14;
            npc.noGravity = false;
            npc.noTileCollide = false;
            npc.boss = true;
            npc.value = 500000;
            npc.width = 28;
            npc.lavaImmune = true;
            npc.knockBackResist = 0;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Confused] = true;
            Main.npcFrameCount[npc.type] = 3;
        }



        public override float SpawnChance(NPCSpawnInfo spawnInfo) {
            if (NPC.AnyNPCs(ModContent.NPCType<Attraidies>())) {
                return 0;
            }

            float chance = 0;
            var p = spawnInfo.player;
            bool nospecialbiome = !p.ZoneJungle && !p.ZoneCorrupt && !p.ZoneCrimson && !p.ZoneHoly && !p.ZoneMeteor && !p.ZoneDungeon;
            bool surface = nospecialbiome && !p.ZoneSkyHeight && (p.position.Y <= Main.worldSurface);

            if (Main.hardMode && Main.bloodMoon && !tsorcRevampWorld.SuperHardMode && surface) {
                chance = (1f / 38500f);
            }
            
            return chance;
        }

        #region AI
        public override void AI() {

            npc.netUpdate = false;
            npc.ai[0]++; // Timer Scythe
            npc.ai[1]++; // Timer Teleport
                         // npc.ai[2]++; // Shots

            if (npc.life > 6000) {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X, npc.velocity.Y, 200, Color.Red, 1f);
                Main.dust[dust].noGravity = true;
            }
            else if (npc.life <= 6000) {
                int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X, npc.velocity.Y, 140, Color.Red, 2f);
                Main.dust[dust].noGravity = true;
            }

            if (npc.ai[0] >= 12 && npc.ai[2] < Main.rand.Next(3, 7))
            {
                float num48 = 0.5f;
                Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                int damage = 60;
                int type = ModContent.ProjectileType<ShadowOrb>();
                float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                Projectile.NewProjectile(vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, damage, 0f, 0);
                Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 20);
                npc.ai[0] = 0;
                npc.ai[2]++;
                npc.netUpdate = true; //new
            }


            if (npc.ai[1] >= 30) {
                npc.velocity.X *= 0.17f;
                npc.velocity.Y *= 0.17f;
            }

            if ((npc.ai[1] >= 230 && npc.life > 6000) || (npc.ai[1] >= 110 && npc.life <= 6000)) {
                Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 8);
                for (int num36 = 0; num36 < 10; num36++) {
                    int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 4f);
                    Main.dust[dust].noGravity = false;
                }
                npc.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                npc.ai[2] = 0;
                npc.ai[1] = 0;
                if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active) {
                    npc.TargetClosest(true);
                }
                if (Main.player[npc.target].dead) {
                    npc.position.X = 0;
                    npc.position.Y = 0;
                    if (npc.timeLeft > 10) {
                        npc.timeLeft = 0;
                        return;
                    }
                }
                else {

                    Player Pt = Main.player[npc.target];
                    Vector2 PtC = Pt.position + new Vector2(Pt.width / 2, Pt.height / 2);
                    npc.position.X = Pt.position.X + (float)((600 * Math.Cos(npc.ai[3])) * -1);
                    npc.position.Y = Pt.position.Y - 35 + (float)((30 * Math.Sin(npc.ai[3])) * -1);

                    float MinDIST = 360f;
                    float MaxDIST = 410f;
                    Vector2 Diff = npc.position - Pt.position;
                    if (Diff.Length() > MaxDIST) {
                        Diff *= MaxDIST / Diff.Length();
                    }
                    if (Diff.Length() < MinDIST) {
                        Diff *= MinDIST / Diff.Length();
                    }
                    npc.position = Pt.position + Diff;

                    Vector2 NC = npc.position + new Vector2(npc.width / 2, npc.height / 2);

                    float rotation = (float)Math.Atan2(NC.Y - PtC.Y, NC.X - PtC.X);
                    npc.velocity.X = (float)(Math.Cos(rotation) * 8) * -1;
                    npc.velocity.Y = (float)(Math.Sin(rotation) * 8) * -1;

                }
            }

            //end of W1k's Death code

            //beginning of Omnir's Ultima Weapon projectile code

            npc.ai[3]++;

            if (npc.ai[3] >= 45) //how often the crystal attack can happen in frames per second
            {

                if (Main.rand.Next(2) == 0) //1 in 2 chance boss will use attack when it flies down on top of you
                {
                    float num48 = 8f;
                    Vector2 vector9 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 520 + (npc.height / 2));
                    float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int damage = 120;//(int) (14f * npc.scale);
                        int type = ModContent.ProjectileType<MassiveCrystalShardsSpell>();
                        int num54 = Projectile.NewProjectile(vector9.X, vector9.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 100;
                        Main.projectile[num54].aiStyle = 4;
                        Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 25);
                        npc.ai[3] = 0; ;
                    }
                }




                if (Main.rand.Next(30) == 0) //1 in 20 chance boss will summon an NPC
                {
                    int Random = Main.rand.Next(100);
                    int Paraspawn = 0;

                    if (Random == 0) Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X - 406 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 16 - this.npc.width / 2, NPCID.ChaosElemental, 0);

                    if (Random == 0) Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X + 406 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 16 - this.npc.width / 2, NPCID.ChaosElemental, 0);

                    Main.npc[Paraspawn].velocity.X = npc.velocity.X;
                    npc.active = true;
                }

                if (Main.rand.Next(20) == 0) //1 in 20 chance boss will summon an NPC
                {
                    int Random = Main.rand.Next(120);
                    int Paraspawn = 0;

                    if (Random == 0) Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X + 800 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 500 - this.npc.width / 2, ModContent.NPCType<Enemies.MindflayerIllusion>(), 0); //Mindflayer Illusion

                    Main.npc[Paraspawn].velocity.X = npc.velocity.X;
                    npc.active = true;
                }

                //if ((npc.life < 45000 && npc.life < 50000) || (npc.life >= 40000 && npc.life <=45000) || (npc.life >= 40000 && npc.life <=45000) || (npc.life >= 40000 && npc.life <=45000))
                //{

                if (customspawn1 > 0 && Main.rand.Next(7000) == 0) {
                    customspawn1 = 0;
                }


                if (Main.netMode != NetmodeID.MultiplayerClient) {

                    if (customspawn1 == 0) {

                        if ((customspawn1 < 1) && Main.rand.Next(2030) == 1) {
                            int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<SecondForm.ShadowDragonHead>(), 0);
                            Main.npc[Spawned].velocity.Y = -8;
                            Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
                            //npc.ai[5] = 20-Main.rand.Next(80);
                            customspawn1 += 1f;
                            if (Main.netMode == NetmodeID.Server) {
                                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
                            }
                        }
                    }
                }


                if (Main.rand.Next(70) == 0) //1 in 25 chance boss will summon an NPC
                                          {
                    int Random = Main.rand.Next(150);
                    int Paraspawn = 0;


                    if (Random == 50) Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X + 900 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 500 - this.npc.width / 2, ModContent.NPCType<AttraidiesMimic>(), 0);


                    Main.npc[Paraspawn].velocity.X = npc.velocity.X;
                    npc.active = true;

                }


                if (Main.rand.Next(80) == 0) //1 in 60 chance boss will summon an NPC
                                          {
                    int Random = Main.rand.Next(180);
                    int Paraspawn = 0;


                    if (Random == 5) Paraspawn = NPC.NewNPC((int)Main.player[this.npc.target].position.X + 900 - this.npc.width / 2, (int)Main.player[this.npc.target].position.Y - 500 - this.npc.width / 2, ModContent.NPCType<Enemies.DiscipleOfAttraidies>(), 0);



                    Main.npc[Paraspawn].velocity.X = npc.velocity.X;
                    npc.active = true;



                }






            }

            npc.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (npc.ai[3] >= 700) {
                if (npc.ai[1] == 0) npc.ai[1] = 1;
                else npc.ai[1] = 0;
            }


            if (Main.rand.Next(100) == 1) {
                float num48 = 10f;
                Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) + Main.rand.Next(-20, 0x15);
                float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) + Main.rand.Next(-20, 0x15);
                if (((speedX < 0f) && (npc.velocity.X < 0f)) || ((speedX > 0f) && (npc.velocity.X > 0f))) {
                    float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                    num51 = num48 / num51;
                    speedX *= num51;
                    speedY *= num51;
                    int damage = 120;//(int) (14f * npc.scale);
                    int type = ModContent.ProjectileType<EnemySuddenDeathBall>();
                    int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, type, damage, 0f, Main.myPlayer);
                    Main.projectile[num54].timeLeft = 6;
                    Main.projectile[num54].aiStyle = 1;
                    Main.PlaySound(SoundID.Item, (int)npc.position.X, (int)npc.position.Y, 0x11);
                }
                npc.netUpdate = true;
            }


            if (Main.player[npc.target].dead) {
                npc.velocity.Y -= 0.04f;
                if (npc.timeLeft > 10) {
                    npc.timeLeft = 0;
                    return;
                }
            }
        }

        #endregion

        public override void NPCLoot() {


            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.TheEnd>());
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.GuardianSoul>());
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulOfAttraidies>(), Main.rand.Next(15, 23));
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 2000);
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.BloomShards>());



            if (!tsorcRevampWorld.SuperHardMode) {

                if (Main.netMode == NetmodeID.SinglePlayer) {
                    Main.NewText("A portal from The Abyss has been opened!");
                    Main.NewText("Artorias, the Ancient Knight of the Abyss has entered this world!...");
                    Main.NewText("You must seek out the Shaman Elder...");

                }
                else if (Main.netMode == NetmodeID.Server) {
                    NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("A portal from The Abyss has been opened!"), new Color(255, 255, 60));
                    NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("Artorias, the Ancient Knight of the Abyss has entered this world!"), new Color(255, 255, 60));
                    NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("You must seek out the Shaman Elder..."), new Color(255, 255, 60));
                }


                Main.hardMode = true;
                tsorcRevampWorld.SuperHardMode = true;
                tsorcRevampWorld.TheEnd = false;

            }

            else if (tsorcRevampWorld.SuperHardMode) {

                if (Main.netMode == NetmodeID.SinglePlayer) {
                    Main.NewText("The portal from The Abyss remains open...");
                    Main.NewText("You must seek out the Shaman Elder...");

                }
                else if (Main.netMode == NetmodeID.Server) {
                    NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("The portal from The Abyss remains open..."), new Color(255, 255, 60));
                    NetMessage.BroadcastChatMessage(NetworkText.FromLiteral("You must seek out the Shaman Elder..."), new Color(255, 255, 60));
                }

                tsorcRevampWorld.SuperHardMode = true;
                Main.hardMode = true;
                tsorcRevampWorld.TheEnd = false;

            }
        }
    }
}
