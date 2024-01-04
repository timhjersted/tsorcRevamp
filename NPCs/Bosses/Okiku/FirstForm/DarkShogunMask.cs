using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Okiku.FirstForm
{
    [AutoloadBossHead]
    public class DarkShogunMask : ModNPC
    {
        private bool initiate;

        public float RotSpeed;

        public bool RotDir;

        public bool OptionSpawned;

        public bool ShieldBroken = false;

        public bool Transform;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }
        public override void SetDefaults()
        {
            NPC.width = 28;
            NPC.height = 44;
            NPC.damage = 0;
            NPC.defDamage = 20;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = (int)(25000 * (Main.masterMode ? 1.5f : 1));
            NPC.boss = true;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.value = 50000;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.DarkShogunMask.DespawnHandler"), Color.DarkMagenta, 54);
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
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            modifiers.DisableCrit();
            modifiers.SetMaxDamage(NPC.life - 50);
        }
        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            modifiers.DisableCrit();
            modifiers.SetMaxDamage(NPC.life - 50);
        }

        int firstSoul = -1;
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            if (!initiate)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.DarkShogunMask.Quote"), new Color(175, 75, 255));
                RotSpeed = 0.015f;
                NPC.alpha = 255;
                initiate = true;
            }
            if (!Transform)
            {
                if (!ShieldBroken)
                {
                    NPC.dontTakeDamage = true;
                }
                else
                {
                    NPC.dontTakeDamage = false;
                    NPC.ai[1]++;
                    if (NPC.ai[1] > 600)
                    {
                        NPC.ai[1] = 0;
                        ShieldBroken = false;
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            if (Main.npc[i] != null && Main.npc[i].type == ModContent.NPCType<DamnedSoul>() && Main.npc[i].ai[1] == NPC.whoAmI)
                            {
                                Main.npc[i].life = Main.npc[i].lifeMax;
                            }
                        }
                    }
                }

                NPC.ai[3] += 0.01f;


                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i] != null && Main.player[i].active && !Main.player[i].dead)
                    {
                        float distance = Main.player[i].Distance(NPC.Center);
                        if (distance > 400)
                        {
                            float proximity = 500 - distance;
                            proximity /= 500f;
                            proximity = 1 - proximity;
                            for (int j = 0; j < 10f * proximity * proximity; j++)
                            {
                                Vector2 diff = Main.player[i].Center - NPC.Center;
                                diff.SafeNormalize(Vector2.Zero);
                                diff *= 500;

                                diff = diff.RotatedBy(Main.rand.NextFloat(-MathHelper.Pi / 15, MathHelper.Pi / 15));

                                Dust.NewDustPerfect(NPC.Center + diff, 62, NPC.velocity, default, default, 1.5f * proximity).noGravity = true;
                            }
                            if (distance > 500)
                            {
                                Main.player[i].velocity = UsefulFunctions.Aim(Main.player[i].Center, NPC.Center, 5);
                            }
                        }
                    }
                }

                if (OptionSpawned == false)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        firstSoul = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<DamnedSoul>(), 0, 0, NPC.whoAmI);

                        for (int i = 1; i < 6; i++)
                        {
                            int spawnedSoul = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<DamnedSoul>(), 0, i, NPC.whoAmI);
                            Main.npc[spawnedSoul].realLife = firstSoul;
                        }
                    }
                    OptionSpawned = true;
                }

                if (NPC.ai[2] < 600)
                {
                    float speed = 0.5f;
                    if (ShieldBroken)
                    {
                        speed = 2f;
                    }
                    NPC.velocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, speed);
                }

                if (NPC.life <= 1000)
                {
                    NPC.ai[3] = 1;
                    Transform = true;
                }
            }
            else
            {
                NPC.ai[2]++;
                NPC.velocity.X = 0;
                NPC.velocity.Y = 0;
                NPC.dontTakeDamage = true;
                NPC.ai[3] *= 1.01f;
                for (int i = 0; i < 200; i++)
                {
                    if (Main.npc[i].ai[1] == NPC.whoAmI && Main.npc[i].type == ModContent.NPCType<DamnedSoul>())
                    {
                        Main.npc[i].damage = 0;
                        Main.npc[i].dontTakeDamage = true;
                        Main.npc[i].scale = 3;
                        Main.npc[i].position.X = (float)((NPC.position.X + (NPC.width / 2) - (Main.npc[i].width / 2)) + Math.Sin(NPC.ai[3] + ((2 * Math.PI) / 6) * (Main.npc[i].ai[0] + 1)/*((360/10)*(1+Main.npc[i].ai[0]))*/) * 120 * (RotSpeed * 200));
                        Main.npc[i].position.Y = (float)((NPC.position.Y + (NPC.height / 2) - (Main.npc[i].height / 2)) + Math.Cos(NPC.ai[3] + ((2 * Math.PI) / 6) * (Main.npc[i].ai[0] + 1)/*((360/10)*(1+Main.npc[i].ai[0]))*/) * 120 * (RotSpeed * 200));
                        if (Main.npc[i].ai[0] == 5) break;
                    }
                }

                for (int i = 0; i < 4f * NPC.ai[2] / 600f; i++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, default, 4f);
                    Main.dust[dust].noGravity = true;
                }

                if (NPC.ai[2] > 600)
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
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<SecondForm.DarkDragonMask>(), 0);
                    }
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
