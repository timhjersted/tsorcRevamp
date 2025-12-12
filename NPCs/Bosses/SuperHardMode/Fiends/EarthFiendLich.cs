using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
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
using tsorcRevamp.Projectiles.Enemy.Triad;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends
{
    [AutoloadBossHead]
    class EarthFiendLich : ModNPC
    {
        int skullDamage = 50;
        int ichorDamage = 60;

        int skullCircleTimer = 0;
        int teleportTimer = 0;
        int telegraphTimer = 0;
        Vector2 teleportTargetPos;

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
            NPC.damage = 120;
            NPC.defense = 82;
            NPC.aiStyle = 22;
            NPC.scale = 1.2f;
            AnimationType = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 350000;
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

        //chaos
        int holdTimer = 0;
        int holdTimer2 = 0;

        //We can override this even further on a per-NPC basis here
        #region AI
        NPCDespawnHandler despawnHandler;
        public override void AI()
        {

            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            Lighting.AddLight((int)NPC.position.X / 16, (int)NPC.position.Y / 16, 0.4f, 0f, 0.25f);

            Player player = Main.player[NPC.target];
            //chaos code: announce proximity debuffs once
            if (holdTimer > 1)
            {
                holdTimer--;
            }
            if (holdTimer2 > 1)
            {
                holdTimer2--;
            }
            //Proximity Debuffs
            if (Vector2.Distance(NPC.Center, Main.player[NPC.target].Center) < 1200)
            {
                player.AddBuff(BuffID.Ichor, 300, false); //Ichor
                player.AddBuff(ModContent.BuffType<TornWings>(), 30, false);
            }

            #region Ichor/Skull Normal Attacks

            bool flag25 = false;
            ProjectileTimer += (Main.rand.Next(2, 5) * 0.1f) * NPC.scale;
            if (ProjectileTimer >= 10f)
            {
                if (NPC.life > NPC.lifeMax * 0.60f)
                {
                    if (Main.rand.NextBool(205))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 4);
                            projVector += Main.rand.NextVector2Circular(13, 13);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ProjectileID.Skull, skullDamage, 0f, Main.myPlayer);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        ProjectileTimer = 1f;
                    }
                    if (Main.rand.NextBool(33))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 10);
                            projVector += Main.rand.NextVector2Circular(4, 4);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), ichorDamage, 0f, Main.myPlayer);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        ProjectileTimer = 1f;
                    }
                }
                else
                {
                    if (Main.rand.NextBool(180))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 5);
                            projVector += Main.rand.NextVector2Circular(14, 14);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ProjectileID.Skull, skullDamage, 0f, Main.myPlayer);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        ProjectileTimer = 1f;
                    }
                    if (Main.rand.NextBool(29))
                    {
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Vector2 projVector = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 11);
                            projVector += Main.rand.NextVector2Circular(5, 5);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, projVector.X, projVector.Y, ModContent.ProjectileType<Projectiles.Enemy.Triad.IchorFragment>(), ichorDamage, 0f, Main.myPlayer);
                        }
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
                        ProjectileTimer = 1f;
                    }
                }
            }
            #endregion

            #region Skull Circle Attack 
            skullCircleTimer++;
            if (skullCircleTimer > 360)
            {
                if (skullCircleTimer < 420)
                {
                    float rotationSpeed = 0.044f;
                    float circleRotation = skullCircleTimer * rotationSpeed;

                    if (NPC.life < NPC.lifeMax * 0.60f && skullCircleTimer == 390 && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 12; i++)
                        {
                            float angle = (float)i / 12f * MathHelper.TwoPi;
                            Vector2 shadowVel = new Vector2(3f, 0).RotatedBy(angle);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, shadowVel,
                                ProjectileID.Shadowflames, skullDamage, 1f, Main.myPlayer);
                        }
                    }

                    for (int i = 0; i < 16; i++)
                    {
                        float angle = (float)i / 16f * MathHelper.TwoPi + circleRotation;
                        Vector2 telePos = NPC.Center + new Vector2(100, 0).RotatedBy(angle);
                        for (int j = 0; j < 3; j++)
                        {
                            Vector2 dustOffset = new Vector2(8, 0).RotatedBy(angle + Main.rand.NextFloat(-0.3f, 0.3f));
                            Vector2 dustPos = telePos + dustOffset;
                            int dust = Dust.NewDust(dustPos, 8, 8, 181, 0, 0, 150, default, 1f + Main.rand.NextFloat(-0.3f, 0.3f));
                            Main.dust[dust].noGravity = true;
                            Main.dust[dust].velocity *= 0.1f;
                        }
                    }
                }

                if (skullCircleTimer >= 420 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    for (int i = 0; i < 16; i++)
                    {
                        float angle = (float)i / 16f * MathHelper.TwoPi;
                        Vector2 skullVel = new Vector2(8f, 0).RotatedBy(angle);
                        int proj = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, skullVel,
                            ProjectileID.Skull, skullDamage, 1f, Main.myPlayer);
                        Main.projectile[proj].timeLeft = 300;
                    }
                }

                if (skullCircleTimer > 420)
                {
                    skullCircleTimer = 0;
                    NPC.netUpdate = true;
                }
            }
            #endregion

            #region Ichor Teleportation
            teleportTimer++;
            int teleportThreshold = NPC.life < NPC.lifeMax * 0.30f ? 600 : 720;
            if (teleportTimer >= teleportThreshold)
            {
                teleportTimer = 0;

                Player target = Main.player[NPC.target];
                float minDistX = 400f;
                float maxDistX = 600f;
                float maxDistY = 200f;

                float absX = Main.rand.NextFloat(minDistX, maxDistX);
                float signX = Main.rand.NextBool() ? 1f : -1f;
                float offsetX = absX * signX;

                float offsetY = Main.rand.NextFloat(-maxDistY, maxDistY);

                Vector2 offset = new Vector2(offsetX, offsetY);
                teleportTargetPos = target.Center + offset;

                telegraphTimer = 90;
                NPC.netUpdate = true;
            }

            if (telegraphTimer > 0)
            {
                telegraphTimer--;

                for (int i = 0; i < 14; i++)
                {
                    Vector2 dustOffset = Main.rand.NextVector2Circular(55, 75);
                    Vector2 dustPos = teleportTargetPos + dustOffset;
                    int dust = Dust.NewDust(dustPos, 0, 0, 169, 0f, 0f, 150, default, 2f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.5f;
                }

                if (telegraphTimer <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.Center = teleportTargetPos;
                    NPC.velocity = Vector2.Zero;

                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 350, 20);

                    if (NPC.life > NPC.lifeMax * 0.60f)
                    {
                        int glob = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<IchorGlob>());
                        if (glob < Main.maxNPCs)
                            Main.npc[glob].velocity.Y = -5f;
                    }
                    else
                    {
                        int missile = NPC.NewNPC(NPC.GetSource_FromThis(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<IchorMissile>());
                        if (missile < Main.maxNPCs)
                            Main.npc[missile].velocity.Y = -5f;
                    }

                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
                    NPC.netUpdate = true;
                }
            }

            if (NPC.life < NPC.lifeMax * 0.30f && telegraphTimer > 0 && telegraphTimer % 10 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float angleSpread = 13f; 
                    float randomAngle = Main.rand.NextFloat(-angleSpread, angleSpread) * MathHelper.ToRadians(1f);
                    Vector2 ichorVel = new Vector2(0, -9f).RotatedBy(randomAngle); 
                    
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, 
                        ichorVel.X, ichorVel.Y, 
                        ProjectileID.GoldenShowerHostile, ichorDamage, 1f, Main.myPlayer);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
            }
            #endregion

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

            if (NPC.life < NPC.lifeMax * 0.60f && holdTimer <= 0)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath10, NPC.Center);
                NPC.netUpdate = true;
                holdTimer = 30000;

                for (int i = 0; i < 30; i++) 
                {
                    Vector2 dustSpeed = Main.rand.NextVector2Circular(30f, 30f);
                    int dustIndex = Dust.NewDust(NPC.Center, 0, 0, DustID.IchorTorch, dustSpeed.X, dustSpeed.Y, 0, default(Color), 2.5f);
                    Main.dust[dustIndex].noGravity = true; 
                }
            }

            if (NPC.life < NPC.lifeMax * 0.30f && holdTimer2 <= 0)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath10, NPC.Center);
                NPC.netUpdate = true;
                holdTimer2 = 30000;

                for (int i = 0; i < 30; i++) 
                {
                    Vector2 dustSpeed = Main.rand.NextVector2Circular(30f, 30f);
                    int dustIndex = Dust.NewDust(NPC.Center, 0, 0, DustID.IchorTorch, dustSpeed.X, dustSpeed.Y, 0, default(Color), 2.5f);
                    Main.dust[dustIndex].noGravity = true; 
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
            SoundEngine.PlaySound(SoundID.Shatter with { Volume = 1.1f });

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Earth Fiend Lich Gore 1").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Earth Fiend Lich Gore 2").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Earth Fiend Lich Gore 2").Type, 1.2f);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 1, UsefulFunctions.ColorToFloat(Color.OrangeRed));
            }
        }
    }
}