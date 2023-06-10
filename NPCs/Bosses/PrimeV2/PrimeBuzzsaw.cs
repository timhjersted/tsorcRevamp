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
using Terraria.GameContent;
using Terraria.Graphics.Shaders;

namespace tsorcRevamp.NPCs.Bosses.PrimeV2
{
    [AutoloadBossHead]
    class PrimeBuzzsaw : ModNPC
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
            NPC.width = 38;
            NPC.height = 100;
            NPC.damage = 53;
            NPC.defense = 20;
            NPC.lifeMax = 7500;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath14;
            NPC.value = 0;
            NPC.knockBackResist = 0f;
            NPC.timeLeft = 99999;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
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
        PrimeV2 Prime
        {
            get => ((PrimeV2)primeHost.ModNPC);
        }

        public Player Target
        {
            get => Main.player[primeHost.target];
        }

        bool active
        {
            get => ((PrimeV2)primeHost.ModNPC).MoveIndex == 2;
        }
        int phase
        {
            get => ((PrimeV2)primeHost.ModNPC).Phase;
        }

        bool damaged;

        bool movingLeft;
        bool seekingPlayer;
        Vector2 drawOffset = Vector2.Zero;

        public Vector2 Offset = new Vector2(600, 200);
        public override void AI()
        {
            NPC.height = 120;
            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 1.5f);
            int SawDamage = 60;
            NPC.rotation = 0;
            if (Prime.aiPaused)
            {
                UsefulFunctions.SmoothHoming(NPC, primeHost.Center + Offset, 0.2f, 50, primeHost.velocity);
                NPC.rotation = MathHelper.PiOver2;
                return;
            }

            if (active)
            {
                NPC.velocity *= 0.95f;
                drawOffset = Main.rand.NextVector2CircularEdge(1, 1);

                if (Prime.MoveTimer == 160)
                {
                    if (!damaged)
                    {
                        //Fire a bouncing saw
                        if (!UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>()))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 25f), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>(), SawDamage, 0.5f, Main.myPlayer, ai1: NPC.whoAmI);
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
                        }
                    }
                    else
                    {
                        //Fire a damaged slower bouncing saw that spawns shards of metal on impacts
                        if (!UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>()))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.Aim(NPC.Center, Target.Center, 15f), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>(), SawDamage, 0.5f, Main.myPlayer, 1, NPC.whoAmI);
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
                        }
                    }
                }
            }
            else
            {
                //Simply sweep left and right, stopping to realign with the player after each pass
                if(Math.Abs(NPC.Center.X - Target.Center.X) > 700)
                {
                    seekingPlayer = true;
                }

                if (seekingPlayer)
                {
                    NPC.velocity.Y = 0;
                    NPC.Center = new Vector2(NPC.Center.X, MathHelper.Lerp(NPC.Center.Y, Target.Center.Y, 0.05f));
                    if(Math.Abs(NPC.Center.Y - Target.Center.Y) < 10)
                    {
                        seekingPlayer = false;
                        movingLeft = NPC.Center.X > Target.Center.X;
                    }
                }
                

                if(!seekingPlayer)
                {
                    NPC.velocity.X = 7;

                    if (movingLeft)
                    {
                        NPC.velocity.X = -7;
                    }

                    if (damaged)
                    {
                        NPC.velocity.Y = 12 * (float)Math.Sin(Main.GameUpdateCount / 20f);
                    }
                }
            }
        }
        public override bool CheckDead()
        {
            if (((PrimeV2)primeHost.ModNPC).Phase == 1)
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

        public static Texture2D holderTexture;
        public static Texture2D sawTexture;
        public static ArmorShaderData data;
        public static ArmorShaderData data2;
        int sawFrameCounter;
        int sawFrame;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            Lighting.AddLight(NPC.Center, TorchID.Bone);

            UsefulFunctions.EnsureLoaded(ref sawTexture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeSawBlade");
            UsefulFunctions.EnsureLoaded(ref holderTexture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeBuzzsaw");

            if (active)
            {
                //Draw aura
            }
            
            //Draw metal bones


            if (!UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>()))
            {
                sawFrameCounter++;
                if (sawFrameCounter > 3)
                {
                    sawFrame++;
                    if (sawFrame >= 6)
                    {
                        sawFrame = 0;
                    }
                }


                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                if (data == null)
                {
                    data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.SolarDye), Main.LocalPlayer);
                }
                if (data2 == null)
                {
                    data2 = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveMetalDye), Main.LocalPlayer);
                }

                Color sawColor = Color.White;
                if (active)
                {
                    if (Prime.MoveTimer < 160)
                    {
                        sawColor = Color.Lerp(Color.White, Color.OrangeRed, Prime.MoveTimer / 160f);

                        data.UseColor(Color.Lerp(Color.Black, Color.OrangeRed, Prime.MoveTimer / 160f));
                        data.Apply(null);
                    }
                }

                int frameHeight = sawTexture.Height / 8;
                int startY = frameHeight * sawFrame;
                Rectangle sawSourceRectangle = new Rectangle(0, startY, sawTexture.Width, frameHeight);
                Vector2 sawDrawOrigin = new Vector2(sawSourceRectangle.Width * 0.5f, sawSourceRectangle.Height * 0.5f);
                Main.EntitySpriteDraw(sawTexture, NPC.Center - Main.screenPosition + new Vector2(0, 44) - new Vector2(0, 16), sawSourceRectangle, sawColor, 0, sawDrawOrigin, 1.5f, SpriteEffects.None, 0);

                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
            }


            //Draw shadow trail (and maybe normal trail?)
            
            if (damaged)
            {
                //Draw damaged version
            }
            //else
            {
                //Draw normal version
                Rectangle sourceRectangle = new Rectangle(0, 0, holderTexture.Width, holderTexture.Height);
                Vector2 drawOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
                Main.EntitySpriteDraw(holderTexture, NPC.Center - Main.screenPosition - new Vector2(0, 16), sourceRectangle, Color.Gray, 0, drawOrigin, 1f, SpriteEffects.None, 0);
            }

            return false;
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