using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.GhostWyvernMage
{
    [AutoloadBossHead]
    class WyvernMageShadow : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            Main.npcFrameCount[NPC.type] = 3;
            AnimationType = 29;
            NPC.aiStyle = 0;
            NPC.damage = 90;
            NPC.defense = 56;
            NPC.height = 44;
            NPC.timeLeft = 22500;
            NPC.lifeMax = 200000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.value = 660000;
            NPC.width = 28;
            NPC.knockBackResist = 0.2f;
            NPC.buffImmune[BuffID.Poisoned] = true;
            NPC.buffImmune[BuffID.Confused] = true;
            NPC.buffImmune[BuffID.OnFire] = true;
            despawnHandler = new NPCDespawnHandler("The Wyvern Mage's imprisoned shadow breaks free...", Color.DarkCyan, DustID.Demonite);
        }


        int frozenSawDamage = 35;
        int lightningDamage = 64;
        int Timer2 = -Main.rand.Next(200);

        public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
            NPC.damage = (int)(NPC.damage / 2);
            frozenSawDamage = (int)(frozenSawDamage / 2);
            lightningDamage = (int)(lightningDamage / 2);
        }

        //float customAi1;
        bool OptionSpawned = false;
        int OptionId = 0;


        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (OptionSpawned == false)
            {
                OptionId = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.position.X + (NPC.width / 2), (int)NPC.position.Y + (NPC.height / 2), ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>(), NPC.whoAmI);
                if (Main.netMode == 2 && OptionId < 200)
                {
                    NetMessage.SendData(23, -1, -1, null, OptionId, 0f, 0f, 0f, 0);
                }
                Main.npc[OptionId].velocity.Y = -10;
                OptionSpawned = true;
            }

            NPC.netUpdate = false;
            NPC.ai[0]++; // Timer Scythe

            NPC.ai[1]++; // Timer Teleport


            // npc.ai[2]++; // Shots

            if (NPC.life > 3000)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, Type: DustID.PurpleTorch, NPC.velocity.X, NPC.velocity.Y, 150, Color.Purple, 1f);
                Main.dust[dust].noGravity = true;
            }
            else if (NPC.life <= 3000)
            {
                int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, Type: DustID.PurpleTorch, NPC.velocity.X, NPC.velocity.Y, 100, Color.BlueViolet, 2f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode != 2)
            {
                if (NPC.ai[0] >= 7 && NPC.ai[2] < 3)
                {
                    float num48 = 4f;
                    Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
                    int type = ModContent.ProjectileType<Projectiles.Enemy.FrozenSawII>();
                    float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
                    int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector8.X, vector8.Y, (float)((Math.Cos(rotation) * num48) * -1), (float)((Math.Sin(rotation) * num48) * -1), type, frozenSawDamage, 0f, Main.myPlayer);

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20, NPC.Center);
                    NPC.ai[0] = 0;
                    NPC.ai[2]++;
                }
            }





            if (NPC.ai[1] >= 10)
            {
                NPC.velocity.X *= 0.77f;
                NPC.velocity.Y *= 0.27f;
            }

            if ((NPC.ai[1] >= 200 && NPC.life > 2000) || (NPC.ai[1] >= 120 && NPC.life <= 2000))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                for (int num36 = 0; num36 < 10; num36++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, NPC.velocity.X + Main.rand.Next(-10, 10), NPC.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Red, 4f);
                    Main.dust[dust].noGravity = false;
                }
                NPC.ai[3] = (float)(Main.rand.Next(360) * (Math.PI / 180));
                NPC.ai[2] = 0;
                NPC.ai[1] = 0;


                Player Pt = Main.player[NPC.target];
                Vector2 NC = NPC.position + new Vector2(NPC.width / 2, NPC.height / 2);
                Vector2 PtC = Pt.position + new Vector2(Pt.width / 2, Pt.height / 2);
                NPC.position.X = Pt.position.X + (float)((600 * Math.Cos(NPC.ai[3])) * -1);
                NPC.position.Y = Pt.position.Y - 45 + (float)((30 * Math.Sin(NPC.ai[3])) * -1);

                float MinDIST = 200f;
                float MaxDIST = 600f;
                Vector2 Diff = NPC.position - Pt.position;
                if (Diff.Length() > MaxDIST)
                {
                    Diff *= MaxDIST / Diff.Length();
                }
                if (Diff.Length() < MinDIST)
                {
                    Diff *= MinDIST / Diff.Length();
                }
                NPC.position = Pt.position + Diff;

                NC = NPC.position + new Vector2(NPC.width / 2, NPC.height / 2);

                float rotation = (float)Math.Atan2(NC.Y - PtC.Y, NC.X - PtC.X);
                NPC.velocity.X = (float)(Math.Cos(rotation) * 13) * -1;
                NPC.velocity.Y = (float)(Math.Sin(rotation) * 13) * -1;

            }

            //end of W1k's Death code
            Timer2++;
            if (Timer2 >= 0)
            {


                bool clearSpace = true;
                for (int i = 0; i < 15; i++)
                {
                    if (UsefulFunctions.IsTileReallySolid((int)NPC.Center.X / 16, ((int)NPC.Center.Y / 16) - i))
                    {
                        clearSpace = false;
                    }
                }

                if (clearSpace)
                {
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, 5);


                    speed.Y += Main.rand.NextFloat(-2f, -6f);
                    //speed += Main.rand.NextVector2Circular(-10, -8);
                    if (((speed.X < 0f) && (NPC.velocity.X < 0f)) || ((speed.X > 0f) && (NPC.velocity.X > 0f)))
                    {
                        int lob = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ProjectileID.DD2DrakinShot, 80, 0f, Main.myPlayer);
                        //ModContent.ProjectileType<Projectiles.Enemy.EnemySporeTrap>()
                        //DesertDjinnCurse; ProjectileID.DD2DrakinShot

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = -0.5f }, NPC.Center);
                        if (Timer2 >= 300)
                        {
                            Timer2 = -200 - Main.rand.Next(1250);
                        }
                    }
                }
                else
                {
                    Timer2 = -200 - Main.rand.Next(100);
                }

            }
            //beginning of Omnir's Ultima Weapon projectile code

            NPC.ai[3]++;

            if (NPC.ai[3] >= 100) //how often the crystal attack can happen in frames per second
            {
                if (Main.rand.NextBool(2)) //1 in 2 chance boss will use attack when it flies down on top of you
                {
                    float num48 = 0.9f;
                    Vector2 vector9 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y - 220 + (NPC.height / 2));
                    float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector9.X) + Main.rand.Next(-20, 0x15);
                    float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector9.Y) + Main.rand.Next(-20, 0x15);
                    if (((speedX < 0f) && (NPC.velocity.X < 0f)) || ((speedX > 0f) && (NPC.velocity.X > 0f)))
                    {
                        float num51 = (float)Math.Sqrt((double)((speedX * speedX) + (speedY * speedY)));
                        num51 = num48 / num51;
                        speedX *= num51;
                        speedY *= num51;
                        int type = ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning4Ball>();//44;//0x37; //14;
                        int num54 = Projectile.NewProjectile(NPC.GetSource_FromThis(), vector9.X, vector9.Y, speedX, speedY, type, lightningDamage, 0f, Main.myPlayer);
                        Main.projectile[num54].timeLeft = 250;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item25, NPC.Center);
                        NPC.ai[3] = 0;
                    }
                }

                if (Main.rand.NextBool(20)) //1 in 20 chance boss will summon an NPC
                {
                    int Random = Main.rand.Next(80);
                    int Paraspawn = 0;
                    if (Random == 0) Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X - 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.MageShadow>(), 0);
                    Main.npc[Paraspawn].velocity.X = NPC.velocity.X;
                    if (Random == 0) Paraspawn = NPC.NewNPC(NPC.GetSource_FromAI(), (int)Main.player[this.NPC.target].position.X + 636 - this.NPC.width / 2, (int)Main.player[this.NPC.target].position.Y - 16 - this.NPC.width / 2, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.MageShadow>(), 0);
                    Main.npc[Paraspawn].velocity.X = NPC.velocity.X;
                    NPC.active = true;
                }
            }

            NPC.ai[3] += 1; // my attempt at adding the timer that switches back to the shadow orb
            if (NPC.ai[3] >= 800)
            {
                if (NPC.ai[1] == 0) NPC.ai[1] = 1;
                else NPC.ai[1] = 0;
            }
        }

        #endregion

        public override void FindFrame(int currentFrame)
        {

            if ((NPC.velocity.X > -9 && NPC.velocity.X < 9) && (NPC.velocity.Y > -9 && NPC.velocity.Y < 9))
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
            if ((NPC.velocity.X > -2 && NPC.velocity.X < 2) && (NPC.velocity.Y > -2 && NPC.velocity.Y < 2))
            {
                NPC.frameCounter = 0;
                NPC.frame.Y = 0;
            }
            else
            {
                NPC.frameCounter += 1.0;
            }
            if (NPC.frameCounter >= 1.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(new Terraria.GameContent.ItemDropRules.ItemDropWithConditionRule(ModContent.ItemType<Items.BossBags.MageShadowBag>(), 1, 1, 1, new GhostWyvernMageDropCondition()));
        }

        #region Gore
        public override void OnKill()
        {
            Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Undead Caster Gore 3").Type, 1f);
            }
            if (!Main.expertMode)
            {
                //Only drop the loot if the dragon is already dead. If it's not, then the dragon will drop it instead.
                if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.GhostDragonHead>()))
                {
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>(), 4);
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.GhostWyvernSoul>(), 8);
                    Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Accessories.RingOfPower>());

                }
                else
                {
                    UsefulFunctions.BroadcastText("The souls of " + NPC.GivenOrTypeName + " have been released!", 175, 255, 75);
                    tsorcRevampWorld.Slain[ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()] = 1;
                }
            }
        }
        #endregion

        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            GhostDragonHead.GhostEffect(NPC, spriteBatch, ref texture, 0.9f);
            return true;
        }
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            //GhostDragonHead.GhostEffect(npc, spriteBatch, ref texture, 0.5f);
        }

    }

    public class GhostWyvernMageDropCondition : Terraria.GameContent.ItemDropRules.IItemDropRuleCondition
    {
        public bool CanDrop(Terraria.GameContent.ItemDropRules.DropAttemptInfo info)
        {
            return !NPC.AnyNPCs(ModContent.NPCType<GhostDragonHead>());
        }

        public bool CanShowItemDropInUI()
        {
            return false;
        }

        public string GetConditionDescription()
        {
            return "Drops if his wyvern is dead";
        }
    }
}