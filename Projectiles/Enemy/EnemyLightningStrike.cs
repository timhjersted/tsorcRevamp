using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy {

    public class EnemyLightningStrike : EnemyGenericLaser {

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

        public override void SetDefaults() {
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
            TelegraphTime = 120;
            FiringDuration = 30;
            MaxCharge = 120;
            LaserLength = 1000;
            TileCollide = true;
            LaserSize = 1.3f;
            LaserColor = Color.Cyan;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.Lightning;
            
            LaserTextureBody = new Rectangle(0, 0, 10, 4);
            LaserSound = null;

            LaserDebuffs = new List<int>(BuffID.Electrified);
            DebuffTimers = new List<int>(300);

            CastLight = true;
        }


        public override void AI()
        {
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.damage = 300;
            displayDuration--;
            if (Charge < MaxCharge - 1)
            {
                Charge = MaxCharge - 1;
            }

            base.ChargeLaser();

            //Dust on player when  using
            Dust.NewDustPerfect(Projectile.position, DustID.TorchworkFountain_Blue, Main.rand.NextVector2Circular(3, 3)).noGravity = true;

            //Dust along lightning lines
            if (FiringTimeLeft == 28)
            {
                int dustCount = 4;
                
                if (branches.Count > 0)
                {
                    for (int i = 0; i < branches.Count; i++)
                    {
                        if (branches[i].Count > 0)
                        {
                            for (int j = 0; j < branches[i].Count - 1; j++)
                            {
                                Vector2 diff = branches[i][j + 1] - branches[i][j];
                                diff = diff / dustCount;
                                float lerpPercent = 0.8f * ((float)j / ((float)branches[i].Count - 1f));

                                float scale = 1.7f;
                                if(i == 0)
                                {
                                    scale = 2.2f;
                                    lerpPercent = 0;
                                }

                                for (int k = 0; k < dustCount; k++)
                                {
                                    Dust thisDust = Dust.NewDustPerfect(branches[i][j] + diff * k, DustID.AncientLight, Scale: scale);
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

        public bool randomized = false;
        List<List<Vector2>> branches;
        List<List<float>> branchAngles;
        public int segmentCount = 60;
        public float segmentLength = 40;
        public float randomness = 50;
        //public float initialAngleLimit = MathHelper.ToRadians(75); //Can diverge up to 75 degrees from 
        private void SetLaserPosition()
        {
            branches = new List<List<Vector2>>();
            branchAngles = new List<List<float>>();

            Tuple<List<Vector2>, List<float>> initialLine = GenerateLightningLine(Projectile.position, Projectile.velocity.ToRotation(), segmentCount, false);

            branches.Add(initialLine.Item1);
            branchAngles.Add(initialLine.Item2);

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
                            if(i == 0)
                            {
                                segmentLimit = 12;
                            }
                            Tuple<List<Vector2>, List<float>> newBranch = GenerateLightningLine(branches[i][j], Projectile.velocity.ToRotation(), Main.rand.Next(segmentLimit), true);
                            branches.Add(newBranch.Item1);
                            branchAngles.Add(newBranch.Item2);
                        }
                    }
                }
            }

        }

        public Tuple<List<Vector2>, List<float>> GenerateLightningLine(Vector2 initialPoint, float initialAngle, int maxLength, bool branch)
        {
            List<Vector2> currentBranch = new List<Vector2>();
            List<float> currentAngles = new List<float>();

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
            bool branchCollided = false;
            if (TileCollide)
            {
                newBranchPositions = new List<Vector2>();

                for (int i = 0; i < maxLength - 1; i++)
                {
                    bool segmentCollides = false;
                    if (!Collision.CanHit(currentBranch[i], 1, 1, currentBranch[i + 1], 1, 1) && !Collision.CanHitLine(currentBranch[i], 1, 1, currentBranch[i + 1], 1, 1))
                    {
                        if(!branch || (!Collision.CanHit(Projectile.position, 1, 1, currentBranch[i + 1], 1, 1) && !Collision.CanHitLine(Projectile.position, 1, 1, currentBranch[i + 1], 1, 1)))
                        {
                            segmentCollides = true;
                            branchCollided = true;
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
            }

            if(currentBranch.Count > 0)
            {
                int dustCount = 3;
                if (!branch)
                {
                    dustCount = 15;
                    Terraria.Audio.SoundEngine.PlaySound(4, currentBranch[currentBranch.Count - 1], 43);
                }
                if (branchCollided)
                {
                    for (int i = 0; i < dustCount; i++)
                    {
                        float dustSize = Main.rand.NextFloat(0.6f, 0.8f);
                        if (branch)
                        {
                            dustSize = Main.rand.NextFloat(0.6f, 0.8f);
                        }
                        if (currentAngles.Count > 0)
                        {
                            Vector2 dustVel = new Vector2(-4f, 0).RotatedBy(currentAngles[currentAngles.Count - 1]);
                            dustVel += Main.rand.NextVector2Circular(10, 10);
                            Dust currentDust = Dust.NewDustPerfect(currentBranch[currentBranch.Count - 1], DustID.Electric, dustVel, 0, Color.RoyalBlue, dustSize);
                            currentDust.noGravity = true;
                        }
                    }
                }
            }
            

            return new Tuple<List<Vector2>, List<float>>(currentBranch, currentAngles);
        }


        public int displayDuration = 0;

        //RenderTarget2D renderTarget;

        public override bool PreDraw(ref Color lightColor)
        {
            if (!IsAtMaxCharge)
            {
                if ((Charge == MaxCharge / 2 || Charge == (MaxCharge / 3) - 5 || Charge == MaxCharge / 3) && displayDuration != 5)
                {
                    randomness = 25;
                    SetLaserPosition();

                    displayDuration = 5;
                }
                if (displayDuration > 0 && branches.Count > 0 && branches[0].Count > 0)
                {
                    for (int i = 0; i < branches[0].Count - 1; i++)
                    {
                        DrawLightning(spriteBatch, TransparentTextureHandler.TransparentTextures[LaserTexture], branches[0][i],
                                branches[0][i + 1], LaserTargetingHead, LaserTextureBody, LaserTargetingTail, Vector2.Distance(branches[0][i], branches[0][i + 1]), branchAngles[0][i], 0.2f, LaserColor * 0.9f);
                    }
                }

            }
            else
            {
                if (!randomized)
                {
                    randomness = 25;
                    SetLaserPosition();
                    randomized = true;
                }
                if (branches[0].Count > 0)
                {
                    float scaleFactor = (float)FiringTimeLeft / (float)FiringDuration;

                    spriteBatch.End();
                    spriteBatch.Begin(SpriteSortMode.Texture, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix); new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);                   
                                        
                    if (branches.Count > 0)
                    {
                        for (int i = 0; i < branches.Count; i++)
                        {
                            List<Vector2> currentBranch = branches[i];
                            List<float> currentAngles = branchAngles[i];

                            if (currentBranch.Count > 0)
                            {
                                for (int j = 0; j < branches[i].Count - 1; j++)
                                {
                                    Vector2 segment = currentBranch[j];
                                    Vector2 nextSegment = currentBranch[j + 1];

                                    float scale = 0.7f;
                                    if(i == 0)
                                    {
                                        scale = 1;
                                    }

                                    DrawLightning(spriteBatch, TransparentTextureHandler.TransparentTextures[LaserTexture], segment,
                                            nextSegment, LaserTargetingHead, LaserTextureBody, LaserTargetingTail, Vector2.Distance(segment, nextSegment), currentAngles[j], scale * scaleFactor, LaserColor * scaleFactor * scaleFactor);
                                }
                            }
                        }
                    }

                    spriteBatch.End();

                    //Revert to normal spritebatch mode
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix); new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);



                    //Testing the DrawPrims function. Currently draws a complex pre-defined 3D shape for testing purposes.
                    //DrawPrims();

                    #region Unfinished


                    /*
                    int vertexCount = currentSegmentCount;
                    GraphicsDevice device = Main.graphics.GraphicsDevice;
                    device.Textures[0] = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.Lightning];

                    SetLaserPosition();
                    VertexPositionColor[] vertices = new VertexPositionColor[vertexCount];
                    for (int i = 0; i < vertexCount; i++)
                    {
                        Vector2 vertexPos = projectile.position - Main.screenPosition;
                        vertexPos.X = (vertexPos.X / (Main.screenWidth / 2)) - 1;
                        vertexPos.Y = (vertexPos.Y / (Main.screenHeight / -2f)) + 1;
                        vertexPos *= Main.GameZoomTarget;
                        vertexPos.X *= 3f / 2f;
                        Main.NewText(vertexPos);
                        vertices[i] = new VertexPositionColor(new Vector3(vertexPos.X, vertexPos.Y, 0), Color.Red);
                    }

                    VertexBuffer vertexBuffer = new VertexBuffer(device, typeof(VertexPositionColor), vertexCount, BufferUsage.WriteOnly);
                    vertexBuffer.SetData(vertices);
                    device.SetVertexBuffer(vertexBuffer);

                    vertices = new VertexPositionColor[12];
                    vertices[0] = new VertexPositionColor(new Vector3(-0.26286500f, 0.0000000f, 0), Color.Red);
                    vertices[1] = new VertexPositionColor(new Vector3(0.26286500f, 0.0000000f, 0), Color.Orange);
                    vertices[2] = new VertexPositionColor(new Vector3(-0.26286500f, 0.0000000f, 0), Color.Yellow);
                    vertices[3] = new VertexPositionColor(new Vector3(0.26286500f, 0.0000000f, 0), Color.Green);
                    vertices[4] = new VertexPositionColor(new Vector3(0.0000000f, 0.42532500f, 0f), Color.Blue);
                    vertices[5] = new VertexPositionColor(new Vector3(0.0000000f, 0.42532500f, -0), Color.Indigo);
                    vertices[6] = new VertexPositionColor(new Vector3(0.0000000f, -0.42532500f, 0), Color.Purple);
                    vertices[7] = new VertexPositionColor(new Vector3(0.0000000f, -0.42532500f, -0), Color.White);
                    vertices[8] = new VertexPositionColor(new Vector3(0.42532500f, 0.26286500f, 0.0000000f), Color.Cyan);
                    vertices[9] = new VertexPositionColor(new Vector3(-0.42532500f, 0.26286500f, 0.0000000f), Color.Black);
                    vertices[10] = new VertexPositionColor(new Vector3(0.42532500f, -0.26286500f, 0.0000000f), Color.DodgerBlue);
                    vertices[11] = new VertexPositionColor(new Vector3(-0.42532500f, -0.26286500f, 0.0000000f), Color.Crimson);

                   vertexBuffer = new VertexBuffer(device, typeof(VertexPositionColor), 12, BufferUsage.WriteOnly);
                    vertexBuffer.SetData<VertexPositionColor>(vertices);
                    device.SetVertexBuffer(null);
                    device.SetVertexBuffer(vertexBuffer);

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
                    

                    IndexBuffer indexBuffer;
                    indexBuffer = new IndexBuffer(device, typeof(short), indices.Length, BufferUsage.WriteOnly);
                    indexBuffer.SetData(indices);
                    device.Indices = indexBuffer;
                    BasicEffect basicEffect = new BasicEffect(device);
                    basicEffect.VertexColorEnabled = true;


                    Vector2 worldPos = projectile.position - Main.screenPosition;
                    worldPos.X = (worldPos.X / (Main.screenWidth / 2)) - 1;
                    worldPos.Y = (worldPos.Y / (Main.screenHeight / -2f)) + 1;
                    worldPos *= Main.GameZoomTarget;
                    worldPos.X *= 3f / 2f;
                    basicEffect.World = Matrix.CreateTranslation(new Vector3(worldPos.X, worldPos.Y, 0));
                    basicEffect.Projection = Matrix.CreateOrthographic(3, 2, 0, 100f);// * Main.GameViewMatrix.ZoomMatrix;

                    

                    RasterizerState rasterizerState = new RasterizerState();
                    rasterizerState.CullMode = CullMode.None;
                    device.RasterizerState = rasterizerState;

                    foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        device.DrawPrimitives(PrimitiveType.LineStrip, 0, 12);// vertexCount - 1);
                    }*/
                    #endregion
                }

                #region Unfinished
                //End normal spritebatch
                //spriteBatch.End();

                /*
                RenderTarget2D oldTarget = (RenderTarget2D)Main.instance.GraphicsDevice.GetRenderTargets()[0].RenderTarget;
                //RenderTargetBinding[] bindings = Main.graphics.GraphicsDevice.GetRenderTargets();
                //RenderTarget2D target = new RenderTarget2D(spriteBatch.GraphicsDevice, Main.screenWidth, Main.screenHeight);
                renderTarget = null;
                //Set new rendertarget
                if (renderTarget == null || renderTarget.IsDisposed)
                {
                    renderTarget = new RenderTarget2D(Main.instance.GraphicsDevice, Main.screenWidth, Main.screenHeight, false, Main.graphics.GraphicsDevice.PresentationParameters.BackBufferFormat, DepthFormat.Depth24, 0, usage: RenderTargetUsage.PreserveContents);
                }
                Main.instance.GraphicsDevice.SetRenderTarget(renderTarget);
                //Main.instance.GraphicsDevice.Clear(Color.Transparent);
                
                //Draw to the rendertarget
                DrawPrims();


                //Unset it and clear
                //Main.graphics.GraphicsDevice.SetRenderTargets(bindings);
                //Main.instance.GraphicsDevice.SetRenderTarget(oldTarget);
                //Main.instance.GraphicsDevice.Clear(Color.Transparent);

                //Draw the rendertarget
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix); new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
                Rectangle screenRect = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
                for(int i = 0; i < 10; i++)
                Main.EntitySpriteDraw(renderTarget, new Vector2(10, 10) * i, screenRect, Color.White);
                spriteBatch.End();

                //Revert to normal spritebatch mode
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix); new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
                */
                #endregion
            }

            return false;
        }

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

        public void DrawLightning(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, Rectangle headRect, Rectangle bodyRect, Rectangle tailRect, float distance, float rotation = 0f, float scale = 1f, Color color = default)
        {

            //Defines an area where laser segments should actually draw, 100 pixels larger on each side than the screen
            Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);

            Rectangle bodyFrame = bodyRect;
            //bodyFrame.X = bodyRect.Width * currentFrame;
            Rectangle headFrame = headRect;
            headFrame.X *= headRect.Width * currentFrame;
            Rectangle tailFrame = tailRect;
            tailFrame.X *= tailRect.Width * currentFrame;

            frameCounter++;
            if (frameCounter >= frameDuration)
            {
                currentFrame++;
                frameCounter = 0;
                if (currentFrame > (frameCount - 1))
                {
                    currentFrame = 0;
                }
            }

            

            float i = 0;
            Vector2 diff = unit - start;
            diff.Normalize();

            Vector2 startPos = start - diff * 3;
            for (; i <= distance; i += (bodyFrame.Height) * scale)
            {
                Vector2 drawStart = startPos + i * diff;
                if (screenRect.Contains(drawStart.ToPoint()))
                {
                    Main.EntitySpriteDraw(texture, drawStart - Main.screenPosition, bodyFrame, color, rotation + MathHelper.PiOver2, new Vector2(bodyRect.Width * .5f, bodyRect.Height * .5f), scale, 0, 0);
                }
            }

            /*
            if (screenRect.Contains(startPos.ToPoint()))
            {
                Main.EntitySpriteDraw(texture, startPos - Main.screenPosition, headFrame, color, r, new Vector2(headRect.Width * .5f, headRect.Height * .5f), scale, 0, 0);
            }
            startPos += (unit * (headRect.Height) * scale);
            i -= (LaserTextureBody.Height) * scale;
            i += (LaserTextureTail.Height + 3) * scale; //Slightly fudged, need to find out why the laser tail is still misaligned for certain texture sizes
            startPos = startPos + i * unit;

            if (screenRect.Contains(startPos.ToPoint()))
            {
                Main.EntitySpriteDraw(texture, startPos - Main.screenPosition, tailFrame, color, r, new Vector2(tailRect.Width * .5f, tailRect.Height * .5f), scale, 0, 0);
            }*/
            }


        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if(branches == null || branches.Count == 0 || branches[0].Count == 0)
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

            return collides;
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
