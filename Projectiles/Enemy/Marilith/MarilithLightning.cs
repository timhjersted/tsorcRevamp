using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Marilith
{

    public class MarilithLightning : EnemyGenericLaser
    {

        //Titled "EnemyLightningStrike", but could also be used for player projectiles (and indeed is right now).
        //Warning: Highly experimental. Many commented out things are unfinished and do not work yet, others are incomplete.
        //What I would *really* like to do is draw the lightning strike as a chain of triangles angled precisely so there aren't any overlaps or breaks in its texture. As with everything here though, that's easier said than done.
        //Currently it just drwas a bunch of squares segments that intersect and don't mesh well at the edge of each joint. This is advanced level cringe, but it displays the concept well enough.
        //DrawPrims function works, and is using a sample list of verticies to draw a sample object.
        //The next thing to do is write the code to convert the stored coordinates of each joint into a list of verticies to send to the GPU.

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laser");
        }

        public override string Texture => base.Texture;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hide = true;
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;

            FollowHost = true;
            LaserOrigin = Main.npc[HostIdentifier].Center;
            TelegraphTime = 60;
            FiringDuration = 30;
            MaxCharge = 60;
            LaserLength = 2000;
            TileCollide = true;
            LaserSize = 1.3f;
            LaserColor = Color.Cyan;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.Lightning;

            LaserTextureBody = new Rectangle(0, 0, 10, 4);
            LaserSound = null;

            LaserDebuffs = new List<int>(BuffID.Electrified);
            DebuffTimers = new List<int>(300);
            LaserName = "Fractoemissive Discharge";

            CastLight = true;
        }


        public override void AI()
        {
            //System.Diagnostics.Stopwatch thisWatch = new System.Diagnostics.Stopwatch();
            //thisWatch.Start();
            if (IsAtMaxCharge)
            {
                displayDuration--;
            }

            if (branches == null && Main.rand.NextBool(3))
            {
                randomness = 25;
                SetLaserPosition();
            }

            base.ChargeLaser();


            Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);

            //Dust along lightning lines
            if (FiringTimeLeft == 28)
            {
                int dustCount = 4;
                if (branches != null && branches.Count > 0)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath43 with { Volume = 0.4f, Pitch = 0.0f }, branches[0][0]);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath43 with { Volume = 0.4f, Pitch = 0.0f }, branches[0][branches[0].Count / 2]);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCDeath43 with { Volume = 0.4f, Pitch = 0.0f }, branches[0][branches[0].Count - 1]);

                    for (int i = 0; i < branches.Count; i++)
                    {
                        if (branches[i].Count > 0)
                        {
                            if (FastContainsPoint(screenRect, branches[i][0]) || i == 0)
                            {
                                for (int j = 0; j < branches[i].Count - 1; j++)
                                {

                                    Vector2 diff = branches[i][j + 1] - branches[i][j];
                                    diff = diff / dustCount;
                                    float lerpPercent = 0.8f * ((float)j / ((float)branches[i].Count - 1f));

                                    float scale = 1.7f;
                                    if (i == 0)
                                    {
                                        scale = 2.2f;
                                        lerpPercent = 0;
                                    }

                                    for (int k = 0; k < dustCount; k++)
                                    {
                                        Dust thisDust = Dust.NewDustPerfect(branches[i][j] + diff * k, DustID.AncientLight, Scale: scale);
                                        thisDust.noLight = true;
                                        thisDust.noGravity = true;
                                        thisDust.velocity = Vector2.Zero;
                                        thisDust.rotation = Main.rand.NextFloatDirection();
                                        thisDust.color = Color.Lerp(Color.White, Color.DarkBlue, lerpPercent);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (FiringTimeLeft > 5)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead && !Main.player[i].immune)
                        CustomCollision(Main.player[i]);
                }
            }

            //thisWatch.Stop();
            //if (thisWatch.ElapsedMilliseconds > 1)
                //Main.NewText("AI: " + thisWatch.Elapsed);
        }

        public bool randomized = false;
        List<List<Vector2>> branches;
        List<List<float>> branchAngles;
        List<List<float>> branchLengths;
        public int segmentCount = 80;
        public float segmentLength = 60;
        public float randomness = 150;
        bool positionSet = false;
        //public float initialAngleLimit = MathHelper.ToRadians(75); //Can diverge up to 75 degrees from 
        private void SetLaserPosition()
        {
            if (!positionSet)
            {
                branches = new List<List<Vector2>>();
                branchAngles = new List<List<float>>();
                branchLengths = new List<List<float>>();

                Tuple<List<Vector2>, List<float>, List<float>> initialLine = GenerateLightningLine(Projectile.position, Projectile.velocity.ToRotation(), segmentCount, false);

                branches.Add(initialLine.Item1);
                branchAngles.Add(initialLine.Item2);
                branchLengths.Add(initialLine.Item3);

                for (int i = 0; i < branches.Count; i++)
                {
                    if (branches[i].Count > 0)
                    {
                        for (int j = 0; j < branches[i].Count - 1; j++)
                        {
                            if (Main.rand.Next(3) == 0 && j > 5)
                            {
                                //If it's the first set of splits, let them go longer
                                int segmentLimit = 3;
                                if (i == 0)
                                {
                                    segmentLimit = 12;
                                }
                                Tuple<List<Vector2>, List<float>, List<float>> newBranch = GenerateLightningLine(branches[i][j], Projectile.velocity.ToRotation(), Main.rand.Next(segmentLimit), true);
                                branches.Add(newBranch.Item1);
                                branchAngles.Add(newBranch.Item2);
                                branchLengths.Add(newBranch.Item3);
                            }
                        }
                    }
                }

                positionSet = true;
            }
        }

        public Tuple<List<Vector2>, List<float>, List<float>> GenerateLightningLine(Vector2 initialPoint, float initialAngle, int maxLength, bool branch)
        {
            List<Vector2> currentBranch = new List<Vector2>();
            List<float> currentAngles = new List<float>();
            List<float> currentLengths = new List<float>();

            //Rotate it left or right by at least 0.35 radians if it's a branch
            if (branch)
            {
                if (Main.rand.NextBool())
                {
                    initialAngle += Main.rand.NextFloat(0.1f, 1.4f);
                }
                else
                {
                    initialAngle -= Main.rand.NextFloat(0.1f, 1.4f);
                }

                //initialAngle *= -1;
            }

            currentBranch.Add(Vector2.Zero);

            for (int j = 0; j < maxLength; j++)
            {
                Vector2 next = currentBranch[j];
                next.X += segmentLength;
                currentBranch.Add(next);
            }

            for (int j = 1; j < maxLength; j++)
            {
                currentBranch[j] = new Vector2(currentBranch[j].X, Main.rand.NextFloat(-randomness, randomness));
            }
            for (int j = 0; j < maxLength; j++)
            {
                currentBranch[j] = currentBranch[j].RotatedBy(initialAngle) + initialPoint;
            }


            //Create a temporary list to store the in-progress values, to avoid modifying the real branch list while also reading from it
            List<Vector2> newBranchPositions = new List<Vector2>();

            //Subdivide the list of points, adding new ones in-between the existing ones with less randomness
            for (int i = 0; i < maxLength - 1; i++)
            {
                newBranchPositions.Add(currentBranch[i]);

                Vector2 newPos = (currentBranch[i + 1] + currentBranch[i]) / 2;
                newPos += Main.rand.NextVector2Circular(randomness / 3, randomness / 3);
                newBranchPositions.Add(newPos);
            }

            currentBranch = newBranchPositions;


            //Then it checks to see if there's a wall in between any segments, and cuts off the lightning if it finds one
            if (TileCollide)
            {
                newBranchPositions = new List<Vector2>();

                for (int i = 0; i < maxLength - 1; i++)
                {
                    bool segmentCollides = false;
                    if (!Collision.CanHit(currentBranch[i], 1, 1, currentBranch[i + 1], 1, 1) && !Collision.CanHitLine(currentBranch[i], 1, 1, currentBranch[i + 1], 1, 1))
                    {
                        if (!branch || (!Collision.CanHit(Projectile.position, 1, 1, currentBranch[i + 1], 1, 1) && !Collision.CanHitLine(Projectile.position, 1, 1, currentBranch[i + 1], 1, 1)))
                        {
                            segmentCollides = true;
                        }
                    }

                    if (segmentCollides)
                    {
                        newBranchPositions.Add(currentBranch[i]);
                        if (i < maxLength - 2)
                        {
                            newBranchPositions.Add(currentBranch[i + 1]);
                        }
                        break;
                    }

                    newBranchPositions.Add(currentBranch[i]);
                }

                currentBranch = newBranchPositions;
            }

            for (int i = 0; i < currentBranch.Count - 1; i++)
            {
                currentAngles.Add((currentBranch[i] - currentBranch[i + 1]).ToRotation());
                currentLengths.Add(Vector2.Distance(currentBranch[i], currentBranch[i + 1]));
            }


            return new Tuple<List<Vector2>, List<float>, List<float>>(currentBranch, currentAngles, currentLengths);
        }


        public int displayDuration = 0;

        RenderTarget2D lightningTarget;
        Vector2 storedPosition;
        public static ArmorShaderData data;
        public override bool PreDraw(ref Color lightColor)
        {
            //Don't draw anything if it hasn't generated the branches
            if (branches == null || branches.Count == 0)
            {
                return false;
            }

            //If the target hasn't been created yet
            if (lightningTarget == null)
            {
                //End the old spritebatch 
                Main.spriteBatch.End();

                //Store a reference to the device to simplify code
                GraphicsDevice device = Main.graphics.GraphicsDevice;

                //Store the old bindings
                RenderTargetBinding[] bindings = device.GetRenderTargets();



                //Create the target
                lightningTarget = new RenderTarget2D(device, device.PresentationParameters.BackBufferWidth, device.PresentationParameters.BackBufferHeight, false, device.PresentationParameters.BackBufferFormat, device.PresentationParameters.DepthStencilFormat, device.PresentationParameters.MultiSampleCount, RenderTargetUsage.PreserveContents);

                //Set the device target to the new target
                device.SetRenderTarget(lightningTarget);

                //Clear it
                //device.Clear(Color.Transparent);

                //Start a new "default" spritebatch and draw the lightning to the target
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
                GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.LivingOceanDye), Main.LocalPlayer).Apply();
                DrawSegments();
                Main.spriteBatch.End();

                //Store the current screen position at time of drawing so it can be used to calculate the draw offset later
                storedPosition = Main.screenPosition;

                //Re-set the old bindings
                device.SetRenderTargets(null);

                //Re-start the spritebatch
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);
            }

            //Draw the target
            float scaleFactor = (float)FiringTimeLeft / (float)FiringDuration;
            Color color = Color.White * scaleFactor * scaleFactor;

            //Make it a bit transparent and desaturated if it's just the targeting lines
            if (!IsAtMaxCharge)
            {
                color = Color.Gray * 0.5f;
            }

            //Calculate how far offset the current screen position is from where it was when it was drawn
            Vector2 offset = storedPosition - Main.screenPosition;

            //Restart the spritebatch so the shader can be applied to it
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.AcidDye), Main.LocalPlayer);

            //Apply the shader, caching it as well
            if(data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/LightningShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "LightningShaderPass");
            }

            //The shader uses "saturation" variable to tell what percent of the lightning to draw, from 0 to 1
            data.UseSaturation((float)Charge / MaxCharge);
            //It uses the TargetPosition variable to tell it the starting point of the lightning it crops relative to
            data.UseTargetPosition((Projectile.position - Main.screenPosition) / Main.ScreenSize.ToVector2());

            //Apply the shader
            data.Apply(null);

            //Draw the rendertarget with the shader
            Main.spriteBatch.Draw(lightningTarget, offset, new Rectangle(0, 0, lightningTarget.Width, lightningTarget.Height), color, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            
            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }



        /*
         * if (!drawnFinalOnce) //This is set to true once the full lightning chain has been drawn once, so there is no need to draw it to the rendertarget again
                {
                    //Runs through a big for loop, simply calling Main.EntitySpriteDraw for every segment of the lightning
                    
                }
         * 
         * System.Diagnostics.Stopwatch thisWatch = new System.Diagnostics.Stopwatch();
            thisWatch.Start();

            thisWatch.Stop();
            if (thisWatch.ElapsedMilliseconds > 1)
            {
                int totalCount = 0;
                for(int i = 0; i < branches.Count; i++)
                {
                    totalCount += branches[i].Count;
                }
                System.TimeSpan avg = thisWatch.Elapsed / totalCount;
                Main.NewText(totalCount + " branches at " + avg + " per branch for " + thisWatch.Elapsed + " total");
            }*/

        public void DrawPrims()
        {
            GraphicsDevice device = Main.graphics.GraphicsDevice;
            device.Textures[0] = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.Lightning];

            VertexPositionColor[] vertices = new VertexPositionColor[12];
            vertices[0] = new VertexPositionColor(new Vector3(-0.26286500f, 0.0000000f, 0.42532500f), Color.Red);
            vertices[1] = new VertexPositionColor(new Vector3(0.26286500f, 0.0000000f, 0.42532500f), Color.Orange);
            vertices[2] = new VertexPositionColor(new Vector3(-0.26286500f, 0.0000000f, -0.42532500f), Color.Yellow);
            vertices[3] = new VertexPositionColor(new Vector3(0.26286500f, 0.0000000f, -0.42532500f), Color.Green);
            vertices[4] = new VertexPositionColor(new Vector3(0.0000000f, 0.42532500f, 0.26286500f), Color.Blue);
            vertices[5] = new VertexPositionColor(new Vector3(0.0000000f, 0.42532500f, -0.26286500f), Color.Indigo);
            vertices[6] = new VertexPositionColor(new Vector3(0.0000000f, -0.42532500f, 0.26286500f), Color.Purple);
            vertices[7] = new VertexPositionColor(new Vector3(0.0000000f, -0.42532500f, -0.26286500f), Color.White);
            vertices[8] = new VertexPositionColor(new Vector3(0.42532500f, 0.26286500f, 0.0000000f), Color.Cyan);
            vertices[9] = new VertexPositionColor(new Vector3(-0.42532500f, 0.26286500f, 0.0000000f), Color.Black);
            vertices[10] = new VertexPositionColor(new Vector3(0.42532500f, -0.26286500f, 0.0000000f), Color.DodgerBlue);
            vertices[11] = new VertexPositionColor(new Vector3(-0.42532500f, -0.26286500f, 0.0000000f), Color.Crimson);

            short[] indices = new short[60];
            indices[0] = 0; indices[1] = 6; indices[2] = 1;
            indices[3] = 0; indices[4] = 11; indices[5] = 6;
            indices[6] = 1; indices[7] = 4; indices[8] = 0;
            indices[9] = 1; indices[10] = 8; indices[11] = 4;
            indices[12] = 1; indices[13] = 10; indices[14] = 8;
            indices[15] = 2; indices[16] = 5; indices[17] = 3;
            indices[18] = 2; indices[19] = 9; indices[20] = 5;
            indices[21] = 2; indices[22] = 11; indices[23] = 9;
            indices[24] = 3; indices[25] = 7; indices[26] = 2;
            indices[27] = 3; indices[28] = 10; indices[29] = 7;
            indices[30] = 4; indices[31] = 8; indices[32] = 5;
            indices[33] = 4; indices[34] = 9; indices[35] = 0;
            indices[36] = 5; indices[37] = 8; indices[38] = 3;
            indices[39] = 5; indices[40] = 9; indices[41] = 4;
            indices[42] = 6; indices[43] = 10; indices[44] = 1;
            indices[45] = 6; indices[46] = 11; indices[47] = 7;
            indices[48] = 7; indices[49] = 10; indices[50] = 6;
            indices[51] = 7; indices[52] = 11; indices[53] = 2;
            indices[54] = 8; indices[55] = 10; indices[56] = 3;
            indices[57] = 9; indices[58] = 11; indices[59] = 0;


            VertexBuffer vertexBuffer;
            vertexBuffer = new VertexBuffer(device, typeof(VertexPositionColor), 12, BufferUsage.WriteOnly);
            vertexBuffer.SetData<VertexPositionColor>(vertices);
            device.SetVertexBuffer(null);
            device.SetVertexBuffer(vertexBuffer);

            IndexBuffer indexBuffer;
            indexBuffer = new IndexBuffer(device, typeof(short), indices.Length, BufferUsage.WriteOnly);
            indexBuffer.SetData(indices);
            device.Indices = indexBuffer;


            BasicEffect basicEffect = new BasicEffect(device);
            basicEffect.VertexColorEnabled = true;


            Vector2 worldPos = Projectile.position - Main.screenPosition;
            worldPos.X = (worldPos.X / (Main.screenWidth / 2)) - 1;
            worldPos.Y = (worldPos.Y / (Main.screenHeight / -2f)) + 1;
            worldPos *= Main.GameZoomTarget;
            worldPos.X *= 3f / 2f;
            basicEffect.World = Matrix.CreateTranslation(new Vector3(worldPos.X, worldPos.Y, 0));
            basicEffect.Projection = Matrix.CreateOrthographic(3, 2, 0, 100f);// * Main.GameViewMatrix.ZoomMatrix;

            //Still literally can't get any of this to work, so i'm working around it for now instead
            //basicEffect.Projection *= Main.GameViewMatrix.ZoomMatrix;
            //basicEffect.View = Matrix.CreateLookAt(new Vector3(0.0f, 0.0f, 1.0f), Vector3.Zero, Vector3.Up);
            //basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(75), 800f / 480f, 1f, 1000f);
            //basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, (float)device.Viewport.Width, (float)device.Viewport.Height, 0, 1.0f, 1000.0f); //Doesn't work :/
            //basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, 1920, 1080, 0, 1.0f, 1000.0f); //Also doesn't work :/
            //basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, 0, -1);


            //basicEffect.World = Matrix.CreateTranslation(Main.screenPosition.X, Main.screenPosition.Y, 0);
            //basicEffect.View = Main.GameViewMatrix.ZoomMatrix;
            //basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, 3, 2, 0, -1, 1);

            RasterizerState rasterizerState = new RasterizerState();
            rasterizerState.CullMode = CullMode.None;
            device.RasterizerState = rasterizerState;

            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 12, 0, 20);
            }
        }

        public void DrawSegments()
        {            
            for (int i = 0; i < branches.Count; i++)
            {
                for (int j = 0; j < branches[i].Count - 1; j++)
                {
                    float scale = 0.7f;
                    if (i == 0)
                    {
                        scale = 1;
                    }
                    DrawLightning(Main.spriteBatch, TransparentTextureHandler.TransparentTextures[LaserTexture], branches[i][j],
                            branches[i][j + 1], LaserTargetingHead, LaserTextureBody, LaserTargetingTail, branchLengths[i][j], branchAngles[i][j], scale, LaserColor);
                }
            }
        }

        public void DrawLightning(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, Rectangle headRect, Rectangle bodyRect, Rectangle tailRect, float distance, float rotation = 0f, float scale = 1f, Color color = default)
        {

            //Defines an area where laser segments should actually draw, 100 pixels larger on each side than the screen
            Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);
            
            float i = 0;
            Vector2 diff = unit - start;
            diff.Normalize();

            Vector2 startPos = start - diff * 3;
            for (; i <= distance; i += (bodyRect.Height) * scale)
            {
                Vector2 drawStart = startPos + i * diff;
                if (screenRect.Contains(drawStart.ToPoint()))
                {
                    Main.EntitySpriteDraw(texture, drawStart - Main.screenPosition, bodyRect, color, rotation + MathHelper.PiOver2, new Vector2(bodyRect.Width * .5f, bodyRect.Height * .5f), scale, 0, 0);
                }
            }
        }

        public override void Kill(int timeLeft)
        {
            if (lightningTarget != null && !lightningTarget.IsDisposed)
            {
                lightningTarget.Dispose();
            }
        }

        public void CustomCollision(Player target)
        {
            Rectangle targetHitbox = target.Hitbox;
            if (branches == null || branches.Count == 0 || branches[0].Count == 0 || FiringTimeLeft != 28)
            {
                return;
            }

            if (!IsAtMaxCharge)
            {
                if (!(Charge == MaxCharge / 2 || Charge == (MaxCharge / 3) - 5 || Charge == MaxCharge / 3))
                {
                    return;
                }
            }

            bool collides = false;
            float point = 0;

            if (branches.Count > 0 && !collides)
            {
                for (int i = 0; i < branches.Count; i++)
                {
                    if (branches[i].Count > 0)
                    {
                        for (int j = 0; j < branches[i].Count - 1; j++)
                        {
                            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), branches[i][j], branches[i][j + 1], 10, ref point))
                            {
                                CanHitPlayer(target);
                                break;
                            }
                        }
                    }
                }
            }

            if (!collides && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), branches[0][0], branches[0][branches[0].Count - 1], 10, ref point))
            {
                CanHitPlayer(target);
            }

        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            /*
             * 
             * 
             * float percentCharged = Charge / MaxCharge;
                //float minCharge = percentCharged - 0.15f;
                float maxCharge = percentCharged + 0.15f;
                for (int i = 0; i < branches.Count; i++)
                {
                    float percentDrawn = ((float)i) / ((float)branches.Count);
                    if (percentDrawn < maxCharge || i == 0)
                    {
                        for (int j = 0; j < branches[i].Count - 1; j++)
                        {
                            float segmentPercentDrawn = ((float)j) / ((float)branches[i].Count);
                            if ((segmentPercentDrawn < maxCharge))
                            {
                                float scale = 0.3f;
                                if (i == 0)
                                {
                                    scale = 0.6f;
                                }
                                DrawLightning(Main.spriteBatch, TransparentTextureHandler.TransparentTextures[LaserTexture], branches[i][j],
                                        branches[i][j + 1], LaserTargetingHead, LaserTextureBody, LaserTargetingTail, branchLengths[i][j], branchAngles[i][j], scale, Color.Gray * scale);
                            }
                        }
                    }
                }
            if (branches == null || branches.Count == 0 || branches[0].Count == 0)
            {
                return false;
            }

            if (!IsAtMaxCharge)
            {
                if (!(Charge == MaxCharge / 2 || Charge == (MaxCharge / 3) - 5 || Charge == MaxCharge / 3))
                {
                    return false;
                }
            }

            bool collides = false;
            float point = 0;

            if (branches.Count > 0 && !collides)
            {
                for (int i = 0; i < branches.Count; i++)
                {
                    if (branches[i].Count > 0)
                    {
                        for (int j = 0; j < branches[i].Count - 1; j++)
                        {
                            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), branches[i][j], branches[i][j + 1], 10, ref point))
                            {
                                collides = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (!collides && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), branches[0][0], branches[0][branches[0].Count - 1], 10, ref point))
            {
                collides = true;
            }
            */
            return false;
        }


        public override void CutTiles()
        {
            if (branches == null || branches.Count == 0 || branches[0].Count == 0)
            {
                return;
            }

            if (!IsAtMaxCharge)
            {
                if (!(Charge == MaxCharge / 2 || Charge == (MaxCharge / 3) - 5 || Charge == MaxCharge / 3))
                {
                    return;
                }
            }

            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;

            if (branches.Count > 0)
            {
                for (int i = 0; i < branches.Count; i++)
                {
                    if (branches[i].Count > 0)
                    {
                        for (int j = 0; j < branches[i].Count - 1; j++)
                        {
                            Utils.PlotTileLine(branches[i][j], branches[i][j + 1], 30, DelegateMethods.CutTiles);
                        }
                    }
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (!IsAtMaxCharge)
            {
                damage /= 3;
            }
            target.AddBuff(BuffID.Electrified, 300);
            target.AddBuff(BuffID.Slow, 150);
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.Electrified, 300);
            target.AddBuff(BuffID.Slow, 150);
        }

        public override bool CanHitPlayer(Player target)
        {

            string deathMessage = Terraria.DataStructures.PlayerDeathReason.ByProjectile(-1, Projectile.whoAmI).GetDeathText(target.name).ToString();
            deathMessage = deathMessage.Replace("Laser", LaserName);
            target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(deathMessage), Projectile.damage * 4, 1);

            return false;
        }
    }
}
