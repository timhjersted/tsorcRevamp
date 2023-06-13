using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Enemy.DarkCloud;
using tsorcRevamp.Buffs.Debuffs;
using Terraria.GameContent.ItemDropRules;
using System;

namespace tsorcRevamp.NPCs.Bosses.PrimeV2
{
    [AutoloadBossHead]
    class PrimeWelder : ModNPC
    {
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 2;
            NPCID.Sets.TrailCacheLength[NPC.type] = (int)TRAIL_LENGTH;    //The length of old position to be recorded
            NPCID.Sets.TrailingMode[NPC.type] = 1;
            NPCID.Sets.NPCBestiaryDrawModifiers value = new NPCID.Sets.NPCBestiaryDrawModifiers(0) {
                Hide = true
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(NPC.type, value);
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            NPC.width = 30;
            NPC.height = 100;
            NPC.damage = 53;
            NPC.defense = 20;
            NPC.lifeMax = PrimeV2.PrimeArmHealth;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = 0;
            NPC.knockBackResist = 0f;
            NPC.timeLeft = 99999;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.damage = 0;
        }
        const float TRAIL_LENGTH = 12;

        public int AttackTimer
        {
            get => (int)NPC.ai[0];
            set => NPC.ai[0] = value;
        }

        NPC primeHost
        {
            get => Main.npc[(int)NPC.ai[1]];
        }
        public Player Target
        {
            get => Main.player[primeHost.target];
        }

        bool active
        {
            get => primeHost != null && ((PrimeV2)primeHost.ModNPC).MoveIndex == 5;
        }

        int phase;
        bool damaged;
        float angle;
        public Vector2 Offset = new Vector2(-810, 250);
        public override void AI()
        {
            int WeldDamage = 100;

            if (primeHost == null || primeHost.active == false || primeHost.type != ModContent.NPCType<PrimeV2>())
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 10, 0, Main.myPlayer, 500, 60);
                }
                NPC.active = false;
                return;
            }

            if (((PrimeV2)primeHost.ModNPC).aiPaused)
            {
                UsefulFunctions.SmoothHoming(NPC, primeHost.Center + Offset, 0.2f, 50, primeHost.velocity);
                NPC.rotation = MathHelper.PiOver2;
                return;

            }
            if (((PrimeV2)primeHost.ModNPC).Phase == 1)
            {
                Offset = new Vector2(1200, 0);
            }

            NPC.rotation = (Target.Center - NPC.Center).ToRotation() - MathHelper.PiOver2;
            if (!UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.MoltenWeld>()))
            {
                if(Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.Prime.MoltenWeld>(), WeldDamage / 4, 0.5f, Main.myPlayer, ai1: NPC.whoAmI);
                }
            }

            if (active)
            {
                UsefulFunctions.SmoothHoming(NPC, Target.Center, 0.2f, 7f, bufferZone: false);
            }
            else
            {
                angle += 0.01f;
                UsefulFunctions.SmoothHoming(NPC, Target.Center + new Vector2(400, 0).RotatedBy(angle), 0.3f, 5f);
                Dust.NewDustPerfect(Target.Center + new Vector2(400, 0).RotatedBy(angle), DustID.ShadowbeamStaff, Main.rand.NextVector2Circular(10, 10));
            }
        }


        public override bool CheckDead()
        {
            if (((PrimeV2)primeHost.ModNPC).dying)
            {
                return true;
            }
            else
            {
                NPC.life = 1;
                damaged = true;
                NPC.dontTakeDamage = true;
                return false;
            }
        }
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            PrimeV2.PrimeProjectileBalancing(ref projectile);
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            PrimeV2.PrimeDamageShare(NPC.whoAmI, damageDone);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            PrimeV2.PrimeDamageShare(NPC.whoAmI, damageDone);
        }

        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight(NPC.Center, TorchID.White);

            UsefulFunctions.EnsureLoaded(ref texture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeWelder");

            
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 drawOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(texture, NPC.Center - Main.screenPosition, sourceRectangle, drawColor, 0, drawOrigin, 1f, SpriteEffects.None, 0);
            DrawSpark();
            //Draw metal bones
            //Draw shadow trail (and maybe normal trail?)
            if (active)
            {
                //Draw aura
            }
            if (damaged)
            {
                //Draw damaged version
            }
            else
            {
                //Draw normal version
            }
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
            //if (effect == null)
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
            CoreEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTexture3.Width);
            CoreEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            CoreEffect.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            CoreEffect.Parameters["ringProgress"].SetValue(0.5f);
            CoreEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            CoreEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            CoreEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTexture3, NPC.Center - Main.screenPosition + new Vector2(0, 42), starRectangle, Color.White, starRotation, starOrigin, 1, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Pass relevant data to the shader via these parameters
            CoreEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTexture3.Width);
            CoreEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            CoreEffect.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            CoreEffect.Parameters["ringProgress"].SetValue(0.5f);
            CoreEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            CoreEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            CoreEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTexture3, NPC.Center - Main.screenPosition + new Vector2(0, 42), starRectangle, Color.White, -starRotation, starOrigin, 1, SpriteEffects.None, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        public override void OnKill()
        {
            //Explosion
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}