using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Okiku.ThirdForm {
    [AutoloadBossHead]
    public class BrokenOkiku : ModNPC {

        int lookMode;
        bool ShieldBroken;
        int attackPhase;
        int subPhase;
        int genericTimer;
        int genericTimer2;
        int phaseTime;
        bool phaseStarted;
        bool Initialized = false;
        public override string Texture => "tsorcRevamp/NPCs/Bosses/Okiku/FirstForm/DarkShogunMask";

        public override void SetStaticDefaults() {
            Main.npcFrameCount[npc.type] = 3;
            DisplayName.SetDefault("Mindflayer Illusion");
        }

        public override void SetDefaults() {
            npc.npcSlots = 10;
            npc.width = 58;
            npc.height = 121;
            npc.damage = 70;
            npc.defense = 30;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath6;
            npc.lifeMax = 8000;
            npc.timeLeft = 22500;
            npc.friendly = false;
            npc.boss = true;
            npc.noTileCollide = true;
            npc.noGravity = true;
            npc.knockBackResist = 0;
            npc.lavaImmune = true;
            npc.value = 350000;
        }

        public void Teleport(float X, float Y) {
            int dustDeath;
            for (int num36 = 0; num36 < 20; num36++) {
                dustDeath = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 200, Color.White, 6f);
                Main.dust[dustDeath].noGravity = true;
            }
            npc.position.X = X;
            npc.position.Y = Y;
            npc.velocity.X = 0;
            npc.velocity.Y = 0;
            for (int num36 = 0; num36 < 20; num36++) {
                dustDeath = Dust.NewDust(new Vector2(X, Y), npc.width, npc.height, 54, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, Color.White, 6f);
                Main.dust[dustDeath].noGravity = true;
            }
        }

        public override void AI() {

            if (!Initialized) {
                lookMode = 0; //0 = Stand, 1 = Player's Direction, 2 = Movement Direction.
                ShieldBroken = false;
                attackPhase = -1;
                subPhase = 0;
                genericTimer = 0;
                genericTimer2 = 0;
                phaseTime = 90;
                phaseStarted = false;
                Initialized = true;
            }
            npc.TargetClosest(true);

            if (npc.target < 0 || npc.target == 255 || Main.player[npc.target].dead || !Main.player[npc.target].active) {
                Teleport(-1000, -1000);
                npc.timeLeft = 0;
            }



            Vector2 vector8 = new Vector2(npc.position.X + (npc.width / 2), npc.position.Y + (npc.height / 2));
            genericTimer++;
            if (attackPhase == -1) {
                lookMode = 0;
                phaseTime = 120;
            }

            if (attackPhase == 0) // PHASE 0
            {
                if (!phaseStarted) {
                    lookMode = 1;
                    phaseTime = 60;
                    if (Main.rand.Next(2) == 0) Teleport(Main.player[npc.target].position.X - 500, Main.player[npc.target].position.Y + 400);
                    else Teleport(Main.player[npc.target].position.X + 500, Main.player[npc.target].position.Y + 400);
                    phaseStarted = true;
                }
                bool left = false;
                if (npc.position.X < Main.player[npc.target].position.X) left = false;
                if (npc.position.X > Main.player[npc.target].position.X) left = true;
                genericTimer2++;
                npc.velocity.Y = -15;
                if (genericTimer2 == 10) {
                    int num54 = 0;
                    if (left) {
                        num54 = Projectile.NewProjectile(vector8.X, vector8.Y, -6 + Main.rand.Next(-1, 1), Main.rand.Next(-10, 10) / 5, ModContent.ProjectileType<CrazedOrb>(), 52, 0f, 0);
                    }
                    else {
                        num54 = Projectile.NewProjectile(vector8.X, vector8.Y, 6 + Main.rand.Next(-1, 1), Main.rand.Next(-10, 10) / 5, ModContent.ProjectileType<CrazedOrb>(), 52, 0f, 0);
                    }
                    genericTimer2 = 0;
                }
            }

            if (attackPhase == 1) // PHASE 1
            {
                if (!phaseStarted) {
                    subPhase = Main.rand.Next(2);
                    for (int lol = 0; lol < Main.projectile.Length; lol++) {
                        if (Main.projectile[lol].active && Main.projectile[lol].type == ModContent.ProjectileType<EnergyPulse>()) {
                            subPhase = 0;
                            break;
                        }
                    }
                    lookMode = 0;
                    phaseTime = 80;
                    Teleport(Main.player[npc.target].position.X + Main.rand.Next(-50, 50), Main.player[npc.target].position.Y + Main.rand.Next(-50, 50) - 300);
                    phaseStarted = true;
                }
                genericTimer2++;
                if (genericTimer2 >= 40) {
                    int randomrot = Main.rand.Next(-20, 20) / 2;
                    if (subPhase == 0) // SUB PHASE 0
                    {
                        for (int num36 = 0; num36 < 9; num36++) {
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, (float)Math.Sin(randomrot + ((360 / 13) * (1 + num36)) * 3), (float)Math.Cos(randomrot + ((360 / 13) * (1 + num36)) * 3), ModContent.ProjectileType<EnergyPulse>(), 46, 0f, 0);
                        }
                        genericTimer2 = 0;
                    }
                    if (subPhase == 1) // SUB PHASE 1
                    {
                        for (int num36 = 0; num36 < 6; num36++) {
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, (float)Math.Sin(randomrot + ((360 / 10) * (1 + num36))) * 6, (float)Math.Cos(randomrot + ((360 / 10) * (1 + num36))) * 6, ModContent.ProjectileType<EnergyPulse>(), 48, 0f, 0);
                            Main.projectile[num54].ai[0] = npc.target;
                        }
                        genericTimer2 = -200;
                    }
                }
            }

            if (attackPhase == 2) // PHASE 2
            {
                if (!phaseStarted) {
                    lookMode = 2;
                    phaseTime = 60;
                    npc.position.X = Main.player[npc.target].position.X + (float)((600 * Math.Cos((float)(Main.rand.Next(360) * (Math.PI / 180)))) * -1);
                    npc.position.Y = Main.player[npc.target].position.Y + (float)((600 * Math.Sin((float)(Main.rand.Next(360) * (Math.PI / 180)))) * -1);
                    Vector2 vector7 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    float rotation = (float)Math.Atan2(vector7.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector7.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    npc.velocity.X = (float)(Math.Cos(rotation) * 16) * -1;
                    npc.velocity.Y = (float)(Math.Sin(rotation) * 16) * -1;
                    phaseStarted = true;
                }
                genericTimer2++;
                if (genericTimer2 >= 10) {
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    rotation += Main.rand.Next(-50, 50) / 100;
                    int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, (float)((Math.Cos(rotation) * 0.5) * -1), (float)((Math.Sin(rotation) * 0.5) * -1), ModContent.ProjectileType<AntiMatterBlast>(), 55, 0f, 0);
                    genericTimer2 = 0;
                }
            }

            if (attackPhase == 3) // PHASE 3
            {
                if (!phaseStarted) {
                    lookMode = 2;
                    phaseTime = 180;
                    npc.position.X = Main.player[npc.target].position.X + (float)((600 * Math.Cos((float)(Main.rand.Next(360) * (Math.PI / 180)))) * -1);
                    npc.position.Y = Main.player[npc.target].position.Y + (float)((600 * Math.Sin((float)(Main.rand.Next(360) * (Math.PI / 180)))) * -1);
                    phaseStarted = true;
                }
                Vector2 vector7 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                float rotation = (float)Math.Atan2(vector7.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector7.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                npc.velocity.X = (float)(Math.Cos(rotation) * 5) * -1;
                npc.velocity.Y = (float)(Math.Sin(rotation) * 5) * -1;
                genericTimer2++;
                if (genericTimer2 >= 8) {
                    rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    rotation += Main.rand.Next(-50, 50) / 100;
                    int num54 = Projectile.NewProjectile(vector8.X + Main.rand.Next(-100, 100), vector8.Y + Main.rand.Next(-100, 100), (float)((Math.Cos(rotation) * (0.5f + (Main.rand.Next(-3, 3) / 10))) * -1), (float)((Math.Sin(rotation) * (0.5f + (Main.rand.Next(-3, 3) / 10))) * -1), ModContent.ProjectileType<PoisonSmog>(), 34, 0f, 0);
                    genericTimer2 = 0;
                }
            }

            if (genericTimer >= phaseTime) {
                attackPhase = Main.rand.Next(4);
                genericTimer = 0;
                genericTimer2 = 0;
                phaseStarted = false;
            }
        }

        public override void FindFrame(int currentFrame) {
            int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 6, npc.velocity.X, npc.velocity.Y, 100, Color.Red, 1f);
            Main.dust[dust].noGravity = true;

            int num = 1;
            if (!Main.dedServ) {
                num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
            }

            if (npc.velocity.X > 1.5f) npc.frame.Y = num;
            if (npc.velocity.X < -1.5f) npc.frame.Y = num * 2;
            if (npc.velocity.X > -1.5f && npc.velocity.X < 1.5f) npc.frame.Y = 0;
            if (ShieldBroken) {
                if (npc.alpha > 40) npc.alpha -= 1;
                if (npc.alpha < 40) npc.alpha += 1;
            }
            else {
                if (npc.alpha < 200) npc.alpha += 1;
                if (npc.alpha > 200) npc.alpha -= 1;
            }

            //BEGIN LOOK MODE 0, same as 1
            if (lookMode == 0) {
                if ((npc.velocity.X > -2 && npc.velocity.X < 2) && (npc.velocity.Y > -2 && npc.velocity.Y < 2)) {
                    npc.frameCounter = 0;
                    npc.frame.Y = 0;
                    if (npc.position.X > Main.player[npc.target].position.X) {
                        npc.spriteDirection = -1;
                    }
                    else {
                        npc.spriteDirection = 1;
                    }
                }
            }

            //BEGIN LOOK MODE 1
            if (lookMode == 1) {
                if ((npc.velocity.X > -2 && npc.velocity.X < 2) && (npc.velocity.Y > -2 && npc.velocity.Y < 2)) {
                    npc.frameCounter = 0;
                    npc.frame.Y = 0;
                    if (npc.position.X > Main.player[npc.target].position.X) {
                        npc.spriteDirection = -1;
                    }
                    else {
                        npc.spriteDirection = 1;
                    }
                }
            }

            if (lookMode == 2) {
                if ((npc.velocity.X > -2 && npc.velocity.X < 2) && (npc.velocity.Y > -2 && npc.velocity.Y < 2)) {
                    npc.frameCounter = 0;
                    npc.frame.Y = 0;
                    if (npc.position.X > Main.player[npc.target].position.X) {
                        npc.spriteDirection = -1;
                    }
                    else {
                        npc.spriteDirection = 1;
                    }
                }
            }
        }

        public override void HitEffect(int hitDirection, double damage) {
            if (npc.life <= 0) {
                for (int num36 = 0; num36 < 50; num36++) {
                    {
                        Color color = default;
                        int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                    }
                } 
            }
        }
        public override void NPCLoot() {
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.BossItems.MindflayerIllusionRelic>());
        }
    }
}
