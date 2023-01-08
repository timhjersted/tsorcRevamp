using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{
    public class MarilithFirewall : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 5;
        }

        public override void SetDefaults()
        {

            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 20;
        }

        int progress = 0;
        float cloudProgress = 0;
        public override void AI()
        {
            //Keep it alive indefinitely while marilith is alive
            Projectile.timeLeft = 2;
            if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>()))
            {
                Projectile.Kill();
            }
            else
            {
                NPCs.Bosses.Fiends.FireFiendMarilith marl = Main.npc[UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.Fiends.FireFiendMarilith>()).Value].ModNPC as NPCs.Bosses.Fiends.FireFiendMarilith;
                if(Projectile.ai[0] == 2 && marl.MoveIndex == 1 && marl.MoveTimer < 1800)
                {
                    cloudProgress++;
                    if (cloudProgress > 300)
                    {
                        cloudProgress = 300;
                    }
                }
                else
                {
                    cloudProgress--;
                    if(cloudProgress < 0)
                    {
                        cloudProgress = 0;
                    }
                }
            }

            if(progress < 100)
            {
                progress++;
            }

            Projectile.alpha = (int)(progress * 2.5f);
            
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {

                int width = (int)(140);
                int longLength = (int)(252 );
                int shortLength = (int)(106);
                //Left
                if (Projectile.ai[0] == 0) 
                {
                    Projectile.width = width;
                    Projectile.height = 16 * shortLength;
                    Projectile.Center = new Vector2(3107, 1731) * 16;
                }
                //Right
                else if(Projectile.ai[0] == 1)
                {
                    Projectile.width = width;
                    Projectile.height = 16 * shortLength;
                    Projectile.Center = new Vector2(3350.2f, 1731) * 16;
                }//Top
                else if (Projectile.ai[0] == 2)
                {
                    Projectile.width = 16 * longLength;
                    Projectile.height = width;
                    Projectile.Center = new Vector2(3228.5f, 1682.3f) * 16;
                }//Bottom
                else if (Projectile.ai[0] == 3)
                {
                    Projectile.width = 16 * longLength;
                    Projectile.height = width;
                    Projectile.Center = new Vector2(3228.5f, 1779.8f) * 16;
                }
            }
            

            int frameHeight = ((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]).Height / Main.projFrames[Projectile.type];
            int segmentCount = Projectile.height / frameHeight;


            DelegateMethods.v3_1 = Color.OrangeRed.ToVector3() * 2f;
            Vector2 startPoint = Projectile.Center;
            Vector2 endpoint = Projectile.Center;
            if (Projectile.ai[0] == 0)
            {
                startPoint.Y -= Projectile.height / 2;
                endpoint.Y += Projectile.height;
            }
            else
            {
                startPoint.X -= Projectile.width / 2;
                endpoint.X += Projectile.width / 2;
            }
            Utils.PlotTileLine(startPoint, endpoint, 16, DelegateMethods.CastLight);




            Projectile.frameCounter++;
            if (Projectile.frameCounter > 3)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            if (Projectile.frame >= 5)
            {
                Projectile.frame = 0;
            }

            for (int i = 0; i < Main.maxPlayers; i++) {
                if (Projectile.Hitbox.Contains(Main.player[i].Center.ToPoint()))
                {
                    Main.player[i].statLife -= 5;
                    CombatText.NewText(Main.player[i].Hitbox, Color.Red, 5);
                    if (Main.player[i].statLife < 1)
                    {
                        Main.player[i].statLife = 1;
                        Main.player[i].immune = false;
                        Main.player[i].immuneTime = 0;
                    }
                }
            }
        }


        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {

        }


        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (progress >= 100)
            {
                //target.immune = false;
                //target.immuneTime = 0;
            }
        }

        public static ArmorShaderData data;
        float modifiedTime;
        public override bool PreDraw(ref Color lightColor)
        {
            
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            //if (data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/FireWallShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "FireWallShaderPass");
            }
                        
            //Pass relevant data to the shader via these parameters
            data.UseSaturation(Projectile.ai[0]);
            data.UseSecondaryColor(progress, cloudProgress, modifiedTime);
            if(Projectile.ai[0] == 2)
            {
                modifiedTime += 1 - (cloudProgress / 300f);
            }

            //Apply the shader
            data.Apply(null);
            
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            Rectangle sourceRectangle = new Rectangle(0, 0, Projectile.width, Projectile.height);
            Vector2 origin = sourceRectangle.Size() / 2f;

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTexture1, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);
            
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
