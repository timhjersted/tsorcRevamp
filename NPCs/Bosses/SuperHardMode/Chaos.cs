using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Weapons.Magic.Tomes;
using tsorcRevamp.Items.Weapons.Ranged.Bows;
using tsorcRevamp.Utilities;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Projectiles.Enemy.Birbs;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class Chaos : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.ShadowFlame] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 130;
            NPC.height = 170;
            NPC.aiStyle = -1; // ChaosMovement
            NPC.damage = 100;
            NPC.defense = 80;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.lifeMax = 400000;
            NPC.knockBackResist = 0f;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.value = 670000;
            NPC.scale = 1.1f;
            NPC.boss = true;
            NPC.lavaImmune = true;

            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Chaos.DespawnHandler"), Color.Yellow, DustID.GoldFlame);

            InitializeMoves();
        }

        int purpleCrushDamage = 45;
        int sickleDamage = 55;
        int BlackFireDamage = 50; 
        bool chargeDamageFlag = false;
        private bool Phase2 => NPC.life <= (int)(NPC.lifeMax * 0.6f);

        List<ChaosMove> MoveList;
        public int MoveIndex
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }
        public int MoveTimer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        ChaosMove CurrentMove => MoveList[MoveIndex];

        NPCDespawnHandler despawnHandler;

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            Lighting.AddLight(NPC.Center, 0.5f, 0.1f, 0.6f);

            if (MoveList == null || MoveIndex >= MoveList.Count) MoveIndex = 0;

            CurrentMove.Move();
        }

        #region IA

        private void NormalPattern() //Setup the patterns for the boss
        {
            MoveTimer++;

            if (MoveTimer % 48 == 0)
            {
                PurpleCrushBarrage();
                
            }

            if (MoveTimer % 35 == 0)
            {
                DemonSickle(); 
            }

            if (MoveTimer % 210 == 0)
            {
                BlackFire();   
            }

            if (MoveTimer % 210 == 20 && Phase2)
            {
                BlackFire();   
            }

            if (MoveTimer >= 900)
            {
                NextMove(); 
                MoveTimer = 0;
            }

            ChaosMovement();
        }

        private void ChaosTeleportPattern() //How the teleport attack works 
        {
            MoveTimer++;

            int telegraphDuration = 95; 
            int attackDuration = MoveTimer - telegraphDuration;

            if (MoveTimer <= telegraphDuration)
            {
                TelegraphChaosAttack();

                if (MoveTimer == 1)
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item163 with { Volume = 1.2f, Pitch = -0.4f }, NPC.Center);

                ChaosMovement();
                return;
            }

            if (attackDuration >= 0 && attackDuration % 45 == 0 && attackDuration / 45 <= 5)
            {
                NPC.velocity = Vector2.Zero;

                TeleportCircleAttack();
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8, NPC.Center);
            }

            if (attackDuration >= 285) 
            {
                NextMove(); 
                MoveTimer = 0;
            }
        }

        #endregion

        #region Pattern

        private void PurpleCrushBarrage() //The base spamming attack 
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return; //I use this form instead, it works so ? 

            Vector2 direction = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1f);
            float baseRot = direction.ToRotation();

            for (int i = -1; i <= 1; i++)
            {
                float offset = MathHelper.ToRadians(9f * i);
                Vector2 vel = new Vector2((float)Math.Cos(baseRot + offset), (float)Math.Sin(baseRot + offset)) * 14f;

                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                    ModContent.ProjectileType<CrazedPurpleCrush>(), purpleCrushDamage, 0f, Main.myPlayer);
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f }, NPC.Center);
        }

        private void DemonSickle() // Shoots consistently demon sickle 
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            Vector2 vel = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 2.7f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
            ProjectileID.DemonSickle, sickleDamage, 0f, Main.myPlayer);

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.4f }, NPC.Center);
        }

        private void BlackFire() //Every 3 seconds, shoots 9 projectiles in a circle, almost the same attack than attraidieis 
        {
            for (int i = 0; i < 9; i++)
            {
                Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / 9f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<EnemyAttraidiesBlackFire>(), BlackFireDamage, -1, Main.myPlayer, -1);
            }

            for (int i = 0; i < 24; i++)
            {
                Dust.NewDustPerfect(NPC.Center, DustID.DemonTorch, Main.rand.NextVector2CircularEdge(4, 4), Scale: 2.2f);
            }
        }

        private void TelegraphChaosAttack() //The telegraph before the teleport circle attack
        {
            for (int i = 0; i < 9; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(95, 95);
                Dust dust = Dust.NewDustPerfect(NPC.Center + offset, DustID.PurpleTorch, offset * 0.04f, Scale: 2.6f);
                dust.noGravity = true;
            }
        }

        private void TeleportCircleAttack() //actual attack of the teleport pattern, shoots 13 projectile + 1 demon spirit in a circle
        {
            float angle = Main.rand.NextFloat(MathHelper.TwoPi);
            NPC.Center = Main.player[NPC.target].Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * 650f;
            NPC.netUpdate = true;

            if (Main.netMode == NetmodeID.MultiplayerClient) return;

            float speed = 11f;

            for (int i = 0; i < 13; i++)
            {
                float rot = MathHelper.TwoPi * i / 13f;
                Vector2 vel = new Vector2(MathF.Cos(rot), MathF.Sin(rot)) * speed;

                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel, ModContent.ProjectileType<RageDemonBolt>(), sickleDamage, 0f, Main.myPlayer);
            }

            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<DemonSpirit>(), sickleDamage, 0f, Main.myPlayer);

            for (int i = 0; i < 38; i++)
            {
                Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch, Main.rand.NextVector2CircularEdge(5, 5), Scale: 3f);
            }
        }

        private void ChaosMovement() //The IA and Movement of the boss
        {
            float maxSpeed = Phase2 ? 11.2f : 7f;
            float acceleration = Phase2 ? 0.158f : 0.09f;

            Vector2 targetDirection = Main.player[NPC.target].Center - NPC.Center;
            targetDirection.Normalize();

            NPC.velocity += targetDirection * acceleration;
            if (NPC.velocity.Length() > maxSpeed)
                NPC.velocity = Vector2.Normalize(NPC.velocity) * maxSpeed;

            if (NPC.velocity.X > 0.1f) NPC.spriteDirection = 1;
            if (NPC.velocity.X < -0.1f) NPC.spriteDirection = -1;

            if (chargeDamageFlag)
            {
                NPC.damage = 120;
                if (NPC.Hitbox.Intersects(Main.player[NPC.target].Hitbox))
                {
                    chargeDamageFlag = false;
                    NPC.damage = 100;
                }
            }
        }

        private void NextMove()
        {
            MoveIndex++;
            if (MoveIndex >= MoveList.Count) MoveIndex = 0;
            MoveTimer = 0;
        }

        private void InitializeMoves()
        {
            MoveList = new List<ChaosMove>
            {
                new ChaosMove(NormalPattern, null, 0, "Normal Chaos"),
                new ChaosMove(ChaosTeleportPattern, null, 1, "Chaos Circle Attack")
            };
        }

        #endregion

        #region Gore

        public override void FindFrame(int frameHeight)
        {
            NPC.rotation = NPC.velocity.X * 0.08f;
            NPC.spriteDirection = NPC.velocity.X < 0 ? -1 : 1;

            NPC.frameCounter++;
            if (NPC.frameCounter >= 4)
            {
                NPC.frameCounter = 0;
                NPC.frame.Y += frameHeight;
                if (NPC.frame.Y >= Main.npcFrameCount[NPC.type] * frameHeight)
                {
                    NPC.frame.Y = 0;
                }
            }
        }

        public override void OnKill() //special death animation
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), (int)NPC.position.X, (int)NPC.position.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.ChaosDeathAnimation>(), 0, 0f, Main.myPlayer);
            }

            if (tsorcRevampWorld.RemixMap && !tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>())))
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Chaos.DarknessLifted"), new Color(255, 225, 20));
            }
        }

        #endregion

        public override bool CheckActive() => false;

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.ChaosBag>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ElfinBow>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<HolyWarElixir>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulOfChaos>(), 1, 2, 4));
            npcLoot.Add(notExpertCondition);
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
        }

    }

    class ChaosMove
    {
        public Action Move;
        public Action Draw;
        public int ID;
        public string Name;

        public ChaosMove(Action MoveAction, Action DrawAction, int MoveID, string AttackName)
        {
            Move = MoveAction;
            Draw = DrawAction;
            ID = MoveID;
            Name = AttackName;
        }
    }
}