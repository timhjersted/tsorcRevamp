using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triad
{
    class CataluminanceTrail : Projectiles.VFX.DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Illuminant Trail");
        }
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 99999999;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.hide = true;

            trailWidth = 45;
            trailPointLimit = 900;
            trailMaxLength = 9999999;
            collisionPadding = 50;
            NPCSource = true;           
            trailCollision = true;
            collisionFrequency = 5;
            noFadeOut = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CataluminanceTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        float timer = 0;
        float transitionTimer = 0;
        public bool FinalStandMode = false;
        bool GreenMode = false; //It's green now. That's it's attack.
        public override void AI()
        {
            if (Projectile.ai[0] == 2)
            {
                Projectile.ai[0] = 1;
                GreenMode = true;
            }

            if (Projectile.ai[0] == 5)
            {
                FinalStandMode = true;
            }            

            if (FinalStandMode)
            {
                if (timer == 0)
                {
                    fadeOut = 0;
                    trailPositions = new List<Vector2>();
                    trailRotations = new List<float>();
                    for(int i = 0; i < 905; i++)
                    {
                        float angle = MathHelper.TwoPi * i / 900f;
                        trailPositions.Add(Projectile.Center + new Vector2(1350, 0).RotatedBy(angle));
                        trailRotations.Add(angle + MathHelper.PiOver2);
                    }
                    trailCurrentLength = CalculateLength();
                }
                timer++;

                if (!NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Cataluminance>()))
                {
                    fadeOut -= 0.01f;
                    if (fadeOut <= 0)
                    {
                        Projectile.Kill();
                    }
                }
                else if(fadeOut < 1)
                {
                    fadeOut += 0.01f;
                }
            }
            else
            {
                if (hostNPC != null && hostNPC.active && hostNPC.life < hostNPC.lifeMax * 2f / 3f)
                {
                    transitionTimer++;
                }
                if (timer == 0)
                {
                    if (hostNPC != null && hostNPC.active && hostNPC.life < hostNPC.lifeMax / 2f)
                    {
                        trailColor = new Color(1f, 0.7f, 0.85f);
                        PreSetTrail = true;
                    }
                }
                timer++;
                //A phase is 900 seconds long
                //Once that is over, stop adding new positions
                if (timer <= 899)
                {
                    base.AI();
                }

                //Once the boss is all the way back to that stage again, then start removing the old positions
                bool finalStandInitiated = false;
                int? cat = UsefulFunctions.GetFirstNPC(ModContent.NPCType<NPCs.Bosses.Cataluminance>());
                if (cat != null) {
                    if (Main.npc[cat.Value].life <= 1000)
                    {
                        finalStandInitiated = true;
                    }
                }
                if (timer > 2700 || cat == null || finalStandInitiated)
                {
                    Projectile.damage = 0;
                    fadeOut -= 1f / 120f;
                    if(fadeOut <= 0)
                    {
                        Projectile.Kill();
                    }
                }
            }
        }

        public override float CollisionWidthFunction(float progress)
        {
            return WidthFunction(progress) - 55;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        bool PreSetTrail = false;
        Color trailColor = new Color(0.2f, 0.7f, 1f);
        float timeFactor = 0;
        public override void SetEffectParameters(Effect effect)
        {
            visualizeTrail = false;
            collisionPadding = 8;
            collisionEndPadding = trailPositions.Count / 24;
            trailWidth = 100;

            //I do it like this so it retains its color state even if the host NPC dies or despawns
            if (!FinalStandMode && !PreSetTrail && hostNPC != null && hostNPC.active && hostNPC.life < (hostNPC.lifeMax * 2f / 3f) && transitionTimer <= 120)
            {
                trailColor = Color.Lerp(new Color(0.2f, 0.7f, 1f), new Color(1f, 0.7f, 0.85f), transitionTimer / 120);
            }

            if (FinalStandMode)
            {
                collisionEndPadding = 0;
                collisionPadding = 0;
                trailColor = new Color(1f, 0.7f, 0.85f);
            }

            if (GreenMode)
            {
                trailColor = Color.GreenYellow;
            }

            timeFactor++;

            //Shifts its color slightly over time
            Vector3 hslColor = Main.rgbToHsl(trailColor);
            hslColor.X += 0.03f * (float)Math.Cos(timeFactor / 25f);
            Color rgbColor = Main.hslToRgb(hslColor);

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTextureWavy);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["finalStand"].SetValue(FinalStandMode.ToInt());
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["shaderColor2"].SetValue(new Color(0.2f, 0.7f, 1f).ToVector4());
            effect.Parameters["length"].SetValue(trailCurrentLength);
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

    }
}