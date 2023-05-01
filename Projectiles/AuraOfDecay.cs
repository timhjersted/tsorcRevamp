using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles
{
    class AuraOfDecay : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Aura of Decay");
        }
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 400;
            Projectile.penetrate = -1;
            Projectile.hostile = false;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = NPC.immuneTime;

            trailWidth = 45;
            trailPointLimit = 2000;
            trailMaxLength = 9999999;
            NPCSource = true;
            trailCollision = true;
            noFadeOut = true;
            collisionFrequency = 2;
            trailWidth = 25;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CataluminanceTrail", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }

        float timer = 0;
        public bool FinalStandMode = false;
        public override void AI()
        {
            timer++;
            if (timer == 1)
            {
                Projectile.timeLeft = 800;
                fadeOut = 0;
                trailPositions = new List<Vector2>();
                trailRotations = new List<float>();
                for (int i = 0; i < 102; i++)
                {
                    float angle = (MathHelper.Pi / 2.5f) + MathHelper.TwoPi * i / 95;
                    trailPositions.Add(Projectile.Center + new Vector2(5, 0).RotatedBy(angle));
                    trailRotations.Add(angle + MathHelper.PiOver2);
                }
                trailCurrentLength = CalculateLength();
            }
            else
            {
                for (int i = 0; i < trailPositions.Count; i++)
                {
                    trailPositions[i] += new Vector2(10, 0).RotatedBy(trailRotations[i] - MathHelper.PiOver2);
                }

                for (int i = 0; i < trailPositions.Count; i++)
                {
                    if (i < trailPositions.Count - 1 && Vector2.Distance(trailPositions[i], trailPositions[i + 1]) > 20 && trailPositions.Count < trailPointLimit)
                    {
                        trailPositions.Insert(i + 1, (trailPositions[i] + trailPositions[i + 1]) / 2f);
                        trailRotations.Insert(i + 1, (trailRotations[i] + trailRotations[i + 1]) / 2f);
                    }
                }
            }
        }
        

        public override float CollisionWidthFunction(float progress)
        {
            return 25;
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        /*
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.Poisoned, 120);
            target.AddBuff(BuffID.Venom, 120);
        }*/

        bool PreSetTrail = false;
        Color trailColor = new Color(0.2f, 0.7f, 1f);
        float timeFactor = 0;
        public override void SetEffectParameters(Effect effect)
        {
            visualizeTrail = false;

            timeFactor++;

            //Shifts its color slightly over time
            Vector3 hslColor = Main.rgbToHsl(Color.YellowGreen);
            hslColor.X += 0.03f * (float)Math.Cos(timeFactor / 25f);
            Color rgbColor = Main.hslToRgb(hslColor);

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.tNoiseTexture3);
            effect.Parameters["fadeOut"].SetValue(0.2f);
            effect.Parameters["finalStand"].SetValue(FinalStandMode.ToInt());
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(rgbColor.ToVector4());
            effect.Parameters["shaderColor2"].SetValue(new Color(0.2f, 0.7f, 1f).ToVector4());
            effect.Parameters["length"].SetValue(trailCurrentLength);
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

    }
}