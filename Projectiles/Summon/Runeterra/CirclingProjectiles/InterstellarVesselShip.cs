using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Summon.Runeterra.CirclingProjectiles
{
    public class InterstellarVesselShip : RuneterraCirclingProjectiles
    {
        public override int ProjFrames => 1;
        public override int Width => 98;
        public override int Height => 50;
        public override int TrailWidth => 45;
        public override int TrailPointLimit => 900;
        public override int TrailMaxLength => 500; //333
        public override string EffectType => "tsorcRevamp/Effects/InterstellarVessel";
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Summon/InterstellarVessel/";
        public override int BuffType => ModContent.BuffType<InterstellarCommander>();
        public override int dustID => DustID.MartianSaucerSpark;
        public override string Texture => "tsorcRevamp/Projectiles/Summon/Runeterra/CirclingProjectiles/InterstellarVesselShip";
        public override void OnSpawn(IEntitySource source)
        {
            InterstellarVesselGauntlet.projectiles.Add(this);
        }
        public override void OnKill(int timeLeft)
        {
            InterstellarVesselGauntlet.projectiles.Remove(this);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[Projectile.owner];
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().SuperShockDuration > 0)
            {
                modifiers.SourceDamage += ScorchingPoint.SuperBurnDmgAmp / 100f;
            }
            if (owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
            {
                modifiers.SourceDamage += InterstellarVesselGauntlet.BoostDmgAmp / 100f;
                modifiers.FinalDamage.Flat += Math.Min(target.lifeMax / 3000, 150);
            }
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().ShockMarks >= 6)
            {
                modifiers.SetCrit();
                modifiers.CritDamage *= ScorchingPoint.MarkDetonationCritDmgAmp;
            }
        }
        public override void CustomOnHitNPC(NPC target)
        {
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().ShockMarks >= 6)
            {
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().ShockMarks = 0;
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().SuperShockDuration = ScorchingPoint.SuperBurnDuration;
                Dust.NewDust(Projectile.position, 20, 20, dustID, 1, 1, 0, default, 1.5f);
                SoundEngine.PlaySound(new SoundStyle(SoundPath + "MarkDetonation") with { Volume = InterstellarVesselGauntlet.SoundVolume * 1.2f });
            }
        }
        public override void SetEffectParameters(Effect effect)
        {
            trailWidth = 45;
            trailMaxLength = 500;

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseWavy);
            effect.Parameters["length"].SetValue(trailCurrentLength);
            float hostVel = 0;
            hostVel = Projectile.velocity.Length();
            float modifiedTime = 0.001f * hostVel;

            if (Main.gamePaused)
            {
                modifiedTime = 0;
            }
            samplePointOffset1.X += (modifiedTime * 2);
            samplePointOffset1.Y -= (0.001f);
            samplePointOffset2.X += (modifiedTime * 3.01f);
            samplePointOffset2.Y += (0.001f);

            samplePointOffset1.X += modifiedTime;
            samplePointOffset1.X %= 1;
            samplePointOffset1.Y %= 1;
            samplePointOffset2.X %= 1;
            samplePointOffset2.Y %= 1;
            collisionEndPadding = trailPositions.Count / 2;

            effect.Parameters["samplePointOffset1"].SetValue(samplePointOffset1);
            effect.Parameters["samplePointOffset2"].SetValue(samplePointOffset2);
            effect.Parameters["fadeOut"].SetValue(trailIntensity);
            effect.Parameters["speed"].SetValue(hostVel);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(new Color(0.8f, 0.6f, 0.2f).ToVector4());
            effect.Parameters["secondaryColor"].SetValue(new Color(0.005f, 0.05f, 1f).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
        public override float CollisionWidthFunction(float progress)
        {
            return WidthFunction(progress) - 35;
        }
        public override void CustomCheckActive()
        {
            InterstellarVesselGauntlet.projectiles.Clear();
        }

        public static Texture2D texture;
        public static Texture2D glowTexture;
        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);
            if (additiveContext)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                if (texture == null || texture.IsDisposed)
                {
                    texture = (Texture2D)ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad);
                }
                if (glowTexture == null || glowTexture.IsDisposed)
                {
                    glowTexture = (Texture2D)ModContent.Request<Texture2D>(Texture + "Glowmask", ReLogic.Content.AssetRequestMode.ImmediateLoad);
                }

                Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                Vector2 origin = sourceRectangle.Size() / 2f;

                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.Lerp(lightColor, Color.Orange, 0.25f), Projectile.rotation, origin, 1, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(glowTexture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation, origin, 1, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
            return false;
        }
    }
}