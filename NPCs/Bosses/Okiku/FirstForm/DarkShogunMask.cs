using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.Okiku.FirstForm
{
    [AutoloadBossHead]
    public class DarkShogunMask : ModNPC
    {
        private bool initiate;

        public float RotSpeed;

        public bool RotDir;

        public bool OptionSpawned;

        public bool ShieldBroken;

        public bool Transform;

        public override void SetDefaults()
        {
            NPC.width = 28;
            NPC.height = 44;
            NPC.damage = 0;
            NPC.defDamage = 20;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 25000;
            NPC.boss = true;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.value = 50000;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            despawnHandler = new NPCDespawnHandler("You've been slain at the hand of Attraidies...", Color.DarkMagenta, 54);
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            DisplayName.SetDefault("Mindflayer King");
        }
        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
        }
        public override void FindFrame(int frameHeight)
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

        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (!initiate)
            {
                RotSpeed = 0.015f;
                NPC.alpha = 255;
                initiate = true;
            }
            if (!Transform)
            {

                if (Main.player[NPC.target].position.X + (Main.player[NPC.target].width / 2) < NPC.position.X + (NPC.width / 2) - 500 || Main.player[NPC.target].position.X + (Main.player[NPC.target].width / 2) > NPC.position.X + (NPC.width / 2) + 500 || Main.player[NPC.target].position.Y + (Main.player[NPC.target].height / 2) < NPC.position.Y + (NPC.height / 2) - 500 || Main.player[NPC.target].position.Y + (Main.player[NPC.target].height / 2) > NPC.position.Y + (NPC.height / 2) + 500)
                {
                    float rotation = (float)Math.Atan2((NPC.position.Y + (NPC.height / 2)) - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height / 2)), (NPC.position.X + (NPC.width / 2)) - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width / 2)));
                    Main.player[NPC.target].position.X += (float)(Math.Cos(rotation) * 5);
                    Main.player[NPC.target].position.Y += (float)(Math.Sin(rotation) * 5);
                }

                if (OptionSpawned == false)
                {
                    int RealLifeId = 0;
                    for (int j = 0; j < 6; j++)
                    {
                        int rotball = NPC.NewNPC(NPC.GetSource_FromAI(), (int)((NPC.position.X + (NPC.width / 2) - (Main.npc[j].width)) + Math.Sin(NPC.rotation + ((360 / 10) * (1 + j))) * 300), (int)((NPC.position.Y + (NPC.height / 2) - (Main.npc[j].height)) + Math.Cos(NPC.rotation + ((360 / 10) * (1 + j))) * 300), ModContent.NPCType<DamnedSoul>(), 0);
                        Main.npc[rotball].ai[0] = j;
                        Main.npc[rotball].ai[1] = NPC.whoAmI;
                        for (int i = 0; i < 20; i++)
                        {
                            int dustDeath = Dust.NewDust(new Vector2(Main.npc[rotball].position.X, Main.npc[rotball].position.Y), Main.npc[rotball].width, Main.npc[rotball].height, 54, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 200, Color.White, 4f);
                            Main.dust[dustDeath].noGravity = true;
                        }
                        if (j == 0)
                        {
                            RealLifeId = rotball;
                        }
                        else
                        {
                            Main.npc[rotball].realLife = RealLifeId;
                        }
                    }
                    OptionSpawned = true;
                }

                NPC.netUpdate = true;
                NPC.ai[2] += 4;

                if (ShieldBroken)
                {
                    if (RotSpeed < 0.03f) RotSpeed += 0.0003f;
                    NPC.dontTakeDamage = false;
                }
                else
                {
                    if (RotDir == true)
                    {
                        RotSpeed += 0.00005f;
                    }
                    if (RotDir == false)
                    {
                        RotSpeed -= 0.00005f;
                    }
                    if (RotSpeed > 0.02f) RotDir = false;
                    if (RotSpeed < 0.01f) RotDir = true;
                    NPC.dontTakeDamage = true;
                }
                NPC.ai[3] += RotSpeed;

                for (int i = 0; i < 200; i++)
                {
                    if (Main.npc[i].ai[1] == NPC.whoAmI && Main.npc[i].type == ModContent.NPCType<DamnedSoul>())
                    {
                        if (Main.npc[i].life <= 1000)
                        {
                            ShieldBroken = true;
                            Main.npc[i].dontTakeDamage = true;
                        }
                        else
                        {
                            ShieldBroken = false;
                            Main.npc[i].dontTakeDamage = false;
                        }
                        Main.npc[i].scale = (RotSpeed * 200) / 2;
                        Main.npc[i].position.X = (float)((NPC.position.X + (NPC.width / 2) - (Main.npc[i].width / 2)) + Math.Sin(NPC.ai[3] + ((2 * Math.PI) / 6) * (Main.npc[i].ai[0] + 1)/*((360/10)*(1+Main.npc[num36].ai[0]))*/) * 120 * (RotSpeed * 200));
                        Main.npc[i].position.Y = (float)((NPC.position.Y + (NPC.height / 2) - (Main.npc[i].height / 2)) + Math.Cos(NPC.ai[3] + ((2 * Math.PI) / 6) * (Main.npc[i].ai[0] + 1)/*((360/10)*(1+Main.npc[num36].ai[0]))*/) * 120 * (RotSpeed * 200));
                        if (Main.npc[i].ai[0] == 5) break;
                    }
                }

                if (NPC.ai[2] < 600)
                {
                    if (Main.player[NPC.target].position.X + (Main.player[NPC.target].width / 2) < NPC.position.X + (NPC.width / 2))
                    {
                        if (NPC.velocity.X > -2) { NPC.velocity.X -= 0.05f; }
                    }
                    if (Main.player[NPC.target].position.X + (Main.player[NPC.target].width / 2) > NPC.position.X + (NPC.width / 2))
                    {
                        if (NPC.velocity.X < 2) { NPC.velocity.X += 0.05f; }
                    }

                    if (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height / 2) < NPC.position.Y + (NPC.height / 2))
                    {
                        if (NPC.velocity.Y > 0f) NPC.velocity.Y -= 0.2f;
                        else NPC.velocity.Y -= 0.01f;
                    }
                    if (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height / 2) > NPC.position.Y + (NPC.height / 2))
                    {
                        if (NPC.velocity.Y < 0f) NPC.velocity.Y += 0.2f;
                        else NPC.velocity.Y += 0.01f;
                    }
                    NPC.ai[2] = 0;
                }


                if (NPC.life <= 1000) //debug
                {
                    Transform = true;
                    NPC.ai[3] = 1;
                    NPC.ai[2] = 0;
                }

            }
            else
            {
                NPC.ai[2]++;
                NPC.velocity.X = 0;
                NPC.velocity.Y = 0;
                if (RotSpeed > 0.002f) RotSpeed -= 0.0001f;
                NPC.dontTakeDamage = true;
                NPC.ai[3] *= 1.01f;
                for (int i = 0; i < 200; i++)
                {
                    if (Main.npc[i].ai[1] == NPC.whoAmI && Main.npc[i].type == ModContent.NPCType<DamnedSoul>())
                    {
                        Main.npc[i].damage = 0;
                        Main.npc[i].dontTakeDamage = true;
                        Main.npc[i].scale = 3;
                        Main.npc[i].position.X = (float)((NPC.position.X + (NPC.width / 2) - (Main.npc[i].width / 2)) + Math.Sin(NPC.ai[3] + ((2 * Math.PI) / 6) * (Main.npc[i].ai[0] + 1)/*((360/10)*(1+Main.npc[num36].ai[0]))*/) * 120 * (RotSpeed * 200));
                        Main.npc[i].position.Y = (float)((NPC.position.Y + (NPC.height / 2) - (Main.npc[i].height / 2)) + Math.Cos(NPC.ai[3] + ((2 * Math.PI) / 6) * (Main.npc[i].ai[0] + 1)/*((360/10)*(1+Main.npc[num36].ai[0]))*/) * 120 * (RotSpeed * 200));
                        if (Main.npc[i].ai[0] == 5) break;
                    }
                }

                if (NPC.ai[2] > 250 && NPC.ai[2] < 500)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                }
                if (NPC.ai[2] > 500 && NPC.ai[2] < 700)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                }
                if (NPC.ai[2] > 700)
                {
                    Color color = new Color();
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                    dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                    Main.dust[dust].noGravity = true;
                }

                if (NPC.ai[2] > 900)
                {
                    for (int i = 0; i < 50; i++)
                    {
                        Color color = new Color();
                        int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                        dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 62, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, color, 4f);
                        Main.dust[dust].noGravity = true;
                    }
                    for (int i = 0; i < 200; i++)
                    {
                        if (Main.npc[i].type == ModContent.NPCType<DamnedSoul>())
                        {
                            Main.npc[i].active = false;
                        }
                    }
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<SecondForm.DarkDragonMask>(), 0);
                    NPC.active = false;
                }
            }
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
        public override bool CheckActive()
        {
            return false;
        }
    }
}
