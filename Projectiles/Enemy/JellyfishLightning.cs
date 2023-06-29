using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{

    public class JellyfishLightning : GenericLaser
    {

        //Titled "EnemyLightningStrike", but could also be used for player projectiles (and indeed is right now).
        //Warning: Highly experimental. Many commented out things are unfinished and do not work yet, others are incomplete.
        //What I would *really* like to do is draw the lightning strike as a chain of triangles angled precisely so there aren't any overlaps or breaks in its texture. As with everything here though, that's easier said than done.
        //Currently it just drwas a bunch of squares segments that intersect and don't mesh well at the edge of each joint. This is advanced level cringe, but it displays the concept well enough.
        //DrawPrims function works, and is using a sample list of verticies to draw a sample object.
        //The next thing to do is write the code to convert the stored coordinates of each joint into a list of verticies to send to the GPU.

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Laser");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
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
            LaserName = "Bioelectric Discharge";

            CastLight = true;
        }

        public static SoundStyle ThunderSoundStyle = new SoundStyle("Terraria/Sounds/Thunder_0");
        public SoundStyle ThunderSound = new SoundStyle("Terraria/Sounds/Thunder_0");
        public override void AI()
        {
            System.Diagnostics.Stopwatch thisWatch = new System.Diagnostics.Stopwatch();
            thisWatch.Start();
            if (IsAtMaxCharge)
            {
                displayDuration--;
            }

            if (branches == null)
            {
                randomness = 25;
                SetLaserPosition();
            }


            if (Projectile.timeLeft - 120 > 0)
            {
                float radius = (Projectile.timeLeft - 120);
                for (int j = 0; j < 2; j++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(radius, radius);
                    Vector2 dustPos = Projectile.Center + dir;
                    Vector2 dustVel = new Vector2(3, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);
                    Dust.NewDustPerfect(dustPos, DustID.FireworkFountain_Blue, dustVel, 200, default, 0.8f).noGravity = true;
                }
            }

            base.ChargeLaser();


            Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);
            int dustSpawned = 0;

            //Dust along lightning lines            
            if (FiringTimeLeft == 28)
            {
                int dustCount = 4;
                if (branches != null && branches.Count > 0)
                {
                    float pitch = Main.rand.NextFloat(-0.2f, 0.2f);
                    //Terraria.Audio.SoundEngine.PlaySound(ThunderSoundStyle with { Volume = 0.4f, Pitch = pitch }, branches[0][0]);
                    //Terraria.Audio.SoundEngine.PlaySound(ThunderSoundStyle with { Volume = 0.4f, Pitch = pitch }, branches[0][branches[0].Count / 2]);
                    //Terraria.Audio.SoundEngine.PlaySound(ThunderSoundStyle with { Volume = 0.4f, Pitch = pitch }, branches[0][branches[0].Count - 1]);

                    for (int i = 0; i < branches.Count; i++)
                    {
                        if (branches[i].Count > 0)
                        {
                            if (FastContainsPoint(screenRect, branches[i][0]) || i == 0)
                            {
                                for (int j = 0; j < branches[i].Count - 1; j++)
                                {

                                    Vector2 diff = branches[i][j + 1] - branches[i][j];
                                    diff /= dustCount;
                                    float lerpPercent = 0.8f * ((float)j / ((float)branches[i].Count - 1f));

                                    float scale = 1.7f;
                                    if (i == 0)
                                    {
                                        scale = 2.2f;
                                        lerpPercent = 0;
                                    }

                                    
                                    for (int k = 0; k < dustCount; k++)
                                    {
                                        dustSpawned++;
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

            if (FiringTimeLeft == 28)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active && !Main.player[i].dead && !Main.player[i].immune)
                        CustomCollision(Main.player[i]);
                }
            }

            thisWatch.Stop();
            //if (thisWatch.ElapsedMilliseconds > 1)
            if (FiringTimeLeft == 28)
            {
                //Main.NewText("AI: " + thisWatch.Elapsed);
                int branchCount = 0;
                for (int i = 0; i < branches.Count; i++)
                {
                    for (int j = 0; j < branches[i].Count - 1; j++)
                    {
                        branchCount++;
                    }
                }

                //Main.NewText("Branches: " + branchCount);

                int dustCounter = 0;
                for(int i = 0; i < Main.maxDust; i++)
                {
                    if (Main.dust[i].active)
                    {
                        dustCounter++;
                    }
                }
                //Main.NewText("dustCounter: " + dustCounter);
            }
        }

        public bool randomized = false;
        List<List<Vector2>> branches;
        List<List<float>> branchAngles;
        List<List<float>> branchLengths;
        public int segmentCount = 30;
        public float segmentLength = 60;
        public float randomness = 150;
        bool positionSet = false;
        //public float initialAngleLimit = MathHelper.ToRadians(75); //Can diverge up to 75 degrees from 
        private void SetLaserPosition()
        {
            segmentCount = 30;
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
                            if (Main.rand.NextBool(2) && j > 5)
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

        public RenderTarget2D tempTarget;
        public RenderTarget2D lightningTarget;
        public override bool PreDraw(ref Color lightColor)
        {
            //Don't draw anything if it hasn't generated the branches
            if (branches == null || branches.Count == 0 || lightningTarget == null)
            {
                return false;
            }

            //Draw the target
            float scaleFactor = (float)FiringTimeLeft / (float)FiringDuration;
            Color color = Color.White * scaleFactor * scaleFactor;

            //Make it a bit transparent and desaturated if it's just the targeting lines
            if (!IsAtMaxCharge)
            {
                color = Color.Gray * 0.5f;
            }

            //Restart the spritebatch so the shader can be applied to it
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            if (lightningEffect == null)
            {
                lightningEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/LightningLine", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            lightningEffect.Parameters["shaderColor"].SetValue(Color.Blue.ToVector3());
            if (IsAtMaxCharge)
            {
                lightningEffect.Parameters["active"].SetValue(1);
                lightningEffect.Parameters["fadeOut"].SetValue(FiringTimeLeft / 28f);
            }
            else
            {
                lightningEffect.Parameters["active"].SetValue(.2f);
                lightningEffect.Parameters["fadeOut"].SetValue(1);
            }
            lightningEffect.Parameters["noiseTex"].SetValue(tsorcRevamp.NoiseVoronoi);
            lightningEffect.Parameters["mulColor"].SetValue(color.R / 255f);

            lightningEffect.CurrentTechnique.Passes[0].Apply();

            //Draw the rendertarget with the shader
            float rotato = (branches[0][branches[0].Count - 1] - branches[0][0]).ToRotation();
            Rectangle drawRect = new Rectangle(0, 0, (int)(lightningTarget.Width * Charge / MaxCharge), lightningTarget.Height);
            if (IsAtMaxCharge)
            {
                drawRect.Width = lightningTarget.Width;
            }
            Main.spriteBatch.Draw(lightningTarget, branches[0][0] - Main.screenPosition, drawRect, color, rotato, new Vector2(lightningXOffset, lightningTarget.Height / 2), 1, SpriteEffects.None, 0);

            //Restart the spritebatch so the shader doesn't get applied to the rest of the game
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);

            return false;
        }

        Vector2 lightningMaxDimensions = new Vector2(2500, 700);
        public static Effect lightningEffect;
        public static Effect blurEffect;
        public void CreateRenderTarget()
        {
            //Store a reference to the graphics device to simplify code
            GraphicsDevice device = Main.graphics.GraphicsDevice;

            //Create a rendertarget. Instead of drawing all 200 lightning branches every frame, we will draw them once and store the results in this.
            //Once that is done, we can simply draw this one rendertarget to display the full lightning strike
            tempTarget = new RenderTarget2D(device, (int)lightningMaxDimensions.X, (int)lightningMaxDimensions.Y, false, device.PresentationParameters.BackBufferFormat, device.PresentationParameters.DepthStencilFormat, device.PresentationParameters.MultiSampleCount, RenderTargetUsage.PreserveContents);

            //Set the device target to the new rendertarget
            device.SetRenderTarget(tempTarget);

            //Clear it, so that whatever was previously stored on the backbuffer (like other lightning) doesn't get put in this target
            device.Clear(Color.Transparent);

            //Start a new "default" texture-sorted spritebatch
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            //Activate the shader to draw it properly
            if (lightningEffect == null)
            {
                lightningEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/LightningLine", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            lightningEffect.Parameters["active"].SetValue(-2);
            lightningEffect.CurrentTechnique.Passes[0].Apply();

            //Draw the lightning
            DrawSegments();

            //End the spritebatch
            Main.spriteBatch.End();

            //Re-set the old bindings
            device.SetRenderTarget(null);

            //Then draw it to a second rendertarget to blur it by applying a second shader in the process
            lightningTarget = new RenderTarget2D(device, (int)lightningMaxDimensions.X, (int)lightningMaxDimensions.Y, false, device.PresentationParameters.BackBufferFormat, device.PresentationParameters.DepthStencilFormat, device.PresentationParameters.MultiSampleCount, RenderTargetUsage.PreserveContents);
            device.SetRenderTarget(lightningTarget);
            device.Clear(Color.Transparent);
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
                        DrawLightning(TransparentTextureHandler.TransparentTextures[LaserTexture], branches[i][j],
                                branchLengths[i][j], branchAngles[i][j]);
                    }
                }
            }
        }

        float lightningXOffset = 50;
        public void DrawLightning(Texture2D texture, Vector2 start, float distance, float rotation = 0f)
        {
            float rotato = (branches[0][branches[0].Count - 1] - branches[0][0]).ToRotation();
            start -= branches[0][0];
            start = start.RotatedBy(-rotato);
            start.X += lightningXOffset;
            start.Y += lightningMaxDimensions.Y / 2;

            float drawScale = 1.5f;
            Rectangle drawRect = new Rectangle(0, 0, (int)(distance * 1.4f / drawScale), 12);
            Vector2 drawOrigin = drawRect.Size() / 2;
            drawOrigin.X = distance / drawScale;
            Main.EntitySpriteDraw(texture, start, drawRect, Color.White, rotation - rotato, drawOrigin, drawScale, 0, 0);
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
            if (branches == null || branches.Count == 0 || branches[0].Count == 0)
            {
                return;
            }

            float point = 0;

            Vector2 topLeft = targetHitbox.TopLeft();
            Vector2 size = targetHitbox.Size();

            for (int i = 0; i < branches.Count; i++)
            {
                if (branches[i].Count > 0)
                {
                    for (int j = 0; j < branches[i].Count - 1; j++)
                    {
                        if (Collision.CheckAABBvLineCollision(topLeft, size, branches[i][j], branches[i][j + 1], 10, ref point))
                        {
                            CanHitPlayer(target);
                            return;
                        }
                    }
                }
            }

            if (Collision.CheckAABBvLineCollision(topLeft, size, branches[0][0], branches[0][branches[0].Count - 1], 10, ref point))
            {
                CanHitPlayer(target);
            }

        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {            
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
            DelegateMethods.tileCutIgnore = TileID.Sets.TileCutIgnore.None;

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

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!IsAtMaxCharge)
            {
                modifiers.FinalDamage /= 3;
            }
            target.AddBuff(BuffID.Electrified, 300);
            target.AddBuff(BuffID.Slow, 150);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
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
