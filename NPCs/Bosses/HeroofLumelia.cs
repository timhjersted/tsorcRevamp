using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Ammo;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.Tools;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    class HeroofLumelia : ModNPC, IStaggerable
    {
        private enum HeroAttack
        {
            None,
            Knife,
            SmokeBomb
        }

        private const int WeaponTellTime = 60;
        private const int FlashTellTime = 25;
        private const int MeleeRangeSkip = 120;
        private const int ArrowRainNearDistance = 130;
        private const int ArrowRainFarDistance = 800;

        public int throwingKnifeDamage = 34;
        public int smokebombDamage = 43;
        int herosArrowDamage = 42;

        bool wolfSpawned1 = false;
        bool wolfSpawned2 = false;
        bool wolfSpawned3 = false;
        float customSpawn1;

        HeroAttack currentAttack = HeroAttack.None;
        int attackTimer;
        int idleTimer = 120;
        int comboAttacksRemaining;
        int attacksThisCombo;
        Vector2 storedPlayerPosition = Vector2.Zero;

        NPCDespawnHandler despawnHandler;

        static Texture2D knifeTexture;
        static Texture2D bombTexture;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.scale = 1.1f;
            NPC.npcSlots = 5;
            AnimationType = 28;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 75;
            NPC.defense = 75;
            NPC.boss = true;
            NPC.lifeMax = 8000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 137430;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0.15f;
            NPC.rarity = 4;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.HeroofLumelia.DespawnHandler"), Color.Gold, DustID.GoldFlame);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.Agility = 0.35f;
            globalNPC.NavSearchRadius = 80;
            globalNPC.MaxJumpPower = 12f;
            globalNPC.MaxJumpBoost = 6f;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;

            globalNPC.CanJumpBeforeAttack = true;
            globalNPC.JumpBeforeAttackChance = 0.45f;
            globalNPC.JumpBeforeAttackDelay = 8;
            globalNPC.JumpBeforeAttackPower = 8f;
            globalNPC.JumpBeforeAttackRequiresGrounded = false;
            globalNPC.JumpBeforeAttackShortForwardHop = true;
            globalNPC.JumpBeforeAttackHighForwardHop = true;
            globalNPC.JumpBeforeAttackOffensiveLeap = true;
            globalNPC.JumpBeforeAttackDesperateHighHop = true;

            EvasiveProfile.RedKnight(globalNPC);
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player P = spawnInfo.Player;
            if (Main.hardMode && !tsorcRevampWorld.SuperHardMode && P.ZoneOverworldHeight && P.ZoneDesert && tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.HeroofLumelia>())) && !(P.ZoneCorrupt || P.ZoneCrimson) && !P.ZoneBeach && Main.rand.NextBool(500))
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.HeroofLumelia.Spawn"), 175, 75, 255);
                return 1;
            }

            return 0;
        }
        #endregion

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.AttackTelegraphing = currentAttack != HeroAttack.None && attackTimer <= WeaponTellTime - FlashTellTime;
            globalNPC.AttackCommitted = currentAttack != HeroAttack.None && attackTimer > WeaponTellTime - FlashTellTime;

            tsorcRevampAIs.FighterAI(NPC, 3f, 0.05f, 0.2f, canTeleport: true, doorBreakingDamage: 10, enragePercent: 0.4f, enrageTopSpeed: 5f, lavaJumping: true, canDodgeroll: false, canPounce: false);

            if (Main.netMode == NetmodeID.MultiplayerClient || Main.player[NPC.target].dead)
            {
                return;
            }

            HandleSummons();
            HandleArrowRain();
            HandleAttackBrain(globalNPC);
        }

        private Player TargetPlayer => Main.player[NPC.target];

        private void HandleSummons()
        {
            if (NPC.Distance(TargetPlayer.Center) <= 220)
            {
                return;
            }

            if ((customSpawn1 == 0) && NPC.life <= 4500)
            {
                SpawnSupport(NPCID.EnchantedSword);
                customSpawn1 += 1f;
            }
            if ((customSpawn1 == 1) && NPC.life <= 3500)
            {
                SpawnSupport(NPCID.EnchantedSword);
                customSpawn1 += 1f;
            }
            if ((customSpawn1 == 2) && NPC.life <= 3000)
            {
                SpawnSupport(ModContent.NPCType<NPCs.Enemies.ManHunter>());
                customSpawn1 += 1f;
            }

            if (!wolfSpawned1 && NPC.life <= 2500)
            {
                SpawnSupport(NPCID.Wolf);
                SpawnSupport(NPCID.Wolf);
                SpawnSupport(ModContent.NPCType<NPCs.Enemies.Assassin>());
                SoundEngine.PlaySound(SoundID.NPCHit6 with { Volume = 0.3f, Pitch = -0.01f }, NPC.Center);
                wolfSpawned1 = true;
            }
            if (!wolfSpawned2 && NPC.life <= 1500)
            {
                SpawnSupport(NPCID.Wolf);
                SpawnSupport(NPCID.Wolf);
                SoundEngine.PlaySound(SoundID.NPCHit6 with { Volume = 0.3f, Pitch = -0.02f }, NPC.Center);
                wolfSpawned2 = true;
            }
            if (!wolfSpawned3 && NPC.life <= 1000)
            {
                SpawnSupport(NPCID.Wolf);
                SpawnSupport(ModContent.NPCType<NPCs.Enemies.RedCloudHunter>());
                SoundEngine.PlaySound(SoundID.NPCHit6 with { Volume = 0.3f, Pitch = 0.01f }, NPC.Center);
                wolfSpawned3 = true;
            }
        }

        private void SpawnSupport(int npcType)
        {
            NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, npcType, 0);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
            Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, NPC.velocity.X, NPC.velocity.Y);
            NPC.netUpdate = true;
        }

        private void HandleArrowRain()
        {
            float distance = NPC.Distance(TargetPlayer.Center);
            if (distance > ArrowRainNearDistance && Main.rand.NextBool(350))
            {
                if (Main.rand.NextBool(4))
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.HeroofLumelia.Archers1"), 175, 75, 255);
                }
                SpawnArrowRain(10, 200, -800f, 7.1f);
            }

            if (distance > ArrowRainFarDistance && Main.rand.NextBool(100))
            {
                if (Main.rand.NextBool(4))
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.HeroofLumelia.Archers2"), 175, 75, 255);
                }
                SpawnArrowRain(12, 400, -900f, 9.1f);
            }
        }

        private void SpawnArrowRain(int count, int horizontalSpread, float verticalOffset, float fallSpeed)
        {
            Player target = TargetPlayer;
            for (int i = 0; i < count; i++)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), target.position.X - horizontalSpread / 2f + Main.rand.Next(horizontalSpread), target.position.Y + verticalOffset, (-50 + Main.rand.Next(100)) / 10f, fallSpeed, ModContent.ProjectileType<Projectiles.Enemy.HerosArrow>(), herosArrowDamage, 2f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item5, NPC.Center);
            }
            NPC.netUpdate = true;
        }

        private void HandleAttackBrain(tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.StaggerTimer > 0)
            {
                return;
            }

            if (currentAttack == HeroAttack.None)
            {
                if (idleTimer > 0)
                {
                    idleTimer--;
                    return;
                }

                if (comboAttacksRemaining <= 0)
                {
                    comboAttacksRemaining = RollComboLength();
                    attacksThisCombo = 0;
                }

                StartAttack(ChooseAttack());
                return;
            }

            attackTimer++;
            if (currentAttack == HeroAttack.Knife && NPC.life <= 1000 && attackTimer >= 20 && attackTimer < WeaponTellTime && attackTimer % 6 == 0 && Main.rand.NextBool(2))
            {
                FireKnife(8f, Main.rand.NextVector2Circular(-4, -2));
            }
            if (attackTimer == WeaponTellTime - FlashTellTime)
            {
                SpawnTelegraphFlash();
                storedPlayerPosition = TargetPlayer.Center;
            }
            if (attackTimer == WeaponTellTime)
            {
                FireCurrentAttack();
                EndAttack();
            }
        }

        private int RollComboLength()
        {
            float lifeRatio = NPC.life / (float)NPC.lifeMax;
            if (lifeRatio <= 0.2f)
            {
                return Main.rand.Next(4, 6);
            }
            if (lifeRatio <= 0.4f)
            {
                return Main.rand.Next(2, 4);
            }
            return 1;
        }

        private HeroAttack ChooseAttack()
        {
            if (NPC.Distance(TargetPlayer.Center) < MeleeRangeSkip)
            {
                return HeroAttack.SmokeBomb;
            }
            return Main.rand.NextBool(2) ? HeroAttack.Knife : HeroAttack.SmokeBomb;
        }

        private void StartAttack(HeroAttack attack)
        {
            currentAttack = attack;
            attackTimer = 0;
            storedPlayerPosition = TargetPlayer.Center;
            NPC.TargetClosest(true);
            NPC.netUpdate = true;
        }

        private void EndAttack()
        {
            currentAttack = HeroAttack.None;
            attackTimer = 0;
            comboAttacksRemaining--;
            attacksThisCombo++;

            if (comboAttacksRemaining > 0)
            {
                idleTimer = 120;
            }
            else
            {
                idleTimer = Main.rand.Next(180, attacksThisCombo >= 4 ? 301 : 241);
                attacksThisCombo = 0;
            }
            NPC.netUpdate = true;
        }

        private void SpawnTelegraphFlash()
        {
            Vector2 spawnPosition = NPC.position;
            if (NPC.direction == 1)
            {
                spawnPosition.X += NPC.width;
            }
            Color flashColor = currentAttack == HeroAttack.Knife ? Color.White : Color.Green;
            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(flashColor));
        }

        private void FireCurrentAttack()
        {
            if (!Collision.CanHit(NPC.Center, 1, 1, TargetPlayer.Center, 1, 1))
            {
                return;
            }

            if (currentAttack == HeroAttack.Knife)
            {
                FireKnife(12f, TargetPlayer.velocity / 2f);
            }
            else if (currentAttack == HeroAttack.SmokeBomb)
            {
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, storedPlayerPosition, 10, fallback: true);
                speed += Main.rand.NextVector2Circular(-4, 0);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySmokebomb>(), smokebombDamage, 0f, Main.myPlayer);
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.3f }, NPC.Center);
            }
        }

        private void FireKnife(float velocity, Vector2 velocityOffset)
        {
            Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, storedPlayerPosition, velocity, fallback: true);
            speed += velocityOffset;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnife>(), throwingKnifeDamage, 0f, Main.myPlayer);
            SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
        }
        public void OnStagger(NPC npc)
        {
            currentAttack = HeroAttack.None;
            attackTimer = 0;
            idleTimer = 120;
            comboAttacksRemaining = 0;
            attacksThisCombo = 0;
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        private Vector2 HeldWeaponWorld()
        {
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            float bob = NPC.velocity.Y == 0f ? (float)Math.Sin(frame * MathHelper.Pi / 4f) * 2f : -8f;
            return NPC.Center + new Vector2(NPC.spriteDirection * 8f, -4f + bob + NPC.gfxOffY);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (knifeTexture == null)
            {
                knifeTexture = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/EnemyThrowingKnifeSmall", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            if (bombTexture == null)
            {
                bombTexture = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/EnemySmokebomb", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            if (currentAttack == HeroAttack.None)
            {
                return;
            }

            Texture2D texture = currentAttack == HeroAttack.Knife ? knifeTexture : bombTexture;
            Vector2 drawPosition = HeldWeaponWorld() - Main.screenPosition;
            Vector2 aim = attackTimer >= WeaponTellTime - FlashTellTime ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
            float sway = attackTimer < WeaponTellTime - FlashTellTime ? (float)Math.Sin(Main.GameUpdateCount * 0.12f) * 0.18f : 0f;
            float rotation = aim.ToRotation() + MathHelper.PiOver2 + sway;
            Vector2 origin = currentAttack == HeroAttack.Knife ? new Vector2(texture.Width / 2f, texture.Height * 0.75f) : new Vector2(texture.Width / 2f, texture.Height / 2f);

            spriteBatch.Draw(texture, drawPosition, null, drawColor, rotation, origin, NPC.scale, SpriteEffects.None, 0);

            if (currentAttack == HeroAttack.SmokeBomb)
            {
                Lighting.AddLight(NPC.Center, Color.Green.ToVector3());
                if (Main.rand.NextBool(2))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y);
                }
            }
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Hero of Lumelia Gore 3").Type, 1f);
            }
        }
        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 60 * 60, false);
            target.AddBuff(ModContent.BuffType<Crippled>(), 20 * 60, false);

            if (Main.rand.NextBool(2))
            {
                target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 30 * 60, false);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.HeroOfLumeliaBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 7, 14));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ArrowOfBard>(), 1, 7, 14));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.WaterWalkingBoots));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<MagicBarrierScroll>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Humanity>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ItemID.ObsidianSkinPotion));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 1, 1, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 1, 1, 2));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<GreaterRestorationPotion>(), 1, 1, 2));
            npcLoot.Add(notExpertCondition);
        }
    }
}
