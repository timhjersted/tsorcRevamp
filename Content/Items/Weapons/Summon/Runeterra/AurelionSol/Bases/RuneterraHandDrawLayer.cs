using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Bases;

public class RuneterraHandDrawLayer : PlayerDrawLayer
{

    public override Position GetDefaultPosition()
    {
        return new AfterParent(PlayerDrawLayers.HandOnAcc);
    }

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        return true;
    }
    
    protected override void Draw(ref PlayerDrawSet drawInfo)
        {
            tsorcRevampPlayer modPlayer = drawInfo.drawPlayer.GetModPlayer<tsorcRevampPlayer>();
            Player drawPlayer = drawInfo.drawPlayer;
            
            if (drawPlayer.HeldItem.type == ModContent.ItemType<ScorchingPoint>() && drawPlayer.HeldItem.type != 0)
            {
                //1) Get texture
                Texture2D scorchingPointTexture = (Texture2D)ModContent.Request<Texture2D>( UsefulFunctions.RefactorableFilepath(typeof(ScorchingPoint)) + "_Hand");

                //2) Get the players hand position
                Vector2 drawPosition = drawPlayer.GetFrontHandPosition(Player.CompositeArmStretchAmount.None, drawPlayer.itemRotation);

                //3) Shift the draw position over a bit so it looks right. Feel free to tune this.
                Vector2 handOffset = new Vector2(12, 2);

                //Flip it if the player is facing left
                SpriteEffects effect = SpriteEffects.None;
                if (drawPlayer.direction == -1)
                {
                    effect = SpriteEffects.FlipHorizontally;
                    handOffset.X *= -1;
                }

                //If the player is on a pully, offset and rotate the sprite to match
                float drawRotation = 0;
                if (drawInfo.drawPlayer.pulley)
                {
                    //If their hand is pointed straight up, use these values
                    if (drawPlayer.pulleyDir == 2)
                    {
                        drawRotation = -MathHelper.PiOver2;
                        handOffset = new Vector2(0, -12);

                        //And flip it if they're looking left
                        if (drawPlayer.direction == -1)
                        {
                            effect = SpriteEffects.FlipVertically;
                        }
                    }
                    else
                    {
                        //If their hand is angled, use these ones
                        drawRotation = -MathHelper.PiOver4;
                        handOffset = new Vector2(10, -10);

                        //And flip it if they're looking left
                        if (drawPlayer.direction == -1)
                        {
                            effect = SpriteEffects.FlipVertically;
                            drawRotation -= MathHelper.PiOver2;
                            handOffset.X *= -1;
                        }
                    }
                }

                //Add the offset to the position
                drawPosition += handOffset;

                //3) Set up its sprite sheet source and origin variables
                Rectangle sourceRectangle = new Rectangle(0, 0, scorchingPointTexture.Width, scorchingPointTexture.Height);
                Vector2 origin = sourceRectangle.Size() / 2f;

                //4) Call the draw function with all the info
                drawInfo.DrawDataCache.Add(new DrawData(scorchingPointTexture, drawPosition - Main.screenPosition, sourceRectangle, Color.White, drawRotation, origin, 1, effect, 0));
            }
            if (drawPlayer.HeldItem.type == ModContent.ItemType<InterstellarVesselGauntlet>() && drawPlayer.HeldItem.type != 0)
            {
                //1) Get texture
                Texture2D interstellarVesselGauntletTexture = (Texture2D)ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(InterstellarVesselGauntlet)) + "_Hand");

                //2) Get the players hand position
                Vector2 drawPosition = drawPlayer.GetFrontHandPosition(Player.CompositeArmStretchAmount.None, drawPlayer.itemRotation);

                //3) Shift the draw position over a bit so it looks right. Feel free to tune this.
                Vector2 handOffset = new Vector2(12, 2);

                //Flip it if the player is facing left
                SpriteEffects effect = SpriteEffects.None;
                if (drawPlayer.direction == -1)
                {
                    effect = SpriteEffects.FlipHorizontally;
                    handOffset.X *= -1;
                }

                //If the player is on a pully, offset and rotate the sprite to match
                float drawRotation = 0;
                if (drawInfo.drawPlayer.pulley)
                {
                    //If their hand is pointed straight up, use these values
                    if (drawPlayer.pulleyDir == 2)
                    {
                        drawRotation = -MathHelper.PiOver2;
                        handOffset = new Vector2(0, -12);

                        //And flip it if they're looking left
                        if (drawPlayer.direction == -1)
                        {
                            effect = SpriteEffects.FlipVertically;
                        }
                    }
                    else
                    {
                        //If their hand is angled, use these ones
                        drawRotation = -MathHelper.PiOver4;
                        handOffset = new Vector2(10, -10);

                        //And flip it if they're looking left
                        if (drawPlayer.direction == -1)
                        {
                            effect = SpriteEffects.FlipVertically;
                            drawRotation -= MathHelper.PiOver2;
                            handOffset.X *= -1;
                        }
                    }
                }

                //Add the offset to the position
                drawPosition += handOffset;

                //3) Set up its sprite sheet source and origin variables
                Rectangle sourceRectangle = new Rectangle(0, 0, interstellarVesselGauntletTexture.Width, interstellarVesselGauntletTexture.Height);
                Vector2 origin = sourceRectangle.Size() / 2f;

                //4) Call the draw function with all the info
                drawInfo.DrawDataCache.Add(new DrawData(interstellarVesselGauntletTexture, drawPosition - Main.screenPosition, sourceRectangle, Color.White, drawRotation, origin, 1, effect, 0));
            }
            if (drawPlayer.HeldItem.type == ModContent.ItemType<CenterOfTheUniverse>() && drawPlayer.HeldItem.type != 0)
            {
                //1) Get texture
                Texture2D centerOfTheUniverseTexture = (Texture2D)ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(CenterOfTheUniverse)) + "_Hand");

                //2) Get the players hand position
                Vector2 drawPosition = drawPlayer.GetFrontHandPosition(Player.CompositeArmStretchAmount.None, drawPlayer.itemRotation);

                //3) Shift the draw position over a bit so it looks right. Feel free to tune this.
                Vector2 handOffset = new Vector2(12, 2);

                //Flip it if the player is facing left
                SpriteEffects effect = SpriteEffects.None;
                if (drawPlayer.direction == -1)
                {
                    effect = SpriteEffects.FlipHorizontally;
                    handOffset.X *= -1;
                }

                //If the player is on a pully, offset and rotate the sprite to match
                float drawRotation = 0;
                if (drawInfo.drawPlayer.pulley)
                {
                    //If their hand is pointed straight up, use these values
                    if (drawPlayer.pulleyDir == 2)
                    {
                        drawRotation = -MathHelper.PiOver2;
                        handOffset = new Vector2(0, -12);

                        //And flip it if they're looking left
                        if (drawPlayer.direction == -1)
                        {
                            effect = SpriteEffects.FlipVertically;
                        }
                    }
                    else
                    {
                        //If their hand is angled, use these ones
                        drawRotation = -MathHelper.PiOver4;
                        handOffset = new Vector2(10, -10);

                        //And flip it if they're looking left
                        if (drawPlayer.direction == -1)
                        {
                            effect = SpriteEffects.FlipVertically;
                            drawRotation -= MathHelper.PiOver2;
                            handOffset.X *= -1;
                        }
                    }
                }

                //Add the offset to the position
                drawPosition += handOffset;

                //3) Set up its sprite sheet source and origin variables
                Rectangle sourceRectangle = new Rectangle(0, 0, centerOfTheUniverseTexture.Width, centerOfTheUniverseTexture.Height);
                Vector2 origin = sourceRectangle.Size() / 2f;

                //4) Call the draw function with all the info
                drawInfo.DrawDataCache.Add(new DrawData(centerOfTheUniverseTexture, drawPosition - Main.screenPosition, sourceRectangle, Color.White, drawRotation, origin, 1, effect, 0));
            }
        }
}