using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.PrimeV2
{
    class PrimeWelder : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 2;
            NPCID.Sets.TrailCacheLength[NPC.type] = (int)TRAIL_LENGTH;    //The length of old position to be recorded
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers()
            {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            NPC.width = 80;
            NPC.height = 150;
            NPC.damage = 0;
            NPC.defense = 20;
            NPC.lifeMax = (int)(TheMachine.PrimeArmHealth * (Main.masterMode ? 1.5f : 1));
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = 0;
            NPC.knockBackResist = 0f;
            NPC.timeLeft = 99999;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
        }
        const float TRAIL_LENGTH = 12;

        public float AttackTimer
        {
            get => NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        NPC primeHost
        {
            get
            {
                if (Main.npc[(int)NPC.ai[1]].active && Main.npc[(int)NPC.ai[1]].type == ModContent.NPCType<TheMachine>())
                {
                    return Main.npc[(int)NPC.ai[1]];
                }
                else
                {
                    return null;
                }
            }
        }
        public Player Target
        {
            get => Main.player[primeHost.target];
        }

        bool active
        {
            get => primeHost != null && ((TheMachine)primeHost.ModNPC).MoveIndex == 5;
        }

        bool damaged;
        float angle
        {
            get => NPC.ai[2];
            set => NPC.ai[2] = value;
        }
        public Vector2 Offset = new Vector2(-810, 250);
        public override void AI()
        {
            AttackTimer++;
            if (NPC.life == 1)
            {
                damaged = true;
            }
            int WeldDamage = 100;

            if (primeHost == null)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 10, 0, Main.myPlayer, 500, 60);
                }
                NPC.active = false;
                return;
            }

            if (((TheMachine)primeHost.ModNPC).aiPaused)
            {
                if (((TheMachine)primeHost.ModNPC).inIntro)
                {
                    Offset = new Vector2(1200, 0);

                    UsefulFunctions.SmoothHoming(NPC, primeHost.Center + Offset, 0.2f, 50, primeHost.velocity);
                    NPC.rotation = MathHelper.PiOver2;
                }

                return;
            }
            if (((TheMachine)primeHost.ModNPC).Phase == 1)
            {
                Offset = new Vector2(1200, 0);
            }

            NPC.rotation = (Target.Center - NPC.Center).ToRotation() - MathHelper.PiOver2;
            if ((!damaged && !UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.MoltenWeld>())) || (damaged && AttackTimer % 60 == 0))
            {
                float aiZero = 0;
                if (damaged)
                {
                    aiZero = 1;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Prime.MoltenWeld>(), WeldDamage / 4, 0.5f, Main.myPlayer, ai0: aiZero, ai1: NPC.whoAmI);
                }
            }


            if (damaged)
            {
                angle += 0.01f;
                if (AttackTimer % 60 == 0)
                {
                    float speed = 9;
                    Vector2 targetPoint;
                    if (active)
                    {
                        targetPoint = Target.Center;
                    }
                    else
                    {
                        speed = 7;
                        targetPoint = Target.Center + new Vector2(400, 0).RotatedBy(angle);
                    }
                    Vector2 distance = targetPoint - NPC.Center;

                    NPC.velocity = Vector2.Zero;
                    if (Math.Abs(distance.X) > Math.Abs(distance.Y))
                    {
                        if (distance.X > 0)
                        {
                            NPC.velocity.X = speed;
                        }
                        else
                        {
                            NPC.velocity.X = -speed;
                        }
                    }
                    else
                    {
                        if (distance.Y > 0)
                        {
                            NPC.velocity.Y = speed;
                        }
                        else
                        {
                            NPC.velocity.Y = -speed;
                        }
                    }
                }
            }
            else
            {
                if (active)
                {
                    UsefulFunctions.SmoothHoming(NPC, Target.Center, 0.1f, 7f, bufferZone: false);
                }
                else
                {
                    angle += 0.01f;
                    UsefulFunctions.SmoothHoming(NPC, Target.Center + new Vector2(400, 0).RotatedBy(angle), 0.3f, 5f);
                }
            }
        }


        public override bool CheckDead()
        {
            if (((TheMachine)primeHost.ModNPC).dying)
            {
                return true;
            }
            else
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0f, Main.myPlayer, 300, 25);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0f, Main.myPlayer, 300, 25);
                }
                UsefulFunctions.SimpleGore(NPC, "Welder_Damaged_2");
                NPC.life = 1;
                damaged = true;
                NPC.dontTakeDamage = true;
                return false;
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            TheMachine.PrimeProjectileBalancing(ref projectile);
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            TheMachine.PrimeDamageShare(NPC.whoAmI, damageDone);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            TheMachine.PrimeDamageShare(NPC.whoAmI, damageDone);
        }

        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight(NPC.Center, TorchID.White);
            UsefulFunctions.EnsureLoaded(ref texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeWelder");

            TheMachine.DrawMachineAura(Color.DarkOrange, active, NPC);

            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height / 2);
            Vector2 drawOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
            if (damaged)
            {
                sourceRectangle.Y += texture.Height / 2;
            }
            Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, 0, drawOrigin, 1f, SpriteEffects.None, 0);

            DrawSpark();
            return false;
        }


        public static Effect CoreEffect;
        float starRotation;
        void DrawSpark()
        {
            Vector3 hslColor1 = Main.rgbToHsl(Color.White);
            Vector3 hslColor2 = Main.rgbToHsl(Color.OrangeRed);
            hslColor1.X += 0.03f * (float)Math.Cos(Main.timeForVisualEffects / 25f);
            hslColor2.X += 0.01f * (float)Math.Cos(Main.timeForVisualEffects / 25f);
            Color rgbColor1 = Main.hslToRgb(hslColor1);
            Color rgbColor2 = Main.hslToRgb(hslColor2);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            if (CoreEffect == null)
            {
                CoreEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatFinalStandAttack", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            starRotation += 0.2f;
            Rectangle starRectangle = new Rectangle(0, 0, 400, 400);
            float attackFadePercent = (float)Math.Pow(0.75, .2) / 2f;
            starRectangle.Width = 14 + (int)(starRectangle.Width * (1 - Math.Pow(0.75, .3)));
            starRectangle.Height = 140 + (int)(starRectangle.Height * (1 - Math.Pow(0.75, .3)));

            Vector2 starOrigin = starRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            CoreEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            CoreEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            CoreEffect.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            CoreEffect.Parameters["ringProgress"].SetValue(0.5f);
            CoreEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            CoreEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            CoreEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition + new Vector2(0, 72), starRectangle, Color.White, starRotation, starOrigin, 1, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Pass relevant data to the shader via these parameters
            CoreEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            CoreEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            CoreEffect.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            CoreEffect.Parameters["ringProgress"].SetValue(0.5f);
            CoreEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            CoreEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            CoreEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, NPC.Center - Main.screenPosition + new Vector2(0, 72), starRectangle, Color.White, -starRotation, starOrigin, 1, SpriteEffects.None, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        public override void OnKill()
        {
            UsefulFunctions.SimpleGore(NPC, "Welder_Destroyed_1");
            UsefulFunctions.SimpleGore(NPC, "Welder_Destroyed_2");
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}