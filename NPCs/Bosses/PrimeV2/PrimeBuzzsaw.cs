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
            NPC.width = 70;
            NPC.height = 150;
            NPC.damage = 60;
            NPC.defense = 20;
            NPC.lifeMax = TheMachine.PrimeArmHealth;
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

        public float SawTimer
        {
            get => NPC.ai[2];
            set => NPC.ai[2] = value;
        }

        TheMachine Prime
        {
            get => ((TheMachine)primeHost.ModNPC);
        }

        public Player Target
        {
            get => Main.player[primeHost.target];
        }

        bool active
        {
            get => primeHost != null && ((TheMachine)primeHost.ModNPC).MoveIndex == 2;
        }

        bool damaged;

        bool movingLeft;
        bool seekingPlayer;
        Vector2 drawOffset = Vector2.Zero;

        public int SawDamage = 120;
        public Vector2 Offset = new Vector2(600, 200);
        public override void AI()
        {
            SawTimer++;
            NPC.rotation = 0;
            if(NPC.life == 1)
            {
                damaged = true;
            }
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
                    UsefulFunctions.SmoothHoming(NPC, primeHost.Center + Offset, 0.2f, 50, primeHost.velocity);
                    NPC.rotation = MathHelper.PiOver2;
                }

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
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, 62), UsefulFunctions.Aim(NPC.Center, Target.Center, 12f), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>(), SawDamage / 4, 0.5f, Main.myPlayer, ai1: NPC.whoAmI);
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
                            auraBonus = 0.2f;
                        }
                    }
                    else
                    {
                        //Fire a damaged slower bouncing saw that spawns shards of metal on impacts
                        if (!UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>()))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(0, 62), UsefulFunctions.Aim(NPC.Center, Target.Center, 11f), ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>(), SawDamage / 4, 0.5f, Main.myPlayer, 1, NPC.whoAmI);
                            }
                            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item70, NPC.Center);
                            auraBonus = 0.2f;
                        }
                    }
                }
            }
            else
            {
                drawOffset = Vector2.Zero;
                //Simply sweep left and right, stopping to realign with the player after each pass
                if(Math.Abs(NPC.Center.X - Target.Center.X) > 800)
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
                    NPC.velocity.X = 5;

                    if (movingLeft)
                    {
                        NPC.velocity.X = -5;
                    }

                    if (damaged)
                    {
                        NPC.velocity.Y = 5 * (float)Math.Sin(SawTimer / 20f);
                    }
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
                UsefulFunctions.SimpleGore(NPC, "Buzzsaw_Damaged_1");
                UsefulFunctions.SimpleGore(NPC, "Buzzsaw_Damaged_2");
                NPC.life = 1;
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

        public static Texture2D holderTexture;
        public static Texture2D sawTexture;
        public static Texture2D glowmask;
        public static ArmorShaderData data;
        int sawFrameCounter;
        int sawFrame;
        float auraBonus;
        float lerpPercent;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (!active)
            {
                Lighting.AddLight(NPC.Center, TorchID.Bone);
            }
            else
            {
                Lighting.AddLight(NPC.Center, TorchID.Orange);
            }

            UsefulFunctions.EnsureLoaded(ref sawTexture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeSawBlade");
            UsefulFunctions.EnsureLoaded(ref holderTexture, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeBuzzsaw");
            UsefulFunctions.EnsureLoaded(ref glowmask, "tsorcRevamp/NPCs/Bosses/PrimeV2/PrimeBuzzsaw_Glowmask");

            TheMachine.DrawMachineAura(Color.OrangeRed, active, NPC, auraBonus);
            auraBonus *= 0.8f;

            //Glowmask lerp percent
            if (active)
            {
                if (Prime.MoveTimer < 160)
                {
                    lerpPercent = Prime.MoveTimer / 160f;
                }
                else
                {
                    lerpPercent = 1;
                }
            }
            else
            {
                lerpPercent *= 0.95f;
            }

            //Draw metal bones


            if (!UsefulFunctions.AnyProjectile(ModContent.ProjectileType<Projectiles.Enemy.Prime.PrimeSaw>()))
            {
                sawFrameCounter++;
                if (sawFrameCounter > 1)
                {
                    sawFrameCounter = 0;
                    sawFrame++;
                    if (sawFrame >= 4)
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

                Color sawColor = Color.Lerp(Color.White, Color.OrangeRed, lerpPercent);
                if (lerpPercent > 0.05f)
                {
                    data.UseColor(Color.Lerp(Color.Black, Color.OrangeRed, lerpPercent));
                    data.Apply(null);
                }

                int frameHeight = sawTexture.Height / 4;
                int startY = frameHeight * sawFrame;
                Rectangle sawSourceRectangle = new Rectangle(0, startY, sawTexture.Width, frameHeight);
                Vector2 sawDrawOrigin = new Vector2(sawSourceRectangle.Width * 0.5f, sawSourceRectangle.Height * 0.5f);
                Main.EntitySpriteDraw(sawTexture, NPC.Center - Main.screenPosition + new Vector2(0, 62) - new Vector2(0, 16) + drawOffset, sawSourceRectangle, sawColor, 0, sawDrawOrigin, 1.3f, SpriteEffects.None, 0);

                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
            }


            //Draw shadow trail (and maybe normal trail?)

            Rectangle sourceRectangle = new Rectangle(0, 0, holderTexture.Width, holderTexture.Height / 2);
            if (damaged)
            {
                sourceRectangle.Y = holderTexture.Height / 2;
            }


            Vector2 drawOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(holderTexture, NPC.Center - Main.screenPosition - new Vector2(0, 16), sourceRectangle, drawColor, 0, drawOrigin, 1f, SpriteEffects.None, 0);



            //Draw glowmask
            Rectangle glowmaskRectangle = new Rectangle(0, 0, glowmask.Width, glowmask.Height);
            Vector2 glowmaskOrigin = new Vector2(sourceRectangle.Width * 0.5f, sourceRectangle.Height * 0.5f);
            Main.EntitySpriteDraw(glowmask, NPC.Center - Main.screenPosition - new Vector2(0, 16), glowmaskRectangle, Color.White * lerpPercent, 0, glowmaskOrigin, 1f, SpriteEffects.None, 0);

            return false;
        }

        public override void OnKill()
        {
            UsefulFunctions.SimpleGore(NPC, "Buzzsaw_Destroyed_1");
            UsefulFunctions.SimpleGore(NPC, "Buzzsaw_Destroyed_2");
            UsefulFunctions.SimpleGore(NPC, "Buzzsaw_Destroyed_3");
        }

        public override bool CheckActive()
        {
            return false;
        }
    }
}