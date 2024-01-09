using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Placeable.Trophies;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses
{
    [AutoloadBossHead]
    class SpazmatismV2 : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 8;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
        }
        public override void SetDefaults()
        {
            NPC.damage = 50;
            NPC.defense = 25;
            AnimationType = -1;
            NPC.lifeMax = 90000;
            NPC.timeLeft = 22500;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.boss = true;
            NPC.width = 80;
            NPC.height = 80;
            NPC.HitSound = SoundID.NPCHit42;

            NPC.value = 600000;
            NPC.aiStyle = -1;

            //Terraria.GameContent.UI.BigProgressBar.IBigProgressBar bossBar;
            //Main.BigBossProgressBar.TryGetSpecialVanillaBossBar(NPC.netID, out bossBar);
            //bossBar.Draw();

            InitializeMoves();
        }


        int FireJetDamage = 40;
        int CursedMalestromDamage = 50;
        int CursedFireballDamage = 35;

        //If this is set to anything but -1, the boss will *only* use that attack ID
        int testAttack = -1;
        float transformationTimer;
        SpazMove CurrentMove
        {
            get
            {
                //Its moves have a different order in phase 2
                if (PhaseTwo)
                {
                    return Phase2MoveList[MoveIndex];
                }
                else
                {
                    return MoveList[MoveIndex];
                }
            }
        }

        List<SpazMove> MoveList;
        List<SpazMove> Phase2MoveList;
        public static int secondStageHeadSlot = -1;

        //Controls what move is currently being performed
        public int MoveIndex
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        //Used by moves to keep track of how long they've been going for
        public int MoveTimer
        {
            get => (int)NPC.ai[1];
            set => NPC.ai[1] = value;
        }

        public bool PhaseTwo
        {
            //get => true;
            get => transformed;
        }

        public Player target
        {
            get => Main.player[NPC.target];
        }

        int finalStandLevel = 0;
        int finalStandTimer = 0;
        int finalStandDelay = 0;
        int deathTimer;
        public override void AI()
        {
            MoveTimer++;
            HandleAura();
            FindFrame(0);

            //This will be changed by other attacks
            NPC.damage = 0;

            if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 30 == 1)
            {
                NPC.netUpdate = true;
            }

            //Smart rotation
            //The conversions are necessary to avoid some XNA rotation bullshit
            Vector2 targetRotation = rotationTarget.ToRotationVector2();
            Vector2 currentRotation = NPC.rotation.ToRotationVector2();
            Vector2 nextRotationVector = Vector2.Lerp(currentRotation, targetRotation, rotationSpeed);
            NPC.rotation = nextRotationVector.ToRotation();

            //HandleLife will block the bosses normal AI if is in a special state (final stand, dying, etc)
            if (!HandleLife())
            {
                return;
            }

            //Initialize move list
            if (MoveList == null)
            {
                InitializeMoves();
            }

            //Debugging
            if (testAttack != -1)
            {
                MoveIndex = testAttack;
            }

            if (MoveTimer < 900)
            {
                CurrentMove.Move();
            }
            else if (MoveTimer < 960)
            {
                //Phase transition
                if (MoveTimer == 901)
                {
                    StartAura(800, 1.08f, 0.07f);
                }
                NPC.velocity *= 0.99f;
            }
            else
            {
                NextAttack();
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.rotation);
            writer.Write(rotationTarget);
            writer.Write(finalStandTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.rotation = reader.ReadSingle();
            rotationTarget = reader.ReadSingle();
            finalStandTimer = reader.ReadInt32();
        }

        //Charges, firing a shotgun spread of eye fire each time it does
        //Phase 2: Fire aura
        void Charging()
        {
            NPC.damage = 170;
            if (PhaseTwo)
            {
                rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
                rotationSpeed = 0.1f;
                //Don't try to dash too close to the start or end
                if (MoveTimer >= 840 || MoveTimer < 90)
                {
                    UsefulFunctions.SmoothHoming(NPC, target.Center, 0.15f, 15, target.velocity, false);
                    return;
                }

                if (MoveTimer % 90 == 0 && MoveTimer >= 90)
                {
                    StartAura(500);
                }

                if (MoveTimer % 90 == 30)
                {
                    NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
                    NPC.velocity = UsefulFunctions.Aim(NPC.Center, target.Center, 21);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Triad.SpazFireJet>(), FireJetDamage, 0.5f, Main.myPlayer, NPC.whoAmI);
                    }
                    NPC.netUpdate = true;
                }
            }
            else
            {
                UsefulFunctions.SmoothHoming(NPC, target.Center, 0.05f, 15, target.velocity, false);

                if (MoveTimer > 800)
                {
                    return;
                }

                //Telegraph for the first second before the starting charge
                if (MoveTimer % 70 == 0 && MoveTimer > 71)
                {
                    NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
                    StartAura(400);
                }
                if (MoveTimer % 70 == 30 && MoveTimer > 71)
                {
                    NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
                    NPC.velocity = UsefulFunctions.Aim(NPC.Center, target.Center, 15);
                    NPC.netUpdate = true;
                }
            }
        }

        //Spams cursed eye fire at the player
        //Phase 2: Flames leave a damaging trail, or maybe it fires 8 in all directions? Unsure
        void Firing()
        {
            Vector2 homingTarget = new Vector2(600, 300);
            if (NPC.Center.X < target.Center.X)
            {
                homingTarget.X *= -1;
            }
            UsefulFunctions.SmoothHoming(NPC, target.Center + homingTarget, 1f, 20);
            NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;

            if (PhaseTwo)
            {
                if (MoveTimer > 120 && MoveTimer % 120 == 80)
                {
                    StartAura(500);
                }
                if (MoveTimer > 120 && MoveTimer % 120 == 0)
                {
                    SoundEngine.PlaySound(SoundID.DD2_BetsyFlameBreath, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Vector2 offset = new Vector2(-50, 0).RotatedBy((NPC.Center - target.Center).ToRotation());
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + offset, UsefulFunctions.Aim(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.CursedMalestrom>(), CursedMalestromDamage, 0.5f, Main.myPlayer);
                    }
                }
            }
            else
            {
                if (MoveTimer % 90 == 0)
                {
                    SoundEngine.PlaySound(SoundID.DD2_BetsyFlameBreath with { Volume = 0.5f, Pitch = 0.9f }, NPC.Center);
                    baseFade = 0.3f;
                    baseRadius = 0.4f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float angle = -MathHelper.Pi / 3;
                        for (int i = 0; i < 3; i++)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(60, 0).RotatedBy(NPC.rotation + MathHelper.PiOver2), UsefulFunctions.Aim(NPC.Center, target.Center, 4).RotatedBy(angle), ModContent.ProjectileType<Projectiles.Enemy.Triad.SpazCursedFireball>(), CursedFireballDamage, 0.5f, Main.myPlayer);
                            angle += MathHelper.Pi / 3;
                        }
                    }
                }
            }
        }

        //Spaz aims down and breathes cursed fire into the earth
        //It erupts in bursts of neon green flame
        //Phase 2: Geysers of continuous flame like kraken has
        void IchorTrackers()
        {
            NPC.rotation = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;


            Vector2 homingTarget = new Vector2(750, 350);
            if (NPC.Center.X < target.Center.X)
            {
                homingTarget.X *= -1;
            }
            UsefulFunctions.SmoothHoming(NPC, target.Center + homingTarget, 0.5f, 20);

            if (MoveTimer < 120)
            {
                return;
            }

            if (!PhaseTwo)
            {
                if (MoveTimer % 180 == 10 && MoveTimer > 60 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    baseFade = 0.3f;
                    baseRadius = 0.4f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<Projectiles.Enemy.Triad.IchorGlob>());
                    }
                }
            }
            else
            {
                if (MoveTimer % 240 == 10 && MoveTimer > 60)
                {
                    baseFade = 0.3f;
                    baseRadius = 0.4f;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        NPC.NewNPCDirect(NPC.GetSource_FromThis(), NPC.Center, ModContent.NPCType<Projectiles.Enemy.Triad.IchorMissile>());
                    }
                }
            }
        }

        float targetAngle;
        float rotationTarget;
        float laserCountdown;
        float rotationSpeed;
        void FinalStand()
        {
            rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;

            if (laserCountdown == 0)
            {
                rotationSpeed = 0.3f;
                if (MoveTimer < 200)
                {
                    UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, -700).RotatedBy(targetAngle + (MathHelper.Pi * 2f / 3f)), 1f, 45);
                    return;
                }
                targetAngle += MathHelper.Pi * 5f / 3f;
                laserCountdown = 616;
                StartAura(650, 1.004f, 0.00f);
            }
            else
            {
                if (laserCountdown > 516)
                {
                    UsefulFunctions.SmoothHoming(NPC, target.Center + new Vector2(0, -700).RotatedBy(targetAngle + (MathHelper.Pi * 2f / 3f)), 1f, 45);
                }
                laserCountdown--;
                NPC.velocity *= 0.95f;

                //Start reducing turn speed
                if (laserCountdown > 386)
                {
                    rotationSpeed = 0.3f;
                }
                else
                {
                    rotationSpeed = 0.02f;
                }

                //Start the countdown 30 frames early to make the boss prematurely slow down
                if (laserCountdown == 586)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, target.Center, 3), ModContent.ProjectileType<Projectiles.Enemy.Triad.MaliciousGaze>(), 0, 0.5f, Main.myPlayer, NPC.whoAmI, 1);
                    }
                }

                //Recoil
                if (laserCountdown == 376)
                {
                    NPC.velocity += new Vector2(7, 0).RotatedBy(NPC.rotation - MathHelper.PiOver2);
                }
            }

            return;
        }

        void FinalFinalStand()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                Vector2 dustPoint = Main.rand.NextVector2Circular(50, 50);
                Vector2 dustVel = Vector2.Normalize(dustPoint);
                Dust.NewDustPerfect(dustPoint + NPC.Center, DustID.CursedTorch, dustVel * Main.rand.NextFloat(1, 2), Scale: Main.rand.NextFloat(1, 3));
                if (Main.GameUpdateCount % 20 == 0)
                {
                    Gore.NewGorePerfect(NPC.GetSource_FromThis(), dustPoint + NPC.Center, Main.rand.NextVector2Circular(1, 1) + NPC.velocity, GoreID.Smoke3);
                }
            }
            rotationSpeed = 0.05f;
            rotationTarget = (NPC.Center - target.Center).ToRotation() + MathHelper.PiOver2;
            if (finalStandTimer % 80 == 0)
            {
                StartAura(800);
            }
            if (finalStandTimer % 100 == 59)
            {
                NPC.velocity = UsefulFunctions.Aim(NPC.Center, target.Center, 27);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Triad.SpazFireJet>(), FireJetDamage, 0.5f, Main.myPlayer, NPC.whoAmI);
                }
                NPC.netUpdate = true;
            }

            if (finalStandTimer == 900)
            {
                finalStandTimer = 0;
            }
        }


        private void NextAttack()
        {
            MoveIndex++;
            if (MoveIndex > MoveList.Count - 1)
            {
                MoveIndex = 0;
            }

            MoveTimer = 0;
        }

        float lightCooldown;
        void Transform()
        {
            NPC.HitSound = SoundID.NPCHit4;
            MoveTimer = 0;
            NPC.velocity *= 0.95f;
            if (Main.netMode != NetmodeID.Server && !Filters.Scene["tsorcRevamp:SpazShockwave"].IsActive())
            {
                Filters.Scene.Activate("tsorcRevamp:SpazShockwave", NPC.Center).GetShader().UseTargetPosition(NPC.Center);
            }

            float animTime = 180f;

            if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:SpazShockwave"].IsActive())
            {
                float opacity = transformationTimer / (animTime / 2);
                if (opacity > 1)
                {
                    opacity = 1;
                }
                if (transformationTimer > animTime)
                {
                    opacity = animTime / transformationTimer;
                }
                float distancePercent = 1 - (transformationTimer / animTime);
                Filters.Scene["tsorcRevamp:SpazShockwave"].GetShader().UseTargetPosition(NPC.Center).UseProgress((float)Math.Pow(distancePercent, 3f)).UseOpacity(opacity * opacity).UseIntensity(0.1f);
            }

            if (!transformed)
            {
                transformationTimer += 2;
            }
            else
            {
                transformationTimer -= 2;
                if (transformationTimer <= 0)
                {
                    if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:SpazShockwave"].IsActive())
                    {
                        Filters.Scene["tsorcRevamp:SpazShockwave"].Deactivate();
                    }
                }
            }

            if (transformationTimer > 240)
            {
                UsefulFunctions.SimpleGore(NPC, "Spazmatism_Gore_1");
                transformed = true;
            }
        }
        float fadeInPercent;
        void HandleAura()
        {
            if (fadeInPercent < 1)
            {
                fadeInPercent += 1f / 30f;
            }
            if (ringCollapse < 0.1f)
            {
                fadePercent += fadeSpeed;
            }
            else
            {
                ringCollapse /= collapseSpeed;
            }

            float intensityMinimum = 0.77f;
            float radiusMinimum = 0.35f;


            if (finalStandTimer > 0)
            {
                intensityMinimum = 0.45f;
                radiusMinimum = 0.4f;
            }

            if (baseFade < intensityMinimum)
            {
                baseFade += 0.02f;
            }
            if (baseFade > intensityMinimum)
            {
                baseFade = intensityMinimum;
            }
            if (baseRadius > radiusMinimum)
            {
                baseRadius -= 0.01f;
            }
            if (baseRadius < radiusMinimum)
            {
                baseRadius = radiusMinimum;
            }
        }

        bool transformed;
        bool HandleLife()
        {
            if (NPC.realLife < 0)
            {
                int? catID = UsefulFunctions.GetFirstNPC(ModContent.NPCType<Cataluminance>());

                if (catID != null)
                {
                    NPC.realLife = catID.Value;
                    NPC.netUpdate = true;
                }
            }

            if (NPC.realLife >= 0 && !Main.npc[NPC.realLife].active)
            {
                OnKill();
                NPC.active = false;
            }
            else if (NPC.realLife >= 0)
            {
                NPC.life = Main.npc[NPC.realLife].life;
                NPC.lifeMax = Main.npc[NPC.realLife].lifeMax;
                NPC.target = Main.npc[NPC.realLife].target;
            }

            //Initiate death at this health gate
            if (NPC.life < NPC.lifeMax * 0.05f)
            {
                HandleDeath();
                deathTimer++;
                return false;
            }

            if (transformationTimer <= 0)
            {
                NPC.dontTakeDamage = false;
            }
            else
            {
                NPC.dontTakeDamage = true;
            }

            //Handle phase change and transformation
            if (NPC.life < NPC.lifeMax * 2f / 3f && (!transformed || transformationTimer > 0))
            {
                Transform();
                return false;
            }

            //Handle final stand
            //if(true)
            if (NPC.life < NPC.lifeMax / 4f)
            {
                if (!NPC.AnyNPCs(ModContent.NPCType<RetinazerV2>()) && finalStandLevel == 0)
                {
                    finalStandDelay = 0;
                    finalStandTimer = 0;
                    finalStandLevel = 1;
                }
                finalStandTimer++;
                if (finalStandDelay < 60)
                {
                    NPC.dontTakeDamage = true;
                    finalStandDelay++;
                    NPC.velocity *= 0.99f;
                    MoveTimer = 0;
                }
                else
                {
                    NPC.dontTakeDamage = false;
                    if (finalStandLevel == 0)
                    {
                        FinalStand();
                    }
                    else
                    {
                        FinalFinalStand();
                    }
                }

                return false;
            }

            return true;
        }

        void HandleDeath()
        {
            NPC.dontTakeDamage = true;
            NPC.velocity *= 0.95f;

            if (deathTimer == 30)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/SoulCrashPre") with { PlayOnlyIfFocused = false, MaxInstances = 0 }, NPC.Center);
            }


            if (Main.netMode != NetmodeID.Server && !Filters.Scene["tsorcRevamp:SpazShockwave"].IsActive())
            {
                Filters.Scene.Activate("tsorcRevamp:SpazShockwave", NPC.Center).GetShader().UseTargetPosition(NPC.Center);
            }

            float animTime = 180f;

            if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:SpazShockwave"].IsActive())
            {
                float opacity = deathTimer / (animTime / 2);
                if (opacity > 1)
                {
                    opacity = 1;
                }

                float distancePercent = 1 - (deathTimer / animTime);
                Filters.Scene["tsorcRevamp:SpazShockwave"].GetShader().UseTargetPosition(NPC.Center).UseProgress((float)Math.Pow(distancePercent, 3f)).UseOpacity(opacity).UseIntensity(0.2f);
            }

            float lightTimer = 20;
            if (deathTimer > 130)
            {
                lightTimer = 8;
            }
            lightCooldown--;
            if (lightCooldown <= 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(NPC.Hitbox), Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.LightRay>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.GreenYellow));
                }
                lightCooldown = lightTimer;
            }

            if (deathTimer > 240)
            {
                UsefulFunctions.BroadcastText(NPC.TypeName + LangUtils.GetTextValue("NPCs.SpazmatismV2.Defeated"), Color.GreenYellow);
                if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:SpazShockwave"].IsActive())
                {
                    Filters.Scene["tsorcRevamp:SpazShockwave"].Deactivate();
                }
                UsefulFunctions.ClearProjectileType(ModContent.ProjectileType<Projectiles.VFX.LightRay>());
                deathTimer = 0;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.BossDeath>(), 0, 0, Main.myPlayer, 3, UsefulFunctions.ColorToFloat(Color.GreenYellow));
                }
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/SoulCrashCut") with { PlayOnlyIfFocused = false, MaxInstances = 0 }, NPC.Center);

                OnKill();
                NPC.dontTakeDamage = false;
                NPC.StrikeNPC(NPC.CalculateHitInfo(999999, 1, true, 0), false, false);
                for (int i = 0; i < 10; i++)
                {
                    CombatText.NewText(NPC.Hitbox, CombatText.DamagedHostile, 9999999, true);
                }
            }
        }

        public override bool CheckDead()
        {
            if (deathTimer > 0)
            {
                return true;
            }
            else
            {
                NPC.life = 1000;
                return false;
            }
        }


        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            if (deathTimer > 0)
            {
                return false;
            }
            return base.CanBeHitByProjectile(projectile);
        }

        public override bool? CanBeHitByItem(Player player, Item item)
        {
            if (deathTimer > 0)
            {
                return false;
            }
            return base.CanBeHitByItem(player, item);
        }
        private void InitializeMoves(List<int> validMoves = null)
        {
            MoveList = new List<SpazMove> {
                new SpazMove(Charging, SpazMoveID.Charging, "Charging"),
                new SpazMove(Firing, SpazMoveID.Firing, "Firing"),
                new SpazMove(IchorTrackers, SpazMoveID.IchorTrackers, "Ichor Trackers"),
                };
            Phase2MoveList = new List<SpazMove> {
                new SpazMove(IchorTrackers, SpazMoveID.IchorTrackers, "Ichor Trackers"),
                new SpazMove(Firing, SpazMoveID.Firing, "Firing"),
                new SpazMove(Charging, SpazMoveID.Charging, "Charging"),
                };
        }

        private class SpazMoveID
        {
            public const short Charging = 0;
            public const short IchorTrackers = 1;
            public const short Firing = 2;
            public const short TBD = 3;
        }
        private class SpazMove
        {
            public Action Move;
            public int ID;
            public Action<SpriteBatch, Color> Draw;
            public string Name;

            public SpazMove(Action MoveAction, int MoveID, string AttackName, Action<SpriteBatch, Color> DrawAction = null)
            {
                Move = MoveAction;
                ID = MoveID;
                Draw = DrawAction;
                Name = AttackName;
            }
        }
        public override bool CheckActive()
        {
            return false;
        }
        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }
        public override void BossHeadSlot(ref int index)
        {
            if (PhaseTwo)
            {
                index = secondStageHeadSlot;
            }
        }
        public override void FindFrame(int frameHeight)
        {
            int frameSize = 1;
            if (!Main.dedServ)
            {
                frameSize = TextureAssets.Npc[NPC.type].Value.Height / Main.npcFrameCount[NPC.type];
            }
            NPC.frameCounter++;
            if (NPC.frameCounter >= 8.0)
            {
                NPC.frame.Y = NPC.frame.Y + frameSize;
                NPC.frameCounter = 0.0;
            }

            if (!transformed)
            {
                if (NPC.frame.Y >= frameSize * Main.npcFrameCount[NPC.type] / 2f)
                {
                    NPC.frame.Y = 0;
                }
            }
            else
            {
                if (NPC.frame.Y >= frameSize * Main.npcFrameCount[NPC.type])
                {
                    NPC.frame.Y = frameSize * Main.npcFrameCount[NPC.type] / 2;
                }
            }
        }
        public override void ModifyHoverBoundingBox(ref Rectangle boundingBox)
        {
            boundingBox = NPC.Hitbox;
        }

        public static Texture2D texture;
        public Effect effect;
        float effectTimer;
        float ringCollapse;
        float fadePercent;
        float effectRadius = 650;
        float fadeSpeed = 0.05f;
        float collapseSpeed = 1.05f;
        float baseFade = 0.77f;
        float baseRadius = 0.25f;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 0f, 0.4f, 0.8f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/SpazAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)(effectRadius / 0.7f), (int)(effectRadius / 0.7f));
            Vector2 origin = sourceRectangle.Size() / 2f;

            Vector3 hslColor = Main.rgbToHsl(Color.GreenYellow);
            if (MoveIndex == 2)
            {
                hslColor = Main.rgbToHsl(Color.Yellow);
            }

            hslColor.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color rgbColor = Main.hslToRgb(hslColor);

            //Pass relevant data to the shader via these parameters
            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseTurbulent.Width);
            effect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["ringProgress"].SetValue(ringCollapse);
            effect.Parameters["fadePercent"].SetValue(fadePercent + (1 - fadeInPercent));
            effect.Parameters["scaleFactor"].SetValue(0.3f);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly / 8f);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, NPC.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, NPC.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Rectangle baseRectangle = new Rectangle(0, 0, 400, 400);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;



            //Pass relevant data to the shader via these parameters
            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseTurbulent.Width);
            effect.Parameters["effectSize"].SetValue(baseRectangle.Size());
            effect.Parameters["effectColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["ringProgress"].SetValue(baseRadius);
            effect.Parameters["fadePercent"].SetValue(baseFade / 1.5f);
            effect.Parameters["scaleFactor"].SetValue(0.5f);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.3f);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, NPC.Center - Main.screenPosition, baseRectangle, Color.White, MathHelper.PiOver2, baseOrigin, NPC.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);



            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
            }

            Color lightingColor = Color.Lerp(Color.White, rgbColor, 0.5f);
            lightingColor = Color.Lerp(drawColor, lightingColor, 0.5f);
            Rectangle sourceRectangle2 = NPC.frame;
            Vector2 origin2 = sourceRectangle2.Size() / 2f;
            spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, sourceRectangle2, lightingColor, NPC.rotation, origin2, 1, SpriteEffects.None, 0f);

            DrawTransformationEffect();
            DrawDeathEffect();
            return false;
        }

        Effect TransformationEffect;
        float starRotation;
        public void DrawTransformationEffect()
        {

            Vector3 hslColor1 = Main.rgbToHsl(Color.GreenYellow);
            Vector3 hslColor2 = Main.rgbToHsl(Color.White);
            hslColor1.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            hslColor2.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color rgbColor1 = Main.hslToRgb(hslColor1);
            Color rgbColor2 = Main.hslToRgb(hslColor2);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            if (TransformationEffect == null)
            {
                TransformationEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatFinalStandAttack", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            starRotation += 0.02f;
            Rectangle starRectangle = new Rectangle(0, 0, 4, 4);
            float attackFadePercent = (float)Math.Pow(1 - (transformationTimer / 60f), 2);
            if (transformationTimer > 60)
            {
                attackFadePercent = 0;
            }
            starRectangle.Width = (int)(starRectangle.Width * transformationTimer);
            starRectangle.Height = (int)(starRectangle.Height * transformationTimer);

            Vector2 starOrigin = starRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            TransformationEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            TransformationEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            TransformationEffect.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            TransformationEffect.Parameters["ringProgress"].SetValue(0.5f);
            TransformationEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            TransformationEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            TransformationEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, starRectangle, Color.White, starRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Pass relevant data to the shader via these parameters
            TransformationEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            TransformationEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            TransformationEffect.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            TransformationEffect.Parameters["ringProgress"].SetValue(0.5f);
            TransformationEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            TransformationEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            TransformationEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, starRectangle, Color.White, -starRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);



            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }
        public void DrawDeathEffect()
        {

            Vector3 hslColor1 = Main.rgbToHsl(Color.GreenYellow);
            Vector3 hslColor2 = Main.rgbToHsl(Color.White);
            hslColor1.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            hslColor2.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color rgbColor1 = Main.hslToRgb(hslColor1);
            Color rgbColor2 = Main.hslToRgb(hslColor2);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            if (TransformationEffect == null)
            {
                TransformationEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatFinalStandAttack", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            starRotation += 0.02f;
            Rectangle starRectangle = new Rectangle(0, 0, 8, 8);
            float attackFadePercent = (float)Math.Pow(1 - (deathTimer / 60f), 2);
            if (deathTimer > 60)
            {
                attackFadePercent = 0;
            }
            starRectangle.Width = (int)(starRectangle.Width * deathTimer);
            starRectangle.Height = (int)(starRectangle.Height * deathTimer);

            Vector2 starOrigin = starRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            TransformationEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            TransformationEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            TransformationEffect.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            TransformationEffect.Parameters["ringProgress"].SetValue(0.5f);
            TransformationEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            TransformationEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            TransformationEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, starRectangle, Color.White, starRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Pass relevant data to the shader via these parameters
            TransformationEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            TransformationEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            TransformationEffect.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            TransformationEffect.Parameters["ringProgress"].SetValue(0.5f);
            TransformationEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            TransformationEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            TransformationEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, starRectangle, Color.White, -starRotation, starOrigin, NPC.scale, SpriteEffects.None, 0);



            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public void StartAura(float radius, float ringSpeed = 1.05f, float fadeOutSpeed = 0.05f)
        {
            effectRadius = radius;
            collapseSpeed = ringSpeed;
            fadeSpeed = fadeOutSpeed;
            fadePercent = 0;
            ringCollapse = 1;
            fadeInPercent = 0;
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SpazmatismTrophy>(), 10));
        }

        //TODO: Copy vanilla death effects
        public override void OnKill()
        {
            UsefulFunctions.SimpleGore(NPC, "Spazmatism_Gore_2");
            UsefulFunctions.SimpleGore(NPC, "Spazmatism_Gore_3");
            if (Main.netMode != NetmodeID.Server && Filters.Scene["tsorcRevamp:SpazShockwave"].IsActive())
            {
                Filters.Scene["tsorcRevamp:SpazShockwave"].Deactivate();
            }
        }
    }
}