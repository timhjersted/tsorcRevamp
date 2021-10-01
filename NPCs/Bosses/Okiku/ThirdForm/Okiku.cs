using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Okiku.ThirdForm {
    [AutoloadBossHead]
    public class Okiku : ModNPC {
        int lookMode;
        int attackPhase;
        int subPhase;
        int genericTimer;
        int genericTimer2;
        int phaseTime;
        bool phaseStarted;
        bool Initialized;

        public override string Texture => "tsorcRevamp/NPCs/Bosses/Okiku/FirstForm/DarkShogunMask";
        public override void SetDefaults() {
            npc.width = 58;
            npc.height = 120;
            npc.aiStyle = -1;
            npc.damage = 40;
            npc.defense = 35;
            npc.boss = true;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.lifeMax = 16000;
            npc.npcSlots = 20f;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.value = Item.buyPrice(0, 26);
            npc.knockBackResist = 0f;
            music = 39;
            npc.buffImmune[20] = true;
            npc.buffImmune[44] = true;
            npc.buffImmune[31] = true;
            npc.buffImmune[39] = true;
            npc.buffImmune[69] = true;
            npc.buffImmune[70] = true;
            despawnHandler = new NPCDespawnHandler("You've been slain at the hand of Attraidies...", Color.DarkMagenta, DustID.PurpleCrystalShard);
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection) {
            if (damage > npc.life || damage * 2 > npc.life) {
                crit = false;
                damage = npc.life - 50;
            }
        }
        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit) {
            if (damage > npc.life || damage * 2 > npc.life) {
                crit = false;
                damage = npc.life - 50;
            }
        }
        public override void SetStaticDefaults() {
            Main.npcFrameCount[npc.type] = 3;
            DisplayName.SetDefault("Mindflayer Illusion");
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale) {
            npc.lifeMax = (int)((float)npc.lifeMax * 0.7f * bossLifeScale);
        }

        public void Teleport(float X, float Y) {
            int dustDeath = 0;
            for (int num36 = 0; num36 < 20; num36++) {
                dustDeath = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 54, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 200, Color.White, 4f);
                Main.dust[dustDeath].noGravity = true;
            }
            npc.position.X = X;
            npc.position.Y = Y;
            npc.velocity.X = 0;
            npc.velocity.Y = 0;
            for (int num36 = 0; num36 < 20; num36++) {
                dustDeath = Dust.NewDust(new Vector2(X, Y), npc.width, npc.height, 54, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, Color.White, 4f);
                Main.dust[dustDeath].noGravity = true;
            }
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(npc.whoAmI);
            if (!Initialized) {
                lookMode = 0;
                attackPhase = -1;
                subPhase = 0;
                genericTimer = 0;
                genericTimer2 = 0;
                phaseTime = 90;
                phaseStarted = false;
                Initialized = true;
            }

            for (int num36 = 0; num36 < 10; num36++) {
                if (Main.player[npc.target].buffType[num36] == 18) {
                    Main.player[npc.target].buffType[num36] = 0;
                    Main.player[npc.target].buffTime[num36] = 0;
                    if (Main.netMode != NetmodeID.Server && Main.myPlayer == npc.target) {
                        Main.NewText("You've done well, Red. But it's not over yet.", 150, 150, 150);
                    }
                    break;
                }
            }

            Vector2 vector8 = new Vector2(npc.position.X + (npc.width / 2), npc.position.Y + (npc.height / 2));
            genericTimer++;
            if (attackPhase == -1) {
                lookMode = 1;
                phaseTime = 120;
            }

            if (attackPhase == 0) // PHASE 0
            {
                if (!phaseStarted) {
                    lookMode = 1;
                    phaseTime = 90;
                    if (Main.rand.Next(2) == 0) Teleport(Main.player[npc.target].position.X - 500, Main.player[npc.target].position.Y + 400);
                    else Teleport(Main.player[npc.target].position.X + 500, Main.player[npc.target].position.Y + 400);
                    phaseStarted = true;
                }
                bool left = false;
                if (npc.position.X < Main.player[npc.target].position.X) left = false;
                if (npc.position.X > Main.player[npc.target].position.X) left = true;
                genericTimer2++;
                npc.velocity.Y = -10;
                if (genericTimer2 == 15) {
                    int num54 = 0;
                    if (left) {
                        num54 = Projectile.NewProjectile(vector8, new Vector2(-6 + Main.rand.Next(-1, 1), Main.rand.Next(-10, 10) / 5), ModContent.ProjectileType<CrazedOrb>(), 55, 0f, 0);
                    }
                    else {
                        num54 = Projectile.NewProjectile(vector8, new Vector2(6 + Main.rand.Next(-1, 1), Main.rand.Next(-10, 10) / 5), ModContent.ProjectileType<CrazedOrb>(), 55, 0f, 0);
                    }
                    genericTimer2 = 0;
                }
            }

            if (attackPhase == 1) // PHASE 1
            {
                if (!phaseStarted) {
                    subPhase = Main.rand.Next(2);
                    for (int lol = 0; lol < Main.projectile.Length; lol++) {
                        if (Main.projectile[lol].active && Main.projectile[lol].type == ModContent.ProjectileType<CrazyOrb>()) {
                            subPhase = 0;
                            break;
                        }
                    }
                    lookMode = 0;
                    phaseTime = 90;
                    Teleport(Main.player[npc.target].position.X + Main.rand.Next(-50, 50), Main.player[npc.target].position.Y + Main.rand.Next(-50, 50) - 300);
                    phaseStarted = true;
                }
                genericTimer2++;
                if (genericTimer2 >= 50) {
                    int randomrot = Main.rand.Next(-20, 20) / 2;
                    if (subPhase == 0) // SUB PHASE 0
                    {
                        for (int num36 = 0; num36 < 9; num36++) {
                            int num54 = Projectile.NewProjectile(vector8.X, vector8.Y, (float)Math.Sin(randomrot + ((360 / 13) * (1 + num36)) * 3), (float)Math.Cos(randomrot + ((360 / 13) * (1 + num36)) * 3), ModContent.ProjectileType<ObscureSaw>(), 65, 0f, Main.myPlayer);
                        }
                        genericTimer2 = 0;
                    }
                    if (subPhase == 1) // SUB PHASE 1
                    {
                        for (int num36 = 0; num36 < 6; num36++) {
                            int num54 = Projectile.NewProjectile(vector8, new Vector2((float)Math.Sin(randomrot + ((360 / 10) * (1 + num36))) * 6, (float)Math.Cos(randomrot + ((360 / 10) * (1 + num36))) * 6), ModContent.ProjectileType<CrazyOrb>(), 45, 0f, Main.myPlayer);
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
                    phaseTime = 90;
                    npc.position.X = Main.player[npc.target].position.X + (float)((600 * Math.Cos((float)(Main.rand.Next(360) * (Math.PI / 180)))) * -1);
                    npc.position.Y = Main.player[npc.target].position.Y + (float)((600 * Math.Sin((float)(Main.rand.Next(360) * (Math.PI / 180)))) * -1);
                    Vector2 vector7 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
                    float rotation = (float)Math.Atan2(vector7.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector7.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    npc.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
                    npc.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
                    phaseStarted = true;
                }
                genericTimer2++;
                npc.velocity.X *= 0.99f;
                npc.velocity.Y *= 0.99f;
                if (genericTimer2 >= 20) {
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    rotation += Main.rand.Next(-50, 50) / 100;
                    int num54 = Projectile.NewProjectile(vector8, new Vector2((float)(Math.Cos(rotation) * 0.5 * -1), (float)((Math.Sin(rotation) * 0.5) * -1)), ModContent.ProjectileType<ObscureSaw>(), 65, 0f, 0);
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
                npc.velocity.X = (float)(Math.Cos(rotation) * 4) * -1;
                npc.velocity.Y = (float)(Math.Sin(rotation) * 4) * -1;
                genericTimer2++;
                if (genericTimer2 >= 12) {
                    rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
                    rotation += Main.rand.Next(-50, 50) / 100;
                    int num54 = Projectile.NewProjectile(new Vector2(vector8.X + Main.rand.Next(-100, 100), vector8.Y + Main.rand.Next(-100, 100)), new Vector2((float)((Math.Cos(rotation) * (0.5f + (Main.rand.Next(-3, 3) / 10))) * -1), (float)((Math.Sin(rotation) * (0.5f + (Main.rand.Next(-3, 3) / 10))) * -1)), ModContent.ProjectileType<PoisonSmog>(), 18, 0f, 0);
                    genericTimer2 = 0;
                }
            }

            if (genericTimer >= phaseTime) {
                attackPhase = Main.rand.Next(4);
                genericTimer = 0;
                genericTimer2 = 0;
                phaseStarted = false;
            }

            if (npc.life <= npc.lifeMax / 2) {
                int NewOkiku = NPC.NewNPC((int)vector8.X, (int)vector8.Y, ModContent.NPCType<BrokenOkiku>(), 0);
                Main.npc[NewOkiku].life = npc.life;
                npc.active = false;
            }
        }
        public override void FindFrame(int currentFrame) {
            int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X, npc.velocity.Y, 100, Color.White, 1f);
            Main.dust[dust].noGravity = true;

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

        public override bool CheckActive()
        {
            return false;
        }
        public override void NPCLoot() {
            if (!tsorcRevampWorld.Slain.ContainsKey(npc.type))
            {
                Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 35000);
            }
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.BossItems.MindflayerIllusionRelic>());
        }

    }
}
