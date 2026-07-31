using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Tools;

namespace tsorcRevamp.NPCs.Enemies.GhostFighter
{
    public class GhostOfTheForgottenWarrior : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
        }
        public int warriorDamage = 20;
        public override void SetDefaults()
        {
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.damage = 35;
            NPC.lifeMax = 100;
            NPC.defense = 16;
            NPC.value = 500; // was 35
            NPC.width = 20;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.knockBackResist = 0.0f;
            NPC.lavaImmune = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GhostOfTheForgottenWarriorBanner>();

            AnimationType = NPCID.GoblinWarrior;
            if (Main.hardMode)
            {
                NPC.lifeMax = 200;
                NPC.defense = 40;
                NPC.value = 1000; // was 45 but 30 defense
                NPC.damage = 60;
                topSpeed = 1.1f;
                warriorDamage = 30;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 1000;
                NPC.defense = 90;
                NPC.damage = 90;
                NPC.value = 4000; // was 100 but worse stats
                topSpeed = 1.8f;
                warriorDamage = 50; // didn't have scaling damage before
            }

            // "Ephemeral Axe Throw" — commitFraction 0.5: first half of the tell is stagger-cancellable (by magic, per
            // the ghost poise), second half is committed/hyperarmor.
            int axeProjectileType = ModContent.ProjectileType<Projectiles.Enemy.EnemyEphemeralThrowingAxeProj>();
            UsefulFunctions.AddAttack(NPC, 300, axeProjectileType, warriorDamage, 8, SoundID.Item17,
                telegraphColor: Color.Orange, stopBeforeFiring: false, needsLineOfSight: true,
                telegraphTime: 45, commitFraction: 0.5f, lockAimAtTelegraph: true);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            HumanoidMeleeProfile meleeProfile = HumanoidMeleeProfile.Standard(
                warriorDamage,
                (int)(warriorDamage * 1.3f),
                guardPressureUnblockable: true,
                guardPressureTelegraphTicks: 90);
            globalNPC.ConfigureHumanoidMelee(meleeProfile);
            CombatTempoProfile.Standard(globalNPC,
                new CombatComboMove(axeProjectileType, 96f, 360f, canRepeat: true, weight: 1f),
                new CombatComboMove(CombatComboMoveKey.CloseHopMelee, 0f, 90f, canRepeat: true, weight: 1.15f),
                new CombatComboMove(CombatComboMoveKey.LongHopMelee, 112f, 240f, canRepeat: false, weight: 0.95f));
            globalNPC.CanPassThroughWalls = true;
            EvasiveProfile.Ghost(globalNPC); // phase back / dash / i-frame quick-step on hit
            // Smart positioning: ordinary pressure favors a 4-13 tile band; distant projectile hits make it close.
            globalNPC.KiteRangeMin = 4f;
            globalNPC.KiteRangeMax = 13f;
            globalNPC.KiteLooseness = 0.5f;
            globalNPC.HasGhostAfterimages = true;
            globalNPC.MaxJumpPower = 9f;
            globalNPC.MaxJumpBoost = 5f;
            // Step 6 ghost levers: drift (Wander) around where it lost the player when it gives up.
            globalNPC.PatrolMode = NPCs.PatrolMode.Wander;
            globalNPC.PatrolAnchorSource = NPCs.PatrolAnchorSource.GiveUpLocation;
            globalNPC.NavSearchRadius = 40; // Phase 2: SmartFighter4AI movement
            globalNPC.CanUseRopes = true;
            globalNPC.TeleportTelegraphTime = 80;
            globalNPC.TeleportDustType = DustID.Smoke;
            globalNPC.TeleportDustColor = new Color(55, 55, 65);
            globalNPC.TeleportDustScale = 0.65f;
            globalNPC.TeleportDustCount = 20;
        }

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0f;

            if (!Main.hardMode && NPC.downedBoss3 && spawnInfo.Player.ZoneDungeon)
            {
                return 0.25f;
            }
            else if (Main.hardMode && spawnInfo.Player.ZoneDungeon)
            {
                return 0.12f;
            }
            else if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon)
            {
                return 0.1f; //.05 is 3.85%
            }
            if (spawnInfo.Player.ZoneGraveyard)
            {
                return 0.2f; // was 0.17
            }
            return chance;
        }

        #endregion


        float topSpeed = 0.8f;

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, topSpeed, .04f, 0.2f, false, enragePercent: 0.2f, enrageTopSpeed: 2.1f); // experimenting with no longer teleporting

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // Cancel the axe charge-up when we lose line of sight — don't telegraph or fire blind.
            // Only reset when the timer is non-zero to avoid a pointless netUpdate every single tick.
            bool hasLos = Collision.CanHit(NPC.position, NPC.width, NPC.height,
                                            Main.player[NPC.target].position,
                                            Main.player[NPC.target].width,
                                            Main.player[NPC.target].height);
            if (!hasLos && globalNPC.ProjectileTimer > 0f)
            {
                globalNPC.ProjectileTimer = 0f;
                NPC.netUpdate = true;
            }

            // Being hit while winding up also cancels the shot (25% chance — not trivially interrupted,
            // but a solid hit should reset the charge).
            if (NPC.justHit && globalNPC.ProjectileTimer < globalNPC.ProjectileTelegraphStart && Main.rand.NextBool(4))
            {
                globalNPC.ProjectileTimer = 0f;
                NPC.netUpdate = true;
            }
        }

        #region Draw Axe Texture
        static Texture2D axeTexture;
        static Texture2D handTexture;
        const float FrameW = 66f;
        const float FrameH = 56f;
        const float HeldAxeWindupTicks = 90f;
        const float HeldAxeScale = 0.8f;
        static readonly float HeldAxeRotationOffset = MathHelper.Pi / 4f;
        static readonly Vector2[] HandPixel = new Vector2[16]
        {
            new Vector2(45, 30), // 0 idle
            new Vector2(45, 25), // 1 jump
            new Vector2(46, 33), // 2
            new Vector2(48, 31), // 3
            new Vector2(48, 31), // 4
            new Vector2(48, 31), // 5
            new Vector2(48, 33), // 6
            new Vector2(46, 33), // 7
            new Vector2(46, 33), // 8
            new Vector2(46, 33), // 9
            new Vector2(44, 31), // 10
            new Vector2(42, 31), // 11
            new Vector2(42, 31), // 12
            new Vector2(43, 32), // 13
            new Vector2(46, 33), // 14
            new Vector2(46, 33), // 15
        };
        static readonly Vector2 AxeGripOrigin = new Vector2(13f, 42f);

        Vector2 CurrentHandWorld()
        {
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            if (frame < 0 || frame >= HandPixel.Length)
            {
                frame = 0;
            }

            Vector2 fp = HandPixel[frame];
            float x = NPC.Center.X + (fp.X - FrameW / 2f) * NPC.scale * -NPC.spriteDirection;
            float y = NPC.Center.Y + 24f + NPC.gfxOffY + (fp.Y - FrameH) * NPC.scale;
            return new Vector2(x, y);
        }

        float GetMeleeAxeRotation(tsorcRevampGlobalNPC globalNPC)
        {
            int direction = globalNPC.ActiveCombatMeleeDirection;
            float readyRotation = direction * 0.2f;
            float windupRotation = direction * -0.9f;
            float strikeRotation = direction * 1.35f;

            if (globalNPC.ActiveCombatMeleeTimer <= globalNPC.ActiveCombatMeleeTelegraphTicks)
            {
                float windupProgress = MathHelper.SmoothStep(0f, 1f, globalNPC.CombatMeleeTelegraphProgress);
                return MathHelper.Lerp(readyRotation, windupRotation, windupProgress);
            }

            if (globalNPC.InCombatMeleeHitWindow)
            {
                float swingProgress = MathHelper.SmoothStep(0f, 1f, globalNPC.CombatMeleeActiveProgress);
                return MathHelper.Lerp(windupRotation, strikeRotation, swingProgress);
            }

            float recoveryProgress = MathHelper.SmoothStep(
                0f, 1f, globalNPC.CombatMeleeFollowThroughProgress);
            return MathHelper.Lerp(strikeRotation, readyRotation, recoveryProgress);
        }

        void DrawHeldAxe(SpriteBatch spriteBatch, Vector2 handPosition, float rotation, Color drawColor,
            tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.ActiveAttackBypassesShield)
            {
                Vector2 axeHeadDirection = -Vector2.UnitY.RotatedBy(rotation);
                Vector2 auraCenter = handPosition + axeHeadDirection * 18f * NPC.scale;
                AttackTelegraphDraw.DrawUnblockableWeaponAura(
                    spriteBatch, axeTexture, handPosition, null, rotation, AxeGripOrigin, NPC.scale * HeldAxeScale);
                drawColor = Color.Lerp(drawColor, new Color(255, 65, 65), 0.8f);
                Lighting.AddLight(auraCenter + Main.screenPosition, Color.Red.ToVector3() * 0.7f);
            }

            // AxeGripOrigin remains on CurrentHandWorld while only rotation changes, so the weapon never orbits
            // or detaches from the fixed hand pixel during the windup, cleave, and recovery.
            spriteBatch.Draw(axeTexture, handPosition, null, drawColor, rotation,
                AxeGripOrigin, NPC.scale * HeldAxeScale, SpriteEffects.None, 0f);
        }
        void DrawHandOverlay(SpriteBatch spriteBatch, Color drawColor)
        {
            if (handTexture == null)
            {
                return;
            }

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH);
            Vector2 drawPosition = NPC.Center + new Vector2(0f, 24f + NPC.gfxOffY) - Main.screenPosition;
            spriteBatch.Draw(handTexture, drawPosition, sourceRectangle, drawColor, NPC.rotation, new Vector2(FrameW / 2f, FrameH), NPC.scale, effects, 0f);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (axeTexture == null || axeTexture.IsDisposed)
            {
                axeTexture = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/EnemyEphemeralThrowingAxeProj", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            if (handTexture == null || handTexture.IsDisposed)
            {
                handTexture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/GhostFighter/GhostOfTheForgottenWarrior_Hand", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (globalNPC.CombatMeleeActive)
            {
                float meleeRotation = GetMeleeAxeRotation(globalNPC);
                DrawHeldAxe(spriteBatch, CurrentHandWorld() - Main.screenPosition, meleeRotation, drawColor, globalNPC);
                DrawHandOverlay(spriteBatch, drawColor);
                return;
            }

            if (!globalNPC.InCombatComboRecovery
                && globalNPC.ProjectileTimer >= globalNPC.ProjectileTimerCap - HeldAxeWindupTicks)
            {
                Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 0.3f);
                if (Main.rand.NextBool(3))
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y);
                }

                Vector2 aimTarget = globalNPC.LockedShotTargetPosition != Vector2.Zero
                    ? globalNPC.LockedShotTargetPosition
                    : Main.player[NPC.target].Center;
                float rotation = UsefulFunctions.Aim(NPC.Center, aimTarget, 1).ToRotation()
                    + MathHelper.PiOver2 - NPC.spriteDirection * HeldAxeRotationOffset;
                DrawHeldAxe(spriteBatch, CurrentHandWorld() - Main.screenPosition, rotation, drawColor, globalNPC);
                DrawHandOverlay(spriteBatch, drawColor);
            }
        }
        #endregion

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int DustType = 5;
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustType);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 25; i++)
                {
                    Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
                }
                if (!Main.dedServ)
                {
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 1").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 3").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 2").Type, 1f);
                    Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Wild Warrior Gore 3").Type, 1f);
                }
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ItemID.GoldenKey, 10));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<EphemeralDust>(), 1, 2, 4));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<Items.Weapons.Throwing.EphemeralThrowingSpear>(), 10, 15, 26));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.DownedSkeletronRule, ModContent.ItemType<GreatMagicShieldScroll>(), 30));
            IItemDropRule hmCondition = new LeadingConditionRule(new Conditions.IsHardmode());
            hmCondition.OnSuccess(ItemDropRule.Common(ItemID.SoulofNight));
            npcLoot.Add(hmCondition);
        }
    }
}
