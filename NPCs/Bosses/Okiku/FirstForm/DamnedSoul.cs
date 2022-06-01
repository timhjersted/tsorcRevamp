using Microsoft.Xna.Framework;
using System;
using Terraria;
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
            NPC.lifeMax = 20000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0f;
            Main.npcFrameCount[NPC.type] = 4;
            despawnHandler = new NPCDespawnHandler(54);
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Damned Soul");
        }

        public int ObscureShotDamage = 30;
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
        }

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (!initiate)
            {
                NPC.ai[3] = -Main.rand.Next(200);
                initiate = true;
            }
            TimerAnim += 1f;
            if (TimerAnim > 10f)
            {
                if (Main.rand.Next(2) == 0)
                {
                    NPC.spriteDirection *= -1;
                }
                TimerAnim = 0f;
            }
            int dust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 62, 0f, 0f, 100, Color.White);
            Main.dust[dust].noGravity = true;
            for (int i = 0; i < 200; i++)
            {
                if (Main.npc[i].active && Main.npc[i].realLife == NPC.whoAmI)
                {
                    Main.npc[i].life = NPC.life;
                }
            }
            if (Main.npc[(int)NPC.ai[1]].life <= 1000)
            {
                return;
            }
            NPC.ai[3] += 1f;
            if (NPC.ai[3] >= 0f)
            {
                if (NPC.life > 1000)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float speed = 0.5f;
                        Vector2 position = new Vector2(NPC.position.X + (float)(NPC.width / 2), NPC.position.Y + (float)(NPC.height / 2));
                        float rotation2 = (float)Math.Atan2(position.Y - (Main.player[NPC.target].position.Y + (float)Main.player[NPC.target].height * 0.5f), position.X - (Main.player[NPC.target].position.X + (float)Main.player[NPC.target].width * 0.5f));
                        rotation2 += (float)(Main.rand.Next(-50, 50) / 100);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), position.X, position.Y, (float)(Math.Cos(rotation2) * (double)speed * -1.0), (float)(Math.Sin(rotation2) * (double)speed * -1.0), ModContent.ProjectileType<ObscureShot>(), ObscureShotDamage, 0f, Main.myPlayer);
                    }
                    NPC.ai[3] = -200 - Main.rand.Next(200);
                }
                else
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float speed = 0.5f;
                        Vector2 position = new Vector2(NPC.position.X + (float)(NPC.width / 2), NPC.position.Y + (float)(NPC.height / 2));
                        float rotation = (float)Math.Atan2(position.Y - (Main.player[NPC.target].position.Y + (float)Main.player[NPC.target].height * 0.5f), position.X - (Main.player[NPC.target].position.X + (float)Main.player[NPC.target].width * 0.5f));
                        rotation += (float)(Main.rand.Next(-50, 50) / 100);
                        int projectile = Projectile.NewProjectile(NPC.GetSource_FromThis(), position.X, position.Y, (float)(Math.Cos(rotation) * (double)speed * -1.0), (float)(Math.Sin(rotation) * (double)speed * -1.0), ModContent.ProjectileType<ObscureShot>(), ObscureShotDamage, 0f, Main.myPlayer);
                        Main.projectile[projectile].scale = 3f;
                    }
                    NPC.ai[3] = -50 - Main.rand.Next(50);
                }
            }
            if (NPC.life > 1000)
            {
                return;
            }
            TimerHeal++;
            if (TimerHeal < 600)
            {
                return;
            }
            NPC.life = NPC.lifeMax;
            TimerHeal = 0;
            for (int i = 0; i < 200; i++)
            {
                if (Main.npc[i].active && Main.npc[i].realLife == NPC.whoAmI)
                {
                    Main.npc[i].life = 2000;
                }
            }
        }

        public override void FindFrame(int frameHeight)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = Main.npcTexture[NPC.type].Height / Main.npcFrameCount[NPC.type];
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
