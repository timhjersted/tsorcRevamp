using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.NPCs.Special
{
    class GwynBossVision : ModNPC
    {
        public override void SetDefaults()
        {
            NPC.scale = 1;
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            Main.npcFrameCount[NPC.type] = 8;
            NPC.width = 40;
            NPC.height = 40;
            NPC.defense = 38;
            AnimationType = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lifeMax = 300000;
            NPC.timeLeft = 22500;
            NPC.alpha = 100;
            NPC.friendly = false;
            NPC.noTileCollide = true;
            NPC.noGravity = true;
            NPC.knockBackResist = 0f;
            NPC.lavaImmune = true;
            NPC.value = 600000;
            NPC.dontTakeDamage = true;
            NPC.behindTiles = true;
            NPC.ShowNameOnHover = false;
        }

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Gwyn Boss Vision");
        }

        //Location in world, "is the boss downed", and "has the vision shattered when the player walked by"
        BossVision[] bossVisions;
        public override void AI()
        {            
            //Make the npc last forever
            NPC.timeLeft++;

            //Make its centerpoint fixed
            NPC.Center = new Vector2(670 * 16, 1164 * 16);

            //Initialize the data for the visions
            if (bossVisions == null)
            {
                bossVisions = new BossVision[7];
                bossVisions[0] = new BossVision(700, 1115, ModContent.NPCType<WaterFiendKraken>(), "tsorcRevamp/NPCs/Special/Visions/WaterFiendKraken", 1, Color.Blue);
                bossVisions[1] = new BossVision(658, 1129, ModContent.NPCType<FireFiendMarilith>(), "tsorcRevamp/NPCs/Special/Visions/FireFiendMarilith", 1.2f, Color.OrangeRed);
                bossVisions[2] = new BossVision(622, 1140, ModContent.NPCType<EarthFiendLich>(), "tsorcRevamp/NPCs/Special/Visions/EarthFiendLich", 1.5f, Color.Green);
                bossVisions[3] = new BossVision(622, 1190, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>(), "tsorcRevamp/NPCs/Special/Visions/Artorias", 1.5f, Color.DarkBlue * 4);
                bossVisions[4] = new BossVision(656, 1201, ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>(), "tsorcRevamp/NPCs/Special/Visions/WyvernMageShadow", 1.5f, Color.Purple * 4);
                bossVisions[5] = new BossVision(700, 1218, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>(), "tsorcRevamp/NPCs/Special/Visions/SeathTheScalelessHead", 1, Color.White);
                bossVisions[6] = new BossVision(736, 1231, ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>(), "tsorcRevamp/NPCs/Special/Visions/Artorias_Outline", 1, Color.Cyan);
            }

            //Lighting
            for(int i = 0; i < bossVisions.Length; i++)
            {
                if (bossVisions[i].activationProgress == 0)
                {
                    Lighting.AddLight(bossVisions[i].Center, bossVisions[i].color);
                }
                else
                {
                    Lighting.AddLight(bossVisions[i].Center, Color.Gray.ToVector3() * 0.3f);
                }
            }

            if (bossVisions[6].activationProgress == 0)
            {
                float diff = (float)Math.Sin(Main.GameUpdateCount / 50f);
                int dust = Dust.NewDust(new Vector2(740, 1231 + diff) * 16, 60, 110, 15, 0, 0, 250, default, 5f);
                Main.dust[dust].noGravity = true;
            }

            //Check if a player is close enough to activate any of the visions
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    for(int j = 0; j < 7; j++)
                    {
                        if (Math.Abs(Main.player[i].Center.X - bossVisions[j].Center.X) < 50 && Math.Abs(Main.player[i].Center.Y - bossVisions[j].Center.Y) < 400)
                        {
                            //If they can activate, and are not activated, then activate them
                            if (bossVisions[j].canActivate && bossVisions[j].activationProgress == 0)
                            {
                                bossVisions[j].activationProgress = 1;
                                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.4f, Pitch = 0.0f });
                            }
                        }
                    }
                }
            }

            //Despawn if Gwyn dies
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.SuperHardMode.Gwyn>())))
            {
                for (int i = 0; i < bossVisions.Length; i++)
                {
                    for (int j = 0; j < 20; j++)
                    {
                        Dust.NewDust(bossVisions[i].Center, 0, 0, DustID.WhiteTorch, Main.rand.NextFloat(-10, 10), Main.rand.NextFloat(-10, 10), Scale: 3);
                    }
                }

                NPC.active = false;
            }
        }

        
        public static Texture2D[] textures;
        public static Texture2D[] outlines;
        //public static ArmorShaderData data;
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)        
        {
            if(bossVisions == null)
            {
                return false;
            }
            //Initialize texture arrays
            if(textures == null)
            {
                textures = new Texture2D[7];
            }
            if (outlines == null)
            {
                outlines = new Texture2D[7];
            }

            for (int i = 0; i < 7; i++)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                /*
                //Apply the shader, caching it as well
                if (data == null)
                {
                    data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/MarilithIntro", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "MarilithIntroPass");
                }*/

                if (textures[i] == null || textures[i].IsDisposed)
                {
                    textures[i] = (Texture2D)ModContent.Request<Texture2D>(bossVisions[i].texturePath, ReLogic.Content.AssetRequestMode.ImmediateLoad);
                }

                //data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.AcidDye), Main.LocalPlayer);

                //data.UseImage("tsorcRevamp/Projectiles/Enemy/Marilith/CataclysmicFirestorm");
                //data.UseColor();
                //data.UseSaturation(progress);

                //Apply the shader
                //data.Apply(null);

                Rectangle recsize = new Rectangle(0, 0, textures[i].Width, textures[i].Height);


                float scale = 1;

                switch (i)
                {
                    case 0:
                        break;
                    case 1:
                        scale = 1.2f;
                        break;
                    case 2:
                        scale = 1.5f;
                        break;
                    case 3:
                        scale = 1.5f;
                        break;
                    case 4:
                        scale = 1.5f;
                        break;
                    case 5:
                        break;
                    case 6:
                        break;
                }

                float diff = (float)Math.Sin(i + Main.GameUpdateCount / 50f);
                scale += diff * diff / 50f;
                Vector2 offset = new Vector2(0, 16 * diff);
                offset -= new Vector2(textures[i].Width / 2, textures[i].Height / 2);
                Color thisColor = Color.White;
                if(bossVisions[i].activationProgress > 0)
                {
                    ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.ReflectiveMetalDye), Main.LocalPlayer);
                    data.Apply(null);
                }

                //Draw the rendertarget with the shader
                Main.spriteBatch.Draw(textures[i], (bossVisions[i].Center) - Main.screenPosition + offset, recsize, thisColor, 0, Vector2.Zero, scale, SpriteEffects.None, 0);
            }

            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
                        
            return false;
        }
        
        public override bool CheckActive()
        {
            return false;
        }
    }

    public struct BossVision
    {
        public Vector2 Center;
        public int bossID;

        public bool canActivate
        {
            get { 
                return tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(bossID));
            }
        }

        public int activationProgress;
        public string texturePath;
        public float scale;
        public Vector3 color;

        public BossVision(float X, float Y, int ID, string texturepath, float drawScale, Color lightColor)
        {
            Center = new Vector2(X, Y) * 16;
            bossID = ID;
            activationProgress = 0;
            texturePath = texturepath;
            scale = drawScale;
            color = lightColor.ToVector3();
        }
    }
}