using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.Enemy.Marilith;
using tsorcRevamp.Content.Projectiles.Enemy.Okiku;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.Attraidies
{
    /// <summary>
    /// First form: shielded behind Damned Souls, then vulnerable for 1,101 ticks at a time with one attack per window.
    /// At 1,000 HP it transforms for 600 ticks and hands over to the second form.
    /// </summary>
    [AutoloadBossHead]
    public class DarkShogunMask : AttraidiesPuppet
    {
        private const int VulnerableTicks = 1101;
        private const int VulnerableOpeningTicks = 200;
        private const int TransformTicks = 600;
        private const int TransformLife = 1000;
        private const int SoulCount = 6;
        private const int ShieldedAlpha = 180;
        private const int SpawnTellTicks = 30;
        private const float SpawnRadius = 500f;
        private const int ProjectileDamage = 30;

        private const int NebulaInterval = 90;
        private const int ThunderInterval = 200;
        private const int ThunderOffset = 100;
        private const int DetonatorInterval = 200;
        private const int LanceInterval = 80;
        private const float NebulaStep = 0.25f;
        private const float ThunderStep = MathHelper.PiOver2 / 3f;

        private static readonly Color NebulaColor = new Color(235, 90, 200);
        private static readonly Color ThunderColor = new Color(90, 220, 255);
        private static readonly Color DetonatorColor = Color.OrangeRed;
        private static readonly Color LanceColor = Color.Cyan;

        private static Effect shieldEffect;

        private NPCDespawnHandler despawnHandler;
        private bool initiate;
        private bool soulsSpawned;
        private bool transforming;
        private int vulnerableTimer;
        private int attackMode;
        private int transitionTimer;
        private float rotationSpeed = 0.015f;
        private float rotationDistance = 1f;
        private float nebulaRotation;
        private float lightningRotation;
        private bool hasPendingSpawn;
        private Vector2 pendingSpawnOffset;
        private float shieldRadius;
        private float shieldRotation;
        private ArmorShaderData inverseAuraData;

        /// <summary>True once no shield NPC remains. Read by the Damned Souls as well.</summary>
        public bool ShieldBroken => !NPC.AnyNPCs(ModContent.NPCType<AttraidiesShield>());

        public bool Transforming => transforming;

        public float TransformProgress
        {
            get => NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        protected override int ArmorDyeItemType => ItemID.PhaseDye;
        protected override NPCDespawnHandler DespawnHandler => despawnHandler;

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();

            // Set NeedsExpertScaling for lifeMax scaling in multiplayer, otherwise scaling is skipped since NPC.damage=0.
            NPCID.Sets.NeedsExpertScaling[Type] = true;
        }

        public override void SetDefaults()
        {
            ApplyBodyDefaults();
            NPC.defDamage = 20;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = (int)(75000 * (Main.masterMode ? 1.5f : 1));
            NPC.boss = true;
            NPC.value = 50000;
            NPC.rarity = 29;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.DarkShogunMask.DespawnHandler"), Color.DarkMagenta, 54);
        }

        public override void Unload()
        {
            shieldEffect = null;
        }

        protected override void TickForm()
        {
            bool authority = IsAuthority;

            if (authority)
            {
                Main.dayTime = false;
                Main.time = Main.nightLength / 2;
                ShareWorldTime();
            }

            // Waits idle during the arena arrival scene, so the shield, quote and player tether all begin with the fight.
            if (Systems.CutsceneSystem.IsActive && Systems.CutsceneSystem.SceneId == NPCs.Special.AttraidiesCutscenes.ArrivalScene)
            {
                NPC.dontTakeDamage = true;
                NPC.velocity *= 0.9f;
                SetPose(AttraidiesPose.Carry);
                return;
            }

            if (!initiate)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.DarkShogunMask.Quote"), new Color(175, 75, 255));
                initiate = true;
            }

            if (transitionTimer > 0)
            {
                transitionTimer--;
                NPC.velocity *= 0.95f;
            }

            if (transforming)
            {
                RunTransformation(authority);
            }
            else
            {
                RunGuardedFight(authority);
            }
        }

        private void SpawnSoulsAndShield()
        {
            int shield = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AttraidiesShield>(), 0, 0, NPC.whoAmI);

            for (int i = 0; i < SoulCount; i++)
            {
                NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<DamnedSoul>(), 0, i, NPC.whoAmI, shield);
            }

            SpawnFlash();
        }

        private void SpawnFlash()
        {
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 550, 20);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ShockwaveEffect>(), 0, 0, Main.myPlayer, 520, 60);
        }

        private void RunGuardedFight(bool authority)
        {
            float speed = 0.5f;

            if (!ShieldBroken)
            {
                NPC.dontTakeDamage = true;
                NPC.alpha = ShieldedAlpha;
                SetPose(AttraidiesPose.Conduct);

                if (shieldRadius > 100)
                {
                    shieldRadius = 100 + (float)Math.Pow(shieldRadius - 100, .8f);
                }
            }
            else
            {
                if (vulnerableTimer <= 0 && authority)
                {
                    vulnerableTimer = VulnerableTicks;
                    transitionTimer = VulnerableOpeningTicks;
                    SpawnFlash();
                    RequestNetworkSnapshot();
                }

                // The last vulnerable tick restores the shield and moves on to the next attack.
                if (vulnerableTimer > 0)
                {
                    vulnerableTimer--;

                    if (vulnerableTimer == 0 && authority)
                    {
                        SpawnSoulsAndShield();
                        attackMode = (attackMode + 1) % 4;
                        hasPendingSpawn = false;
                        RequestNetworkSnapshot();
                    }
                }

                if (shieldRadius < 500)
                {
                    shieldRadius = 500 - (float)Math.Pow(500 - shieldRadius, .8f);
                }

                if (rotationSpeed < 0.02f)
                {
                    rotationSpeed += 0.00005f;
                }

                SetPose(AttraidiesPose.Carry);
                if (transitionTimer <= 0)
                {
                    TelegraphNextSpawn(authority);
                    RunAttack(authority);
                }

                speed = 2f;
                NPC.dontTakeDamage = false;
                NPC.alpha = 0;
            }

            if (TargetPlayer.Distance(NPC.Center) > 250)
            {
                NPC.velocity = UsefulFunctions.Aim(NPC.Center, TargetPlayer.Center, speed);
            }
            else
            {
                NPC.velocity *= 0.98f;
            }

            // The tether: each client reels in its own player.
            Player localPlayer = LocalLivingPlayer;
            if (localPlayer != null && localPlayer.Distance(NPC.Center) > 500)
            {
                localPlayer.velocity = UsefulFunctions.Aim(localPlayer.Center, NPC.Center, 5);
            }

            if (!soulsSpawned)
            {
                if (authority)
                {
                    SpawnSoulsAndShield();
                }

                soulsSpawned = true;
            }

            if (NPC.life <= TransformLife && authority)
            {
                rotationDistance = 1;
                transforming = true;
                RequestNetworkSnapshot();
            }
        }

        private int TicksUntil(int interval, int offset)
        {
            int remainder = (vulnerableTimer - offset) % interval;
            if (remainder < 0)
            {
                remainder += interval;
            }

            return remainder;
        }

        // Shows the next attack's spawn points. vulnerableTimer counts down, so remainder 1-30 is the 30 ticks before release.
        private void TelegraphNextSpawn(bool authority)
        {
            switch (attackMode)
            {
                case 0:
                {
                    int ticks = TicksUntil(NebulaInterval, 0);
                    if (ticks >= 1 && ticks <= SpawnTellTicks)
                    {
                        SetPose(AttraidiesPose.Overhead);
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 point = NPC.Center + new Vector2(SpawnRadius, 0).RotatedBy(nebulaRotation + NebulaStep + i * MathHelper.TwoPi / 3f);
                            GatherDust(point, NebulaColor, 24f);
                        }
                    }
                    break;
                }

                case 1:
                {
                    int ticks = TicksUntil(ThunderInterval, ThunderOffset);
                    if (ticks >= 1 && ticks <= SpawnTellTicks)
                    {
                        SetPose(AttraidiesPose.Overhead);
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 point = NPC.Center + new Vector2(SpawnRadius, 0).RotatedBy(lightningRotation + ThunderStep + i * MathHelper.PiOver2);
                            GatherDust(point, ThunderColor, 24f);
                        }
                    }
                    break;
                }

                case 2:
                    TelegraphAimedSpawn(authority, DetonatorInterval, DetonatorColor);
                    break;

                case 3:
                    TelegraphAimedSpawn(authority, LanceInterval, LanceColor);
                    break;
            }
        }

        private void TelegraphAimedSpawn(bool authority, int interval, Color color)
        {
            int ticks = TicksUntil(interval, 0);
            if (ticks == SpawnTellTicks && authority)
            {
                pendingSpawnOffset = Main.rand.NextVector2CircularEdge(SpawnRadius, SpawnRadius);
                hasPendingSpawn = true;
                RequestNetworkSnapshot();
            }

            if (ticks >= 1 && ticks <= SpawnTellTicks && hasPendingSpawn)
            {
                Vector2 point = NPC.Center + pendingSpawnOffset;
                SetAimPose(point);
                GatherDust(point, color, 26f);
                GatherDust(StaffTip, color, 12f, 0.7f);
            }
        }

        // The window's opening can skip a lead tick, so an attack without a rolled point rolls one on release.
        private Vector2 TakePendingSpawnPoint()
        {
            if (!hasPendingSpawn)
            {
                pendingSpawnOffset = Main.rand.NextVector2CircularEdge(SpawnRadius, SpawnRadius);
            }

            hasPendingSpawn = false;
            return NPC.Center + pendingSpawnOffset;
        }

        private void RunAttack(bool authority)
        {
            if (!authority)
            {
                return;
            }

            switch (attackMode)
            {
                case 0:
                    if (vulnerableTimer % NebulaInterval == 0)
                    {
                        nebulaRotation += NebulaStep;
                        RequestNetworkSnapshot();
                        Vector2 spawnOffset = new Vector2(SpawnRadius, 0).RotatedBy(nebulaRotation);
                        for (int i = 0; i < 3; i++)
                        {
                            Vector2 spawnPoint = NPC.Center + spawnOffset;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPoint, UsefulFunctions.Aim(spawnPoint, NPC.Center, 6), ModContent.ProjectileType<NebulaShot>(), ProjectileDamage, 0, Main.myPlayer);
                            spawnOffset = spawnOffset.RotatedBy(MathHelper.TwoPi / 3f);
                        }
                    }
                    break;

                case 1:
                    if (vulnerableTimer % ThunderInterval == ThunderOffset)
                    {
                        lightningRotation += ThunderStep;
                        RequestNetworkSnapshot();
                        Vector2 spawnOffset = new Vector2(SpawnRadius, 0).RotatedBy(lightningRotation);
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 spawnPoint = NPC.Center + spawnOffset;
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPoint, UsefulFunctions.Aim(spawnPoint, TargetPlayer.Center, 1), ModContent.ProjectileType<MarilithLightning>(), ProjectileDamage, 0, Main.myPlayer);
                            spawnOffset = spawnOffset.RotatedBy(MathHelper.PiOver2);
                        }
                    }
                    break;

                case 2:
                    if (vulnerableTimer % DetonatorInterval == 0)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), TakePendingSpawnPoint(), Vector2.Zero, ModContent.ProjectileType<SolarDetonator>(), ProjectileDamage, 0, Main.myPlayer);
                    }
                    break;

                case 3:
                    if (vulnerableTimer % LanceInterval == 0)
                    {
                        Vector2 spawnPoint = TakePendingSpawnPoint();
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPoint, UsefulFunctions.Aim(spawnPoint, TargetPlayer.Center, 1), ModContent.ProjectileType<StardustShot>(), ProjectileDamage, 0, Main.myPlayer, NPC.target, 45);
                    }
                    break;
            }
        }

        private void RunTransformation(bool authority)
        {
            TransformProgress++;
            NPC.velocity = Vector2.Zero;
            NPC.dontTakeDamage = true;
            NPC.alpha = 0;
            rotationDistance *= 0.97f;
            rotationSpeed *= 1.01f;
            SetPose(AttraidiesPose.Overhead);

            if (TransformProgress == 1 && authority)
            {
                for (int i = 1; i < SoulCount + 1; i++)
                {
                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<DamnedSoul>(), 0, -i, NPC.whoAmI);
                }

                SpawnFlash();
            }

            if (TransformProgress < 550 && authority)
            {
                bool slowBolt = Main.GameUpdateCount % 60 == 0 && TransformProgress > 200;
                bool offBeatBolt = Main.GameUpdateCount % 60 == 30 && TransformProgress > 350;
                bool rapidBolt = Main.GameUpdateCount % 20 == 0 && TransformProgress > 450;

                if (slowBolt || offBeatBolt)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(1, 1), ModContent.ProjectileType<MarilithLightning>(), 30, 0, Main.myPlayer);
                }

                if (rapidBolt)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(1, 1), ModContent.ProjectileType<MarilithLightning>(), 15, 0, Main.myPlayer);
                }
            }

            int soulType = ModContent.NPCType<DamnedSoul>();
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC soul = Main.npc[i];
                if (soul.active && soul.type == soulType)
                {
                    float orbitRadius = 40 + 460 * rotationDistance * (float)Math.Sin(TransformProgress / 500f);
                    Vector2 orbitOffset = new Vector2(orbitRadius, 0).RotatedBy(MathHelper.TwoPi * soul.ai[0] / SoulCount).RotatedBy(200 * rotationSpeed);
                    soul.position = NPC.Center + orbitOffset - new Vector2(30, 0);
                }
            }

            for (int i = 0; i < 4f * TransformProgress / TransformTicks; i++)
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, default, 4f);
                Main.dust[dust].noGravity = true;
            }

            if (TransformProgress == TransformTicks + 1)
            {
                for (int i = 0; i < 100; i++)
                {
                    int dustType = 54;
                    if (i % 2 == 1)
                    {
                        dustType = 62;
                    }

                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, dustType, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, default, 4f);
                    Main.dust[dust].noGravity = true;
                }

                DeactivateAll(soulType);
                SpawnNextForm<DarkDragonMask>(removeSelf: true);
            }
        }

        public override bool CheckDead()
        {
            NPC.life = TransformLife;
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.GreaterHealingPotion;
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!ShieldBroken)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                if (shieldEffect == null || shieldEffect.IsDisposed)
                {
                    shieldEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/SimpleRing", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }

                shieldRotation += 0.01f;
                Rectangle ringRectangle = new Rectangle(0, 0, 500, 500);
                Vector2 ringOrigin = ringRectangle.Size() / 2f;

                float shaderAngle = MathHelper.Pi / 3f;
                float shaderRotation = 0;
                shieldEffect.Parameters["textureToSizeRatio"].SetValue(tsorcRevamp.NoiseWavy.Size() / ringRectangle.Size());
                shieldEffect.Parameters["shaderColor"].SetValue(Color.Purple.ToVector4());
                shieldEffect.Parameters["splitAngle"].SetValue(shaderAngle);
                shieldEffect.Parameters["rotation"].SetValue(shaderRotation);
                shieldEffect.Parameters["length"].SetValue(.1f);
                shieldEffect.Parameters["firstEdge"].SetValue(.15f);
                shieldEffect.Parameters["secondEdge"].SetValue(.115f);

                //Precomputed
                shieldEffect.Parameters["rotationMinusPI"].SetValue(shaderRotation - MathHelper.Pi);
                shieldEffect.Parameters["splitAnglePlusRotationMinusPI"].SetValue(shaderRotation + shaderAngle - MathHelper.Pi);
                shieldEffect.Parameters["RotationMinus2PIMinusSplitAngleMinusPI"].SetValue((shaderRotation - (MathHelper.TwoPi - shaderAngle)) - MathHelper.Pi);
                shieldEffect.CurrentTechnique.Passes[0].Apply();

                for (int i = 0; i < SoulCount; i++)
                {
                    Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, ringRectangle, Color.White, MathHelper.PiOver2 + shieldRotation + i * MathHelper.TwoPi / SoulCount, ringOrigin, 1, SpriteEffects.None);
                }
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            inverseAuraData ??= new ArmorShaderData(ModContent.Request<Effect>("tsorcRevamp/Effects/AttraidiesInverseAura", ReLogic.Content.AssetRequestMode.ImmediateLoad), "AttraidiesInverseAuraPass");

            // The flow direction travels through the color and opacity parameters, because there is no direction one.
            inverseAuraData.UseColor(NPC.Center.X, NPC.Center.Y, 0f);
            inverseAuraData.UseOpacity(0f);

            float saturation = 1;
            if (transforming)
            {
                saturation = 1 - TransformProgress / TransformTicks;
            }

            inverseAuraData.UseSaturation(saturation);
            inverseAuraData.Apply(null);

            Rectangle noiseSize = new Rectangle(0, 0, tsorcRevamp.NoiseTurbulent.Width, tsorcRevamp.NoiseTurbulent.Height);
            float noiseScale = 20;
            Vector2 auraCorner = NPC.Center - Main.screenPosition - new Vector2(noiseSize.Width, noiseSize.Height) / 2 * noiseScale;
            Main.spriteBatch.Draw(tsorcRevamp.NoiseTurbulent, auraCorner, noiseSize, Color.White, 0, Vector2.Zero, noiseScale, SpriteEffects.None, 0);

            RestoreNpcBatch();
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((short)vulnerableTimer);
            writer.Write((byte)attackMode);
            writer.Write((short)transitionTimer);
            writer.Write(transforming);
            writer.Write(soulsSpawned);
            writer.Write(rotationSpeed);
            writer.Write(rotationDistance);
            writer.Write(nebulaRotation);
            writer.Write(lightningRotation);
            writer.Write(hasPendingSpawn);
            writer.WriteVector2(pendingSpawnOffset);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            vulnerableTimer = reader.ReadInt16();
            attackMode = reader.ReadByte();
            transitionTimer = reader.ReadInt16();
            transforming = reader.ReadBoolean();
            soulsSpawned = reader.ReadBoolean();
            rotationSpeed = reader.ReadSingle();
            rotationDistance = reader.ReadSingle();
            nebulaRotation = reader.ReadSingle();
            lightningRotation = reader.ReadSingle();
            hasPendingSpawn = reader.ReadBoolean();
            pendingSpawnOffset = reader.ReadVector2();
        }
    }
}
