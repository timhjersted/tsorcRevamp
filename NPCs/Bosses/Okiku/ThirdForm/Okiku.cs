using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Okiku.ThirdForm
{
    [AutoloadBossHead]
    public class Okiku : ModNPC
    {
        int lookMode;
        int attackPhase;
        int subPhase;
        int genericTimer;
        int genericTimer2;
        int phaseTime;
        bool phaseStarted;
        bool Initialized;

        public override string Texture => "tsorcRevamp/NPCs/Bosses/Okiku/FirstForm/DarkShogunMask";
        public override void SetDefaults()
        {
            NPC.width = 58;
            NPC.height = 120;
            NPC.aiStyle = -1;
            NPC.damage = 70;
            NPC.defense = 36;
            NPC.boss = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lifeMax = 22400;
            NPC.npcSlots = 2f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = Item.buyPrice(0, 26);
            NPC.knockBackResist = 0f;
            Music = 39;
            NPC.buffImmune[20] = true;
            NPC.buffImmune[44] = true;
            NPC.buffImmune[31] = true;
            NPC.buffImmune[39] = true;
            NPC.buffImmune[69] = true;
            NPC.buffImmune[70] = true;
            despawnHandler = new NPCDespawnHandler("You've been slain at the hand of Attraidies...", Color.DarkMagenta, DustID.PurpleCrystalShard);
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (damage > NPC.life || damage * 2 > NPC.life)
            {
                crit = false;
                damage = NPC.life - 50;
            }
        }
        public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
        {
            if (damage > NPC.life || damage * 2 > NPC.life)
            {
                crit = false;
                damage = NPC.life - 50;
            }
        }
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            DisplayName.SetDefault("Mindflayer Illusion");
        }

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
        }

        public void Teleport(float X, float Y)
        {
            int dustDeath = 0;
            for (int i = 0; i < 20; i++)
            {
                dustDeath = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 200, Color.White, 4f);
                Main.dust[dustDeath].noGravity = true;
            }
            NPC.position.X = X;
            NPC.position.Y = Y;
            NPC.velocity.X = 0;
            NPC.velocity.Y = 0;
            for (int i = 0; i < 20; i++)
            {
                dustDeath = Dust.NewDust(new Vector2(X, Y), NPC.width, NPC.height, 54, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.White, 4f);
                Main.dust[dustDeath].noGravity = true;
            }
        }

        public void CircleTeleport()
        {
            NPC.position.X = Main.player[NPC.target].position.X + (float)(600 * Math.Cos(nextWarpAngle) * -1);
            NPC.position.Y = Main.player[NPC.target].position.Y + (float)(600 * Math.Sin(nextWarpAngle) * -1);
            nextWarpAngle = MathHelper.ToRadians(Main.rand.Next(360));
            NPC.netUpdate = true;
        }


        int nextSubPhase = 0;
        float nextWarpAngle = 0;
        bool teleportLeft = false;
        int nextAttackPhase = 0;
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (!Initialized)
            {
                lookMode = 0;
                attackPhase = -1;
                subPhase = 0;
                genericTimer = 0;
                genericTimer2 = 0;
                phaseTime = 90;
                phaseStarted = false;
                Initialized = true;

                UsefulFunctions.BroadcastText("You've done well, Red. But it's not over yet.", new Color(150, 150, 150));
            }

            for (int i = 0; i < 10; i++)
            {
                if (Main.player[NPC.target].buffType[i] == 18)
                {
                    Main.player[NPC.target].buffType[i] = 0;
                    Main.player[NPC.target].buffTime[i] = 0;
                    if (Main.netMode != NetmodeID.Server && Main.myPlayer == NPC.target)
                    {
                        Main.NewText("What a horrible night to have your Gravitation buff dispelled...", 150, 150, 150);
                    }
                    break;
                }
            }

            Vector2 center = new Vector2(NPC.position.X + (NPC.width / 2), NPC.position.Y + (NPC.height / 2));
            genericTimer++;
            if (attackPhase == -1)
            {
                lookMode = 1;
                phaseTime = 120;
            }

            if (attackPhase == 0) // PHASE 0
            {
                if (!phaseStarted)
                {
                    lookMode = 1;
                    phaseTime = 90;
                    if (teleportLeft)
                    {
                        Teleport(Main.player[NPC.target].position.X - 500, Main.player[NPC.target].position.Y + 400);
                    }
                    else
                    {
                        Teleport(Main.player[NPC.target].position.X + 500, Main.player[NPC.target].position.Y + 400);
                    }
                    teleportLeft = !teleportLeft;
                    phaseStarted = true;
                }
                bool left = false;
                if (NPC.position.X < Main.player[NPC.target].position.X) left = false;
                if (NPC.position.X > Main.player[NPC.target].position.X) left = true;
                genericTimer2++;
                NPC.velocity.Y = -10;
                if (genericTimer2 == 15)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (left)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), center, new Vector2(-6 + Main.rand.Next(-1, 1), Main.rand.Next(-10, 10) / 5), ModContent.ProjectileType<CrazedOrb>(), 55, 0f, 0);
                        }
                        else
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), center, new Vector2(6 + Main.rand.Next(-1, 1), Main.rand.Next(-10, 10) / 5), ModContent.ProjectileType<CrazedOrb>(), 55, 0f, 0);
                        }
                    }
                    genericTimer2 = 0;
                }
            }

            if (attackPhase == 1) // PHASE 1
            {
                if (!phaseStarted)
                {
                    subPhase = nextSubPhase;
                    nextSubPhase = Main.rand.Next(2);
                    NPC.netUpdate = true;
                    for (int lol = 0; lol < Main.projectile.Length; lol++)
                    {
                        if (Main.projectile[lol].active && Main.projectile[lol].type == ModContent.ProjectileType<CrazyOrb>())
                        {
                            subPhase = 0;
                            break;
                        }
                    }
                    lookMode = 0;
                    phaseTime = 90;
                    Teleport(Main.player[NPC.target].position.X, Main.player[NPC.target].position.Y - 300);
                    phaseStarted = true;
                }
                genericTimer2++;
                if (genericTimer2 >= 50)
                {
                    if (subPhase == 0) // SUB PHASE 0
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int randomrot = Main.rand.Next(-20, 20) / 2;
                            for (int i = 0; i < 9; i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), center.X, center.Y, (float)Math.Sin(randomrot + ((360 / 13) * (1 + i)) * 3), (float)Math.Cos(randomrot + ((360 / 13) * (1 + i)) * 3), ModContent.ProjectileType<ObscureSaw>(), 65, 0f, Main.myPlayer);
                            }
                        }
                        genericTimer2 = 0;
                    }
                    if (subPhase == 1) // SUB PHASE 1
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int randomrot = Main.rand.Next(-20, 20) / 2;
                            for (int i = 0; i < 6; i++)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), center, new Vector2((float)Math.Sin(randomrot + ((360 / 10) * (1 + i))) * 6, (float)Math.Cos(randomrot + ((360 / 10) * (1 + i))) * 6), ModContent.ProjectileType<CrazyOrb>(), 45, 0f, Main.myPlayer, NPC.target);
                            }
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
                    phaseTime = 90;
                    CircleTeleport();
                    Vector2 vector7 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    float rotation = (float)Math.Atan2(vector7.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector7.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    NPC.velocity.X = (float)(Math.Cos(rotation) * 14) * -1;
                    NPC.velocity.Y = (float)(Math.Sin(rotation) * 14) * -1;
                    phaseStarted = true;
                }
                genericTimer2++;
                NPC.velocity.X *= 0.99f;
                NPC.velocity.Y *= 0.99f;
                if (genericTimer2 >= 20)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float rotation = (float)Math.Atan2(center.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), center.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        rotation += Main.rand.Next(-50, 50) / 100;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), center, new Vector2((float)(Math.Cos(rotation) * 0.5 * -1), (float)((Math.Sin(rotation) * 0.5) * -1)), ModContent.ProjectileType<ObscureSaw>(), 65, 0f, 0);
                    }
                    genericTimer2 = 0;
                }
            }

            if (attackPhase == 3) // PHASE 3
            {
                if (!phaseStarted)
                {
                    lookMode = 2;
                    phaseTime = 180;
                    CircleTeleport();
                    phaseStarted = true;
                }
                Vector2 vector7 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                float rotation = (float)Math.Atan2(vector7.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector7.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                NPC.velocity.X = (float)(Math.Cos(rotation) * 4) * -1;
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 4) * -1;
                genericTimer2++;
                if (genericTimer2 >= 12)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        rotation = (float)Math.Atan2(center.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), center.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                        rotation += Main.rand.Next(-50, 50) / 100;
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(center.X + Main.rand.Next(-100, 100), center.Y + Main.rand.Next(-100, 100)), new Vector2((float)((Math.Cos(rotation) * (0.5f + (Main.rand.Next(-3, 3) / 10))) * -1), (float)((Math.Sin(rotation) * (0.5f + (Main.rand.Next(-3, 3) / 10))) * -1)), ModContent.ProjectileType<PoisonSmog>(), 18, 0f, 0);
                    }
                    genericTimer2 = 0;
                }
            }

            if (genericTimer >= phaseTime)
            {
                attackPhase = nextAttackPhase;
                nextAttackPhase = Main.rand.Next(4);
                NPC.netUpdate = true;
                genericTimer = 0;
                genericTimer2 = 0;
                phaseStarted = false;
            }

            if (NPC.life <= NPC.lifeMax / 2)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int NewOkiku = NPC.NewNPC(NPC.GetSource_FromAI(), (int)center.X, (int)center.Y, ModContent.NPCType<BrokenOkiku>(), 0);
                    Main.npc[NewOkiku].life = NPC.life;
                }
                NPC.active = false;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(nextSubPhase);
            writer.Write(nextAttackPhase);
            writer.Write(nextWarpAngle);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            nextSubPhase = reader.ReadInt32();
            nextAttackPhase = reader.ReadInt32();
            nextWarpAngle = reader.ReadSingle();
        }

        public override void FindFrame(int currentFrame)
        {
            int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X, NPC.velocity.Y, 100, Color.White, 1f);
            Main.dust[dust].noGravity = true;

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

        public override bool CheckActive()
        {
            return false;
        }
        public override void OnKill()
        {
            if (!Main.expertMode)
            {
                if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(NPC.type)))
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 35000);
                }
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.BossItems.MindflayerIllusionRelic>());
            }
        }

    }
}
