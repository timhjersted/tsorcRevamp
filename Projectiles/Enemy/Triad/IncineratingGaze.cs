using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Utilities;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triad;

public class IncineratingGaze : ModProjectile
{

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
    }
    public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

    public override void SetDefaults()
    {

        Projectile.friendly = false;
        Projectile.hostile = true;
        Projectile.penetrate = 50;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 586;
        Projectile.width = 10;
        Projectile.height = 250;
    }

    float chargeProgress;
    float laserWidth = 250;
    float firingTime = 216;

    //Sound Effect by Adam Wilson
    //https://www.youtube.com/watch?v=q_41f7Xp9_A
    SlotId soundSlotID;
    SoundStyle LaserSoundStyle = new SoundStyle("tsorcRevamp/Sounds/Custom/ChargeBeam") with { PlayOnlyIfFocused = false, MaxInstances = 0 };
    bool soundPaused;
    ActiveSound laserSound;
    public override void AI()
    {
        if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.RetinazerV2>()))
        {
            Projectile.timeLeft = 0;
            Projectile.Kill();
            Projectile.active = false;
            laserWidth = 1;
        }
        if(chargeProgress < firingTime)
        {
            if (chargeProgress == 0)
            {
                soundSlotID = SoundEngine.PlaySound(LaserSoundStyle, Projectile.Center);
            }
            chargeProgress++;
        }
        else
        {
            laserWidth += 60;
        }

        
        if (laserSound == null)
        {
            SoundEngine.TryGetActiveSound(soundSlotID, out laserSound);
        }
        else
        {
            if (SoundEngine.AreSoundsPaused && !soundPaused)
            {
                laserSound.Pause();
                soundPaused = true;
            }
            else if (!SoundEngine.AreSoundsPaused && soundPaused)
            {
                laserSound.Resume();
                soundPaused = false;
            }
            laserSound.Position = Main.LocalPlayer.Center;
        }

        //Stick to retinazer and rotate to face wherever it is looking            \
        bool validBoss = Main.npc[(int)Projectile.ai[0]].type == ModContent.NPCType<NPCs.Bosses.RetinazerV2>() || Main.npc[(int)Projectile.ai[0]].type == ModContent.NPCType<NPCs.Bosses.SpazmatismV2>() || Main.npc[(int)Projectile.ai[0]].type == ModContent.NPCType<NPCs.Bosses.Cataluminance>();
        if (Main.npc[(int)Projectile.ai[0]] != null && Main.npc[(int)Projectile.ai[0]].active && validBoss)
        {
            Projectile.rotation = Main.npc[(int)Projectile.ai[0]].rotation + MathHelper.PiOver2;
            Projectile.Center = Main.npc[(int)Projectile.ai[0]].Center + new Vector2(40, 0).RotatedBy(Projectile.rotation);
        }
        //If ret is dead then fade out
        else
        {
            if(Projectile.timeLeft > 130)
            {
                if (laserSound != null)
                {
                    laserSound.Stop();
                }
                Projectile.timeLeft = 130;
            }
        }

        //Cast light
        Vector3 colorVector = Color.OrangeRed.ToVector3() * 2f;
        
        Vector2 startPoint = Projectile.Center;
        Vector2 endpoint = Projectile.Center + Projectile.rotation.ToRotationVector2() * laserWidth;
        
        //Normal charging
        if (chargeProgress < (firingTime - 30))
        {
            colorVector *= chargeProgress / (firingTime - 30);
            endpoint = Projectile.Center + Projectile.rotation.ToRotationVector2() * 1000;
        }

        //Fade out before blast
        if (chargeProgress < firingTime && chargeProgress >= firingTime - 30)
        {
            float factor = (firingTime - chargeProgress) / 30f;
            colorVector *= factor * factor;
            endpoint = Projectile.Center + Projectile.rotation.ToRotationVector2() * 1000;
        }

        //Fade out at end
        if (Projectile.timeLeft < 130)
        {
            colorVector *= Projectile.timeLeft / 130f;
            colorVector *= Projectile.timeLeft / 130f;
        }
        DelegateMethods.v3_1 = colorVector;

        Utils.PlotTileLine(startPoint, endpoint, 100, DelegateMethods.CastLight);

        float point = 0;

        if (chargeProgress == firingTime && Projectile.timeLeft > 130)
        {
            //Collision (TODO: Update)
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    Rectangle targetHitbox = Main.player[i].Hitbox;
                    if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center,
                    Projectile.Center + Projectile.rotation.ToRotationVector2() * laserWidth, Projectile.height / 3f, ref point))
                    {
                        if (Main.GameUpdateCount % 2 == 0)
                        {
                            Rectangle randBox = Main.player[i].Hitbox;
                            randBox.X += (int)Main.rand.NextFloat(-16, 16);
                            randBox.Y += (int)Main.rand.NextFloat(-16, 16);
                            CombatText.NewText(randBox, Color.OrangeRed, 999999999, true);
                        }
                        Main.player[i].immune = false;
                        Main.player[i].statLife -= 999999999;
                        Main.player[i].KillMe(PlayerDeathReason.ByProjectile(999, Projectile.whoAmI), 999999999, 0);
                    }
                }
            }
        }
    }

    public static ArmorShaderData data;
    public static ArmorShaderData targetingData;
    public override bool PreDraw(ref Color lightColor)
    {
        Color laserColor = new Color(1.0f, 0.1f, 0.1f);

        if (Projectile.ai[1] == 1)
        {
            laserColor = Color.GreenYellow;
        }
        
        if (Projectile.ai[1] == 2)
        {
            laserColor = new Color(0.1f, 0.5f, 1f);
        }

        if (chargeProgress < firingTime)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //if (targetingData == null)
            {
                targetingData = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/IncineratingGazeTargeting", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "IncineratingGazeTargetingPass");
            }

            Rectangle targetingSourceRectangle = new Rectangle(0, 0, (int)10000, Projectile.height);

            //Pass relevant data to the shader via these parameters
            targetingData.UseTargetPosition(new Vector2(10000, 250));
            float targetingScaleUp = 1;
            if (chargeProgress <= 170)
            {
                targetingScaleUp = (float)Math.Pow(chargeProgress / firingTime, 0.2f);
            }
            else if(chargeProgress < firingTime - 20)
            {
                targetingScaleUp = (float)Math.Pow(1 - ((chargeProgress - (firingTime - 40)) / 20), 0.2f);
            }
            else
            {
                targetingScaleUp = 0;
            }

            targetingData.UseSaturation((targetingScaleUp / 1.2f));
            targetingData.UseOpacity(1);
            targetingData.UseColor(laserColor);
            //Apply the shader
            targetingData.Apply(null);

            SpriteEffects targetingSpriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                targetingSpriteEffects = SpriteEffects.FlipHorizontally;
            }

            Vector2 targetingOrigin = new Vector2(0, targetingSourceRectangle.Height / 2);

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTexture1, Projectile.Center - Main.screenPosition, targetingSourceRectangle, Color.White, Projectile.rotation, targetingOrigin, Projectile.scale, targetingSpriteEffects, 0);
        }

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        //Apply the shader, caching it as well
        //if (data == null)
        {
            data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/IncineratingGaze", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "IncineratingGazePass");
        }

        

        Rectangle sourceRectangle = new Rectangle(0, 0, (int)laserWidth, Projectile.height);

        //Pass relevant data to the shader via these parameters
        data.UseTargetPosition(new Vector2(laserWidth, 250));
        float scaleDown = 1;
        if (Projectile.timeLeft < 130)
        {
            scaleDown = Projectile.timeLeft / 130f;

        }
        if (chargeProgress <= 170)
        {
            scaleDown = (float)Math.Pow(chargeProgress / firingTime, 0.2f);
        }
        else if(chargeProgress < firingTime - 20)
        {
            scaleDown = (float)Math.Pow(1 - ((chargeProgress - (firingTime - 40)) / 20), 0.2f);
        }
        else if(chargeProgress < firingTime)
        {
            scaleDown = 0;
        }

        data.UseOpacity(scaleDown);
        data.UseColor(laserColor);
        //data.UseSecondaryColor(1, 1, Main.time);

        //Apply the shader
        data.Apply(null);

        SpriteEffects spriteEffects = SpriteEffects.None;
        if (Projectile.spriteDirection == -1)
        {
            spriteEffects = SpriteEffects.FlipHorizontally;
        }
        
        Vector2 origin = new Vector2(0, sourceRectangle.Height / 2);

        Main.EntitySpriteDraw(tsorcRevamp.tNoiseTexture1, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);


        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

        return false;
    }
}

public class MaliciousGaze : IncineratingGaze {}
public class BlindingGaze : IncineratingGaze {}