using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Okiku;

namespace tsorcRevamp.NPCs.Bosses.Okiku.FinalForm
{
    class AttraidiesFragment : ModNPC
    {

        int lookMode = 0; //0 = Stand, 1 = Player's Direction, 2 = Movement Direction.
        int attackPhase = -1;
        int subPhase = 0;
        int genericTimer = 0;
        int genericTimer2 = 0;
        int phaseTime = 400;
        bool phaseStarted = false;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.width = 58;
            NPC.height = 121;
            NPC.defense = 25;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 1500;
            NPC.timeLeft = 9999999;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0;
            NPC.lavaImmune = true;
            NPC.value = 3500;
        }


        NPC HostNPC
        {
            get => Main.npc[(int)NPC.ai[0]];
        }

        int AttackTimer
        {
            get => (int)NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        int clockwise = 1;
        public int NebulaShotDamage = 45;
        public int PoisonTrailDamage = 45;
        float currentRotation = 0;
        public override void AI()
        {
            NPC.damage = 0;
            int timeLimit = 900;
            if (NPC.ai[3] != 0)
            {
                timeLimit = 1200;
                if (NPC.ai[3] == 3)
                {
                    timeLimit = 99999999;
                }
            }


            if (HostNPC.active)
            {
                currentRotation += 0.006f;
                float radius = 700;
                float offset = 0;
                if (AttackTimer < 60)
                {
                    radius *= AttackTimer / 60f;
                    offset = 1 - AttackTimer / 60f;
                }
                if (AttackTimer > timeLimit - 60)
                {
                    radius *= (AttackTimer - timeLimit) / 60f;
                    offset = 1 - (AttackTimer - timeLimit) / 60f;
                }

                NPC.Center = HostNPC.Center + new Vector2(0, -radius).RotatedBy(offset - 0.15f + currentRotation + NPC.ai[1] * MathHelper.TwoPi / 5f);
                NPC.realLife = HostNPC.whoAmI;
                NPC.lifeMax = HostNPC.lifeMax;
                NPC.life = HostNPC.life;
            }
            else
            {
                NPC.active = false;
            }

            AttackTimer++;

            if (NPC.ai[3] == 3)
            {
                if (AttackTimer % 720 == 690)
                {
                    animationState = 1;
                    animationTimer = 30;
                }
                if (AttackTimer % 720 == 0)
                {

                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / 4f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<NebulaShot>(), NebulaShotDamage, 1, Main.myPlayer, clockwise);
                        }
                        clockwise *= -1;
                    }
                }

            }
            else
            {
                if (NPC.ai[3] == 0 && AttackTimer % 120 == 90)
                {
                    animationState = 1;
                    animationTimer = 30;
                }
                if (NPC.ai[3] == 0 && AttackTimer % 120 == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 1 }, NPC.Center);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 projVel = new Vector2(5, 0).RotatedBy(i * 2f * MathHelper.Pi / 4f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, projVel, ModContent.ProjectileType<NebulaShot>(), NebulaShotDamage, 1, Main.myPlayer, clockwise);
                        }
                    }
                }


                if (AttackTimer > timeLimit)
                {
                    NPC.active = false;
                }
            }
        }
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
        }

        int animationState;
        int animationTimer;
        public override void FindFrame(int currentFrame)
        {
            if (texture == null || texture.IsDisposed)
            {
                return;
            }
            int frameSize = texture.Height / Main.npcFrameCount[NPC.type];

            //Standing there, he realizes
            if (animationState == 0)
            {
                NPC.frame = new Rectangle(0, 0, texture.Width, frameSize);
            }

            //Spreading arms out dramatically
            if (animationState == 1)
            {
                NPC.frame = new Rectangle(0, frameSize, texture.Width, frameSize);

                if (animationTimer == 0)
                {
                    animationState = 0;
                }
                else
                {
                    animationTimer--;
                }
            }
        }

        float effectTimer;
        public static Effect effect;
        public static Texture2D texture;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight((int)NPC.Center.X / 16, (int)NPC.Center.Y / 16, 1f, 0.4f, 0.4f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            if (effect == null)
            {
                effect = ModContent.Request<Effect>("tsorcRevamp/Effects/AttraidiesAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Vector3 hslColor = Main.rgbToHsl(Color.Purple * 3);
            hslColor.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color rgbColor = Main.hslToRgb(hslColor);


            Rectangle baseRectangle = new Rectangle(0, 0, 350, 350);
            Vector2 baseOrigin = baseRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            effect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseTurbulent.Width);
            effect.Parameters["effectSize"].SetValue(baseRectangle.Size());
            effect.Parameters["effectColor1"].SetValue(rgbColor.ToVector4());
            effect.Parameters["effectColor2"].SetValue(rgbColor.ToVector4());
            effect.Parameters["ringProgress"].SetValue(0);
            effect.Parameters["fadePercent"].SetValue(0f);
            effect.Parameters["scaleFactor"].SetValue(.5f * 50);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 0.05f * 0.5f);
            effect.Parameters["colorSplitAngle"].SetValue(MathHelper.TwoPi);

            //Apply the shader
            effect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, NPC.Center - Main.screenPosition, baseRectangle, Color.White, 0, baseOrigin, NPC.scale, SpriteEffects.None, 0);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);



            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(NPC.ModNPC.Texture);
            }
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (NPC.Center.X - 20 < Main.LocalPlayer.Center.X)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            Color lightingColor = Color.Lerp(Color.White, rgbColor, 0.5f);
            lightingColor = Color.Lerp(drawColor, lightingColor, 0.5f);
            Rectangle sourceRectangle2 = NPC.frame;
            Vector2 origin2 = sourceRectangle2.Size() / 2f;
            spriteBatch.Draw(texture, NPC.Center - Main.screenPosition, sourceRectangle2, lightingColor, NPC.rotation, origin2, 1, spriteEffects, 0f);

            return false;
        }

        public override bool CheckDead()
        {
            if (HostNPC.active)
            {
                NPC.life = 1;
                return false;
            }
            else
            {
                return true;
            }
        }
        public override bool CheckActive()
        {
            return false;
        }

        public override void OnKill()
        {
            for (int i = 0; i < 50; i++)
            {
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
            }
        }
    }
}
