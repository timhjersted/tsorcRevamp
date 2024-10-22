using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends
{
    [AutoloadBossHead]
    class EarthFiendLich : ModNPC
    {
        int lightningDamage = 48;
        int oracleDamage = 43;
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.width = 120;
            NPC.height = 160;
            NPC.damage = 128;
            NPC.defense = 82;
            NPC.aiStyle = 22;
            NPC.scale = 1.1f;
            AnimationType = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 275000;
            NPC.timeLeft = 22500;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.value = 600000;
            NPC.rarity = 35;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.EarthFiendLich.DespawnHandler"), Color.DarkGreen, DustID.GreenFairy);

        }
        public float ProjectileTimer
        {
            get => NPC.ai[1];
            set => NPC.ai[1] = value;
        }
        bool OptionSpawned = false;
        int OptionId = 0;

        //chaos
        int holdTimer = 0;

        //We can override this even further on a per-NPC basis here
        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.4f, 0f, 0.25f);

            if (OptionSpawned == false)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    OptionId = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<LichKingDisciple>(), NPC.whoAmI);
                    Main.npc[OptionId].velocity.Y = -10;
                    NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, OptionId, 0f, 0f, 0f, 0);
                }
                OptionSpawned = true;
            }



            Player player = Main.player[NPC.target];
            //chaos code: announce proximity debuffs once
            if (holdTimer > 1)
            {
                holdTimer--;
            }
            //Proximity Debuffs
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) < 1200)
            {
                player.AddBuff(BuffID.OnFire, 30, false); //on fire
                player.AddBuff(ModContent.BuffType<TornWings>(), 30, false);

                if (holdTimer <= 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.EarthFiendLich.Heat"), 255, 255, 0); //yellow
                    holdTimer = 12000;
                }

            }

            bool flag25 = false;
            ProjectileTimer += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
            if (ProjectileTimer >= 10f)
            {
                if (Main.rand.NextBool(90))
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 9);
                        projVector += Main.rand.NextVector2Circular(20, 20);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning3Ball>(), lightningDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    ProjectileTimer = 1f;
                }
                if (Main.rand.NextBool(20))
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 13);
                        projVector += Main.rand.NextVector2Circular(10, 10);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.TheOracle>(), oracleDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                    ProjectileTimer = 1f;
                }
            }


            if (NPC.justHit)
            {
                NPC.ai[2] = 0f;
            }
            if (NPC.ai[2] >= 0f)
            {
                int num258 = 16;
                bool flag26 = false;
                bool flag27 = false;
                if (NPC.position.X > NPC.ai[0] - (float)num258 && NPC.position.X < NPC.ai[0] + (float)num258)
                {
                    flag26 = true;
                }
                else
                {
                    if ((NPC.velocity.X < 0f && NPC.direction > 0) || (NPC.velocity.X > 0f && NPC.direction < 0))
                    {
                        flag26 = true;
                    }
                }
                num258 += 24;
                if (NPC.position.Y > NPC.ai[1] - (float)num258 && NPC.position.Y < NPC.ai[1] + (float)num258)
                {
                    flag27 = true;
                }
                if (flag26 && flag27)
                {
                    NPC.ai[2] += 1f;
                    if (NPC.ai[2] >= 30f && num258 == 16)
                    {
                        flag25 = true;
                    }
                    if (NPC.ai[2] >= 60f)
                    {
                        NPC.ai[2] = -200f;
                        NPC.direction *= -1;
                        NPC.velocity.X = NPC.velocity.X * -1f;
                        NPC.collideX = false;
                    }
                }
                else
                {
                    NPC.ai[0] = NPC.position.X;
                    NPC.ai[1] = NPC.position.Y;
                    NPC.ai[2] = 0f;
                }
            }
            else
            {
                NPC.ai[2] += 1f;
                if (Main.player[NPC.target].position.X + (float)(Main.player[NPC.target].width / 2) > NPC.position.X + (float)(NPC.width / 2))
                {
                    NPC.direction = -1;
                }
                else
                {
                    NPC.direction = 1;
                }
            }
            int num259 = (int)((NPC.position.X + (float)(NPC.width / 2)) / 16f) + NPC.direction * 2;
            int num260 = (int)((NPC.position.Y + (float)NPC.height) / 16f);
            bool flag28 = true;
            //bool flag29; //What is this? It doesn't seem to do anything, so i'm commenting it out.
            int num261 = 3;
            for (int num269 = num260; num269 < num260 + num261; num269++)
            {
                if (Main.tile[num259, num269] == null)
                {
                    Main.tile[num259, num269].ClearTile();
                }
                if ((Main.tile[num259, num269].HasTile && Main.tileSolid[(int)Main.tile[num259, num269].TileType]) || Main.tile[num259, num269].LiquidAmount > 0)
                {
                    //if (num269 <= num260 + 1)
                    //{
                    //	flag29 = true;
                    //	}
                    flag28 = false;
                    break;
                }
            }
            if (flag25)
            {
                //	flag29 = false;
                flag28 = true;
            }
            if (flag28)
            {
                NPC.velocity.Y = NPC.velocity.Y + 0.1f;
                if (NPC.velocity.Y > 3f)
                {
                    NPC.velocity.Y = 3f;
                }
            }
            else
            {
                if (NPC.directionY < 0 && NPC.velocity.Y > 0f)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.1f;
                }
                if (NPC.velocity.Y < -4f)
                {
                    NPC.velocity.Y = -4f;
                }
            }
            if (NPC.collideX)
            {
                NPC.velocity.X = NPC.oldVelocity.X * -0.4f;
                if (NPC.direction == -1 && NPC.velocity.X > 0f && NPC.velocity.X < 1f)
                {
                    NPC.velocity.X = 1f;
                }
                if (NPC.direction == 1 && NPC.velocity.X < 0f && NPC.velocity.X > -1f)
                {
                    NPC.velocity.X = -1f;
                }
            }
            if (NPC.collideY)
            {
                NPC.velocity.Y = NPC.oldVelocity.Y * -0.25f;
                if (NPC.velocity.Y > 0f && NPC.velocity.Y < 1f)
                {
                    NPC.velocity.Y = 1f;
                }
                if (NPC.velocity.Y < 0f && NPC.velocity.Y > -1f)
                {
                    NPC.velocity.Y = -1f;
                }
            }
            float num270 = 2f;
            if (NPC.direction == -1 && NPC.velocity.X > -num270)
            {
                NPC.velocity.X = NPC.velocity.X - 0.1f;
                if (NPC.velocity.X > num270)
                {
                    NPC.velocity.X = NPC.velocity.X - 0.1f;
                }
                else
                {
                    if (NPC.velocity.X > 0f)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.05f;
                    }
                }
                if (NPC.velocity.X < -num270)
                {
                    NPC.velocity.X = -num270;
                }
            }
            else
            {
                if (NPC.direction == 1 && NPC.velocity.X < num270)
                {
                    NPC.velocity.X = NPC.velocity.X + 0.1f;
                    if (NPC.velocity.X < -num270)
                    {
                        NPC.velocity.X = NPC.velocity.X + 0.1f;
                    }
                    else
                    {
                        if (NPC.velocity.X < 0f)
                        {
                            NPC.velocity.X = NPC.velocity.X - 0.05f;
                        }
                    }
                    if (NPC.velocity.X > num270)
                    {
                        NPC.velocity.X = num270;
                    }
                }
            }
            if (NPC.directionY == -1 && (double)NPC.velocity.Y > -1.5)
            {
                NPC.velocity.Y = NPC.velocity.Y - 0.04f;
                if ((double)NPC.velocity.Y > 1.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y - 0.05f;
                }
                else
                {
                    if (NPC.velocity.Y > 0f)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.03f;
                    }
                }
                if ((double)NPC.velocity.Y < -1.5)
                {
                    NPC.velocity.Y = -1.5f;
                }
            }
            else
            {
                if (NPC.directionY == 1 && (double)NPC.velocity.Y < 1.5)
                {
                    NPC.velocity.Y = NPC.velocity.Y + 0.04f;
                    if ((double)NPC.velocity.Y < -1.5)
                    {
                        NPC.velocity.Y = NPC.velocity.Y + 0.05f;
                    }
                    else
                    {
                        if (NPC.velocity.Y < 0f)
                        {
                            NPC.velocity.Y = NPC.velocity.Y - 0.03f;
                        }
                    }
                    if ((double)NPC.velocity.Y > 1.5)
                    {
                        NPC.velocity.Y = 1.5f;
                    }
                }
            }

        }
        #endregion

        #region Frames
        public override void FindFrame(int currentFrame)
        {
            int num = 1;
            if (!Main.dedServ)
            {
                num = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            if (NPC.velocity.X < 0)
            {
                NPC.spriteDirection = -1;
            }
            else
            {
                NPC.spriteDirection = 1;
            }
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.frameCounter += 1.0;
            if (NPC.frameCounter >= 4.0)
            {
                NPC.frame.Y = NPC.frame.Y + num;
                NPC.frameCounter = 0.0;
            }
            if (NPC.frame.Y >= num * Main.npcFrameCount[NPC.type])
            {
                NPC.frame.Y = 0;
            }
            if (NPC.ai[3] == 0)
            {
                NPC.alpha = 0;
            }
            else
            {
                NPC.alpha = 200;
            }
        }
        #endregion
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.LichBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HolyWarElixir>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<LichBone>(), 1, 2, 4));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ForgottenGaiaSword>()));
            npcLoot.Add(notExpertCondition);
        }
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Earth Fiend Lich Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Earth Fiend Lich Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Earth Fiend Lich Gore 2").Type, 1f);
            }
        }
    }
}