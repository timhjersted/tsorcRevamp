
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    class RealityFracture : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("RealityCrack");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
        }

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 48;
            Projectile.height = 62;
            Projectile.penetrate = -1;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 999;
        }

        public string filterIndex;

        float effectTimer = 0;
        float effectLimit = 60;
        float newCrackDelay = 0;
        bool initialized = false;
        bool newCrack = false;
        public override void AI()
        {
            Projectile.timeLeft++;

            if(!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>()))
            {
                Projectile.Kill();
                return;
            }

            if (Projectile.ai[0] == 1)
            {
                effectLimit = 250;
            }

            //Get an unused copy of the effect from the scene filter dictionary, or create one if they're all in use
            if (!initialized && Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(SoundID.Shatter with { Volume = 0.5f });
                int index = 0;
                do
                {
                    string currentIndex = "tsorcRevamp:realityfracture" + index;

                    //If there is an unused loaded shader, then start using it instead of creating a new one
                    if (Filters.Scene[currentIndex] != null && !Filters.Scene[currentIndex].Active)
                    {
                        Filters.Scene.Activate(currentIndex, Projectile.Center).GetShader().UseTargetPosition(Projectile.Center);
                        filterIndex = currentIndex;
                        tsorcRevampWorld.boundShaders.Add(filterIndex);
                        initialized = true;
                        break;
                    }

                    //If we have reached the point no more entries exist, then create a new one
                    if (Filters.Scene[currentIndex] == null)
                    {
                        Filters.Scene[currentIndex] = new Filter(new ScreenShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/RealityCrack", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "RealityCrackPass"), EffectPriority.VeryHigh);
                        filterIndex = currentIndex;
                        initialized = true;
                        break;
                    }

                    //If more than 10 are already active at once, give up and just kill the shockwave instead of creating yet another one.
                    if (index >= 20)
                    {
                        initialized = true;
                        Projectile.Kill();
                        break;
                    }
                    index++;

                } while (index < 20);
            }
            if (filterIndex == null && Main.netMode != NetmodeID.Server)
            {
                Projectile.Kill();
                return;
            }

            //Apply it
            if (Main.netMode != NetmodeID.Server && !Filters.Scene[filterIndex].IsActive() && renTarget != null && !renTarget.IsDisposed)
            {
                Filters.Scene.Activate(filterIndex, Projectile.Center).GetShader().UseTargetPosition(storedPosition).UseProgress(1).UseOpacity(1).UseIntensity(1).UseColor(Color.White.ToVector3()).UseImage(renTarget, samplerState: SamplerState.PointClamp).UseDirection(-Projectile.velocity);
            }

            if (Main.netMode != NetmodeID.Server && Filters.Scene[filterIndex].IsActive() && renTarget != null && !renTarget.IsDisposed)
            {
                Filters.Scene[filterIndex].GetShader().UseTargetPosition(storedPosition).UseOpacity(1).UseProgress(0).UseIntensity(1).UseColor(Color.White.ToVector3()).UseImage(renTarget, samplerState: SamplerState.PointClamp).UseDirection(-Projectile.velocity);
            }


            if (effectTimer > effectLimit)
            {
                Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.netMode != NetmodeID.Server && filterIndex != null && Filters.Scene[filterIndex].IsActive())
            {
                //Set its 'useimage' to this so that it doesn't hold onto a reference to the soon to be disposed rendertarget
                Filters.Scene[filterIndex].GetShader().UseOpacity(0).UseImage(tsorcRevamp.NoiseTurbulent);
                Filters.Scene[filterIndex].Deactivate();
                tsorcRevampWorld.boundShaders.Remove(filterIndex);
            }
            if (renTarget != null && !renTarget.IsDisposed)
            {
                renTarget.Dispose();
            }
        }


        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }


        Vector2 storedPosition;
        public RenderTarget2D renTarget;
        public RenderTarget2D tempTarget;
        public Vector2 newCrackLocation = Vector2.Zero;
        Vector2 targetOffset;
        public static Effect preRenderEffect;
        public static Effect blurEffect;
        public void CreateRenderTarget()
        {
            if (newCrackLocation == Vector2.Zero)
            {
                return;
            }
            newCrackLocation.Normalize();
            Projectile.velocity = newCrackLocation;
            newCrackLocation = Vector2.Zero;

            //Store a reference to the graphics device to simplify code
            GraphicsDevice device = Main.graphics.GraphicsDevice;

            //Create a rendertarget. Instead of drawing all 200 lightning branches every frame, we will draw them once and store the results in this.
            //Once that is done, we can simply draw this one rendertarget to display the full lightning strike
            if (tempTarget == null || tempTarget.IsDisposed)
            {
                tempTarget = new RenderTarget2D(device, device.PresentationParameters.BackBufferWidth * 2, device.PresentationParameters.BackBufferHeight * 3, false, device.PresentationParameters.BackBufferFormat, device.PresentationParameters.DepthStencilFormat, device.PresentationParameters.MultiSampleCount, RenderTargetUsage.PreserveContents);

                targetOffset = tempTarget.Size() / 2;
                storedPosition = Main.screenPosition - targetOffset;
            }

            device.Clear(Color.Transparent);

            //Set the device target to the new rendertarget
            device.SetRenderTarget(tempTarget);

            //Clear it, so that whatever was previously stored on the backbuffer (like other lightning) doesn't get put in this target

            //Start a new "default" texture-sorted spritebatch
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);
            if (preRenderEffect == null)
            {
                preRenderEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/RealityCrackPreRender", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            preRenderEffect.Parameters["xOffset"].SetValue((Projectile.velocity.X + 1) / 2f);
            preRenderEffect.Parameters["yOffset"].SetValue((Projectile.velocity.Y + 1) / 2f);
            preRenderEffect.CurrentTechnique.Passes[0].Apply();

            CreateLightningSegments();

            //Draw the lightning
            DrawSegments();

            //End the spritebatch
            Main.spriteBatch.End();



            //Store the current screen position at time of drawing so it can be used to calculate where to draw the lightning later

            //Re-set the old bindings
            //If I do this instead, then everything drawn *before* the lightning (tiles, backrounds, etc) is blacked out this frame
            //device.SetRenderTargets(bindings);
            device.SetRenderTarget(null);

            //Then draw it to a second rendertarget to blur it by applying a second shader in the process
            if (renTarget == null || renTarget.IsDisposed)
            {
                renTarget = new RenderTarget2D(device, device.PresentationParameters.BackBufferWidth * 2, device.PresentationParameters.BackBufferHeight * 3, false, device.PresentationParameters.BackBufferFormat, device.PresentationParameters.DepthStencilFormat, device.PresentationParameters.MultiSampleCount, RenderTargetUsage.PreserveContents);

                targetOffset = renTarget.Size() / 2;
                storedPosition = Main.screenPosition - targetOffset;
            }
            device.Clear(Color.Transparent);
            device.SetRenderTarget(renTarget);
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            if (blurEffect == null)
            {
                blurEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/BlurEffect", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            blurEffect.CurrentTechnique.Passes[0].Apply();

            Main.spriteBatch.Draw(tempTarget, Vector2.Zero, new Rectangle(0, 0, tempTarget.Width, tempTarget.Height), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tempTarget, Vector2.Zero, new Rectangle(0, 0, tempTarget.Width, tempTarget.Height), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tempTarget, Vector2.Zero, new Rectangle(0, 0, tempTarget.Width, tempTarget.Height), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);
            Main.spriteBatch.Draw(tempTarget, Vector2.Zero, new Rectangle(0, 0, tempTarget.Width, tempTarget.Height), Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, 0);


            Main.spriteBatch.End();

            device.SetRenderTarget(null);


            //Clean up the temp target to free up memory
            if (tempTarget != null && !tempTarget.IsDisposed)
            {
                tempTarget.Dispose();
            }
            if (tempTarget != null)
            {
                tempTarget = null;
            }
        }
        public void DrawSegments()
        {

            if (branches != null)
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
                        DrawLightning(tsorcRevamp.NoiseSplotchy, branches[i][j],
                                branches[i][j + 1], new Rectangle(0, 0, 10, 4), branchLengths[i][j], branchAngles[i][j], scale * 1, Color.White);
                    }
                }
            }
        }
        public void DrawLightning(Texture2D texture, Vector2 start, Vector2 unit, Rectangle bodyRect, float distance, float rotation = 0f, float scale = 1f, Color color = default)
        {

            float i = 0;
            Vector2 diff = unit - start;
            diff.Normalize();

            Vector2 startPos = start - diff * 3;
            for (; i <= distance; i += (bodyRect.Height) * scale)
            {
                Vector2 drawStart = startPos + i * diff;
                Main.EntitySpriteDraw(texture, drawStart - Main.screenPosition + targetOffset, bodyRect, color, rotation + MathHelper.PiOver2, new Vector2(bodyRect.Width * .5f, bodyRect.Height * .5f), scale, 0, 0);
            }
        }

        public bool randomized = false;
        public List<List<Vector2>> branches;
        List<List<float>> branchAngles;
        List<List<float>> branchLengths;
        public int segmentCount = 80;
        public float segmentLength = 60;
        public float randomness = 50;
        //public float initialAngleLimit = MathHelper.ToRadians(75); //Can diverge up to 75 degrees from 
        private void CreateLightningSegments()
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
                        if (Main.rand.NextBool(3) && j > 5)
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

            for (int i = 0; i < currentBranch.Count - 1; i++)
            {
                currentAngles.Add((currentBranch[i] - currentBranch[i + 1]).ToRotation());
                currentLengths.Add(Vector2.Distance(currentBranch[i], currentBranch[i + 1]));
            }


            return new Tuple<List<Vector2>, List<float>, List<float>>(currentBranch, currentAngles, currentLengths);
        }

        public override bool? CanDamage()
        {
            return false;
        }
    }
}