using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Okiku.ThirdForm
{
    [AutoloadBossHead]
    public class BrokenOkiku : ModNPC
    {

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

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            DisplayName.SetDefault("Mindflayer Illusion");
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.width = 58;
            NPC.height = 121;
            NPC.damage = 70;
            NPC.defense = 30;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 15400;
            NPC.timeLeft = 22500;
            NPC.friendly = false;
            NPC.boss = true;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0;
            NPC.lavaImmune = true;
            NPC.value = 350000;
            despawnHandler = new NPCDespawnHandler(DustID.PurpleCrystalShard);
        }
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
        }
        public void Teleport(float X, float Y)
        {
            int dustDeath;
            for (int num36 = 0; num36 < 20; num36++)
            {
                dustDeath = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 200, Color.White, 6f);
                Main.dust[dustDeath].noGravity = true;
            }
            NPC.position.X = X;
            NPC.position.Y = Y;
            NPC.velocity.X = 0;
            NPC.velocity.Y = 0;
            for (int num36 = 0; num36 < 20; num36++)
            {
                dustDeath = Dust.NewDust(new Vector2(X, Y), NPC.width, NPC.height, 54, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.White, 6f);
                Main.dust[dustDeath].noGravity = true;
            }
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (!Initialized)
            {
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


            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width / 2), NPC.position.Y + (NPC.height / 2));
            genericTimer++;
            if (attackPhase == -1)
            {
                lookMode = 0;
                phaseTime = 120;
            }

            if (attackPhase == 0) // PHASE 0
            {
                if (!phaseStarted)
                {
                    lookMode = 1;
                    phaseTime = 60;
                    if (Main.rand.Next(2) == 0) Teleport(Main.player[NPC.target].position.X - 500, Main.player[NPC.target].position.Y + 400);
                    else Teleport(Main.player[NPC.target].position.X + 500, Main.player[NPC.target].position.Y + 400);
                    phaseStarted = true;
                }
                bool left = false;
                if (NPC.position.X < Main.player[NPC.target].position.X) left = false;
                if (NPC.position.X > Main.player[NPC.target].position.X) left = true;
                genericTimer2++;
                NPC.velocity.Y = -15;
                if (genericTimer2 == 10)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                        if (left)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, -6 + Main.rand.Next(-1, 1), Main.rand.Next(-10, 10) / 5, ModContent.ProjectileType<CrazedOrb>(), 62, 0f, Main.myPlayer);
                        }
                        else
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, 6 + Main.rand.Next(-1, 1), Main.rand.Next(-10, 10) / 5, ModContent.ProjectileType<CrazedOrb>(), 62, 0f, Main.myPlayer);
                        }
                    genericTimer2 = 0;
                }
            }

            if (attackPhase == 1) // PHASE 1
            {
                if (!phaseStarted)
                {
                    subPhase = Main.rand.Next(2);
                    for (int lol = 0; lol < Main.projectile.Length; lol++)
                    {
                        if (Main.projectile[lol].active && Main.projectile[lol].type == ModContent.ProjectileType<EnergyPulse>())
                        {
                            subPhase = 0;
                            break;
                        }
                    }
                    lookMode = 0;
                    phaseTime = 80;
                    Teleport(Main.player[NPC.target].position.X + Main.rand.Next(-50, 50), Main.player[NPC.target].position.Y + Main.rand.Next(-50, 50) - 300);
                    phaseStarted = true;
                }
                genericTimer2++;
                if (genericTimer2 >= 40)
                {
                    int randomrot = Main.rand.Next(-20, 20) / 2;
                    if (subPhase == 0) // SUB PHASE 0
                    {
                        for (int num36 = 0; num36 < 9; num36++)
                        {
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)Math.Sin(randomrot + ((360 / 13) * (1 + num36)) * 3), (float)Math.Cos(randomrot + ((360 / 13) * (1 + num36)) * 3), ModContent.ProjectileType<EnergyPulse>(), 66, 0f, Main.myPlayer);
                        }
                        genericTimer2 = 0;
                    }
                    if (subPhase == 1) // SUB PHASE 1
                    {
                        for (int num36 = 0; num36 < 6; num36++)
                        {
                            int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)Math.Sin(randomrot + ((360 / 10) * (1 + num36))) * 6, (float)Math.Cos(randomrot + ((360 / 10) * (1 + num36))) * 6, ModContent.ProjectileType<EnergyPulse>(), 58, 0f, Main.myPlayer);
                            Main.projectile[num54].ai[0] = NPC.target;
                        }
                        genericTimer2 = -200;
                    }
                }
            }

            if (attackPhase == 2) // PHASE 2
            {
                if (!phaseStarted)
                {
                    lookMode = 2;
                    phaseTime = 60;
                    NPC.position.X = Main.player[NPC.target].position.X + (float)((600 * Math.Cos((float)(Main.rand.Next(360) * (Math.PI / 180)))) * -1);
                    NPC.position.Y = Main.player[NPC.target].position.Y + (float)((600 * Math.Sin((float)(Main.rand.Next(360) * (Math.PI / 180)))) * -1);
                    Vector2 vector7 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector7.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector7.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 16) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 16) * -1;
                    phaseStarted = true;
                }
                genericTimer2++;
                if (genericTimer2 >= 10)
                {
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    rotation += Main.rand.Next(-50, 50) / 100;
                    int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * 0.5) * -1), (float)((Math.Sin(rotation) * 0.5) * -1), ModContent.ProjectileType<PhasedMatterBlast>(), 65, 0f, Main.myPlayer);
                    genericTimer2 = 0;
                }
            }

            if (attackPhase == 3) // PHASE 3
            {
                if (!phaseStarted)
                {
                    lookMode = 2;
                    phaseTime = 180;
                    NPC.position.X = Main.player[NPC.target].position.X + (float)((600 * Math.Cos((float)(Main.rand.Next(360) * (Math.PI / 180)))) * -1);
                    NPC.position.Y = Main.player[NPC.target].position.Y + (float)((600 * Math.Sin((float)(Main.rand.Next(360) * (Math.PI / 180)))) * -1);
                    phaseStarted = true;
                }
                Vector2 vector7 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float rotation = (float)Math.Atan2(vector7.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector7.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                NPC.velocity.X = (float)(Math.Cos(rotation) * 5) * -1;
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 5) * -1;
                genericTimer2++;
                if (genericTimer2 >= 8)
                {
                    rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    rotation += Main.rand.Next(-50, 50) / 100;
                    int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X + Main.rand.Next(-100, 100), vector8.Y + Main.rand.Next(-100, 100), (float)((Math.Cos(rotation) * (0.5f + (Main.rand.Next(-3, 3) / 10))) * -1), (float)((Math.Sin(rotation) * (0.5f + (Main.rand.Next(-3, 3) / 10))) * -1), ModContent.ProjectileType<PoisonSmog>(), 34, 0f, Main.myPlayer);
                    genericTimer2 = 0;
                }
            }

            if (genericTimer >= phaseTime)
            {
                attackPhase = Main.rand.Next(4);
                genericTimer = 0;
                genericTimer2 = 0;
                phaseStarted = false;
            }
        }

        public override void FindFrame(int currentFrame)
        {
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X, NPC.velocity.Y, 100, Color.Red, 1f);
            Main.dust[dust].noGravity = true;

            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }

            if (NPC.velocity.X > 1.5f) NPC.frame.Y = num;
            if (NPC.velocity.X < -1.5f) NPC.frame.Y = num * 2;
            if (NPC.velocity.X > -1.5f && NPC.velocity.X < 1.5f) NPC.frame.Y = 0;
            if (ShieldBroken)
            {
                if (NPC.alpha > 40) NPC.alpha -= 1;
                if (NPC.alpha < 40) NPC.alpha += 1;
            }
            else
            {
                if (NPC.alpha < 200) NPC.alpha += 1;
                if (NPC.alpha > 200) NPC.alpha -= 1;
            }

            //BEGIN LOOK MODE 0, same as 1
            if (lookMode == 0)
            {
                if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
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
            }

            //BEGIN LOOK MODE 1
            if (lookMode == 1)
            {
                if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
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
            }

            if (lookMode == 2)
            {
                if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
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
            }
        }

        public override void HitEffect(int hitDirection, double damage)
        {
            if (NPC.life <= 0)
            {
                for (int num36 = 0; num36 < 50; num36++)
                {
                    {
                        Color color = default;
                        int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                    }
                }
            }
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
        public override void OnKill()
        {
            Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.BossItems.MindflayerIllusionRelic>());
        }
    }
}
