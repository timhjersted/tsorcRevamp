using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.BigProgressBar;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Marilith;
using tsorcRevamp.Projectiles.Enemy.Okiku;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Utilities;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace tsorcRevamp.NPCs.Bosses.Okiku.FirstForm
{
    [AutoloadBossHead]
    public class DarkShogunMask : ModNPC
    {
        private bool initiate;

        public float RotSpeed;

        public bool RotDir;

        public bool DamnedSoulsSpawned;

        //Upon access, check if any Damned Souls are in immune phase
        public bool ShieldBroken
        {
            get 
            {
                if (!NPC.AnyNPCs(ModContent.NPCType<AttraidiesShield>()))
                {
                    return true;
                }

                return false;
            }
        }

        private bool _transforming;
        public bool Transforming
        {
            get => _transforming;
            set
            {
                _transforming = value;
                NPC.netUpdate = true;
            }
        }

        public float TransformProgress
        {
            get => NPC.ai[2];
            set => NPC.ai[2] = value;
        }

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
            NPC.lifeMax = (int)(75000 * (Main.masterMode ? 1.5f : 1));
            NPC.boss = true;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.value = 50000;
            NPC.rarity = 29;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.DarkShogunMask.DespawnHandler"), Color.DarkMagenta, 54);
            IBigProgressBar bar;
            Main.BigBossProgressBar.TryGetSpecialVanillaBossBar(NPCID.LunarTowerNebula, out bar);
            //NPC.BossBar = bar;
        }

        float RotDistance = 1;
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
        }
        
        NPCDespawnHandler despawnHandler;
        int vulnerableTimer = 0;
        int attackMode = 0;
        int transitionTimer;
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            Main.dayTime = false;
            Main.time = Main.nightLength / 2;

            if (!initiate)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.DarkShogunMask.Quote"), new Color(175, 75, 255));
                RotSpeed = 0.015f;
                initiate = true;
            }

            if(transitionTimer > 0)
            {
                transitionTimer--;
                NPC.velocity *= 0.95f;
            }
                        

            if (!Transforming)
            {
                float speed = 0.5f;
                if (!ShieldBroken)
                {
                    NPC.dontTakeDamage = true;
                    NPC.alpha = 180;
                    if (shieldRadius > 100)
                    {
                        shieldRadius = 100 + (float)Math.Pow(shieldRadius - 100, .8f);
                    }
                }
                else
                {                    
                    //Initialize vulnerable timer to 15s
                    if(vulnerableTimer <= 0)
                    {
                        vulnerableTimer = 1101;
                        transitionTimer = 200;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 550, 20);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 520, 60);
                        }
                    }

                    //On the final frame of vulnerability, increment attackMode and respawn shield+souls
                    if (vulnerableTimer > 0)
                    {
                        vulnerableTimer--;
                        if (vulnerableTimer == 0)
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                int shield = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AttraidiesShield>(), 0, 0, NPC.whoAmI);

                                for (int i = 0; i < 6; i++)
                                {
                                    NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<DamnedSoul>(), 0, i, NPC.whoAmI, shield);
                                }
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 550, 20);
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 520, 60);
                            }
                            attackMode++;
                            if (attackMode > 3)
                            {
                                attackMode = 0;
                            }
                        }
                    }

                    if (shieldRadius < 500)
                    {
                        shieldRadius = 500 - (float)Math.Pow(500 - shieldRadius, .8f);
                    }

                    if (RotSpeed < 0.02f) RotSpeed += 0.00005f;
                    if (transitionTimer <= 0)
                    {
                        RunAttack();
                    }
                    speed = 2f;
                    NPC.dontTakeDamage = false;
                    NPC.alpha = 0;
                }

                if (Main.player[NPC.target].Distance(NPC.Center) > 250)
                {
                    NPC.velocity = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, speed);
                }
                else
                {
                    NPC.velocity *= 0.98f;
                }

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
                                diff = diff.SafeNormalize(Vector2.Zero);
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

                if (DamnedSoulsSpawned == false)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        int shield = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<AttraidiesShield>(), 0, 0, NPC.whoAmI);

                        for (int i = 0; i < 6; i++)
                        {
                            NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<DamnedSoul>(), 0, i, NPC.whoAmI, shield);
                        }
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 550, 20);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 520, 60);
                    }
                    DamnedSoulsSpawned = true;
                }

                //Initiate Transformation
                if (NPC.life <= 1000)
                {
                    RotDistance = 1;
                    Transforming = true;
                }
            }
            else
            {
                TransformProgress++;
                NPC.velocity.X = 0;
                NPC.velocity.Y = 0;
                NPC.dontTakeDamage = true;
                RotDistance *= 0.97f;
                RotSpeed *= 1.01f;

                if (TransformProgress == 1)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 1; i < 7; i++)
                        {
                            NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, ModContent.NPCType<DamnedSoul>(), 0, -i, NPC.whoAmI);
                        }
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<ExplosionFlash>(), 0, 0, Main.myPlayer, 550, 20);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 520, 60);
                    }
                }

                if (TransformProgress < 550)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (Main.GameUpdateCount % 60 == 0 && TransformProgress > 200)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(1, 1), ModContent.ProjectileType<MarilithLightning>(), 30, 0, Main.myPlayer);
                        }
                        if (Main.GameUpdateCount % 60 == 30 && TransformProgress > 350)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(1, 1), ModContent.ProjectileType<MarilithLightning>(), 30, 0, Main.myPlayer);
                        }
                        if (Main.GameUpdateCount % 20 == 0 && TransformProgress > 450)
                        {
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Main.rand.NextVector2CircularEdge(1, 1), ModContent.ProjectileType<MarilithLightning>(), 15, 0, Main.myPlayer);
                        }
                    }
                }

                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<DamnedSoul>())
                    {
                        Main.npc[i].position = NPC.Center + new Vector2(40 + 460 * RotDistance * (float)Math.Sin(TransformProgress / 500f), 0).RotatedBy(MathHelper.TwoPi * Main.npc[i].ai[0] / 6).RotatedBy(200 * RotSpeed) - new Vector2(30, 0);
                    }
                }


                for (int i = 0; i < 4f * TransformProgress / 600f; i++)
                {
                    int dust = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 54, Main.rand.Next(-20, 20) * 2, Main.rand.Next(-20, 20) * 2, 100, default, 4f);
                    Main.dust[dust].noGravity = true;
                }

                if (TransformProgress > 600)
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

        void RunAttack()
        {
            switch (attackMode)
            {
                case 0:
                    {
                        NebulaSpiral();
                        break;
                    }
                case 1:
                    {
                        ThunderRain();
                        break;
                    }
                case 2:
                    {
                        DetonatorRing();
                        break;
                    }
                case 3:
                    {
                        StardustLance();
                        break;
                    }
            }
        }

        float detRotation;
        void DetonatorRing()
        {
            if (vulnerableTimer % 200 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnVec = NPC.Center + Main.rand.NextVector2CircularEdge(500, 500);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnVec, Vector2.Zero, ModContent.ProjectileType<SolarDetonator>(), 30, 0, Main.myPlayer);
                }
            }
        }

        float lightningRotation;
        void ThunderRain()
        {
            if (vulnerableTimer % 200 == 100)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    lightningRotation += MathHelper.PiOver2 / 3f;
                    Vector2 spawnOffset = new Vector2(500, 0).RotatedBy(lightningRotation);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnOffset, UsefulFunctions.Aim(NPC.Center + spawnOffset, Main.player[NPC.target].Center, 1), ModContent.ProjectileType<MarilithLightning>(), 30, 0, Main.myPlayer);
                    spawnOffset = spawnOffset.RotatedBy(MathHelper.PiOver2);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnOffset, UsefulFunctions.Aim(NPC.Center + spawnOffset, Main.player[NPC.target].Center, 1), ModContent.ProjectileType<MarilithLightning>(), 30, 0, Main.myPlayer);
                    spawnOffset = spawnOffset.RotatedBy(MathHelper.PiOver2);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnOffset, UsefulFunctions.Aim(NPC.Center + spawnOffset, Main.player[NPC.target].Center, 1), ModContent.ProjectileType<MarilithLightning>(), 30, 0, Main.myPlayer);
                    spawnOffset = spawnOffset.RotatedBy(MathHelper.PiOver2);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnOffset, UsefulFunctions.Aim(NPC.Center + spawnOffset, Main.player[NPC.target].Center, 1), ModContent.ProjectileType<MarilithLightning>(), 30, 0, Main.myPlayer);
                }
            }
        }

        float nebRotation = 0;
        void NebulaSpiral()
        {
            if (vulnerableTimer % 90 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    nebRotation += 0.25f;
                    Vector2 spawnOffset = new Vector2(500, 0).RotatedBy(nebRotation);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnOffset, UsefulFunctions.Aim(NPC.Center + spawnOffset, NPC.Center, 6), ModContent.ProjectileType<NebulaShot>(), 30, 0, Main.myPlayer);
                    spawnOffset = spawnOffset.RotatedBy(MathHelper.TwoPi / 3f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnOffset, UsefulFunctions.Aim(NPC.Center + spawnOffset, NPC.Center, 6), ModContent.ProjectileType<NebulaShot>(), 30, 0, Main.myPlayer);
                    spawnOffset = spawnOffset.RotatedBy(MathHelper.TwoPi / 3f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + spawnOffset, UsefulFunctions.Aim(NPC.Center + spawnOffset, NPC.Center, 6), ModContent.ProjectileType<NebulaShot>(), 30, 0, Main.myPlayer);
                }
            }
        }
        void StardustLance()
        {
            if (vulnerableTimer % 80 == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 spawnVec = NPC.Center + Main.rand.NextVector2CircularEdge(500, 500);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnVec, UsefulFunctions.Aim(spawnVec, Main.player[NPC.target].Center, 1), ModContent.ProjectileType<StardustShot>(), 30, 0, Main.myPlayer, NPC.target, 45);
                }
            }
        }


        public override bool CheckDead()
        {
            NPC.life = 1000;
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

        float shieldRadius;
        float shieldRotation;
        public static Effect shieldEffect;
        public static Effect ringEffect;
        ArmorShaderData data;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!ShieldBroken)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                if (shieldEffect == null)
                {
                    shieldEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/SimpleRing", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }

                shieldRotation += 0.01f;
                int partCount = 6;

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

                for (int i = 0; i < partCount; i++)
                {
                    Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition, ringRectangle, Color.White, MathHelper.PiOver2 + shieldRotation + i * MathHelper.TwoPi / 6f, ringOrigin, 1, SpriteEffects.None);
                }
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            //if (data == null)
            {
                data = new ArmorShaderData(ModContent.Request<Effect>("tsorcRevamp/Effects/AttraidiesInverseAura", ReLogic.Content.AssetRequestMode.ImmediateLoad), "AttraidiesInverseAuraPass");
            }

            //data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.AcidDye), Main.LocalPlayer);
            Vector2 offset = Vector2.Zero;
            //Pass the flow directions parameter in through the "color" and "targetposition" variables, because there isn't a "direction" one
            data.UseColor(NPC.Center.X, NPC.Center.Y, offset.X);
            //data.UseTargetPosition(offset);
            data.UseOpacity(offset.Y);
            offset -= new Vector2(10.3f * Main.GameUpdateCount, 0);
            
            
            //offset += new Vector2(-3f, 1.2f) / 300f;
            float saturation = 1;
            if(Transforming)
            {
                saturation = 1 - TransformProgress / 600f;
            }
            data.UseSaturation(saturation);
            //Apply the shader
            data.Apply(null);



            Rectangle recsize = new Rectangle(0, 0, tsorcRevamp.NoiseTurbulent.Width, tsorcRevamp.NoiseTurbulent.Height);

            float scaler = 20;

            //Draw the rendertarget with the shader
            Main.spriteBatch.Draw(tsorcRevamp.NoiseTurbulent, NPC.Center - Main.screenPosition - new Vector2(recsize.Width, recsize.Height) / 2 * scaler, recsize, Color.White, 0, Vector2.Zero, scaler, SpriteEffects.None, 0);

            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);


            return true;
        }
    }
}
