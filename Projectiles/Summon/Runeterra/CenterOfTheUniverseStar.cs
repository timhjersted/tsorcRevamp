using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.NPCs;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Summon.Runeterra
{
    public class CenterOfTheUniverseStar : RuneterraCirclingProjectiles
    {
        public override int ProjFrames => 1;
        public override int Width => 98;
        public override int Height => 50;
        public override int TrailWidth => 45; //35
        public override int TrailPointLimit => 900;
        public override int TrailMaxLength => 500; //250
        public override string EffectType => "tsorcRevamp/Effects/InterstellarVessel";
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Summon/CenterOfTheUniverse/";
        public override int BuffType => ModContent.BuffType<CenterOfTheUniverseBuff>();
        public override int dustID => DustID.AncientLight;
        public override string Texture => "tsorcRevamp/Projectiles/Summon/Runeterra/CenterOfTheUniverseStar";
        public override void OnSpawn(IEntitySource source)
        {
            CenterOfTheUniverse.projectiles.Add(this);
        }
        public override void OnKill(int timeLeft)
        {
            CenterOfTheUniverse.projectiles.Remove(this);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[Projectile.owner];
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().SuperSunburnDuration > 0)
            {
                modifiers.SourceDamage += ScorchingPoint.SuperBurnDmgAmp / 100f;
            }
            if (owner.GetModPlayer<tsorcRevampPlayer>().InterstellarBoost)
            {
                modifiers.SourceDamage += 0.25f;
                modifiers.FinalDamage.Flat += Math.Min(target.lifeMax / 3000, 150);
            }
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().SunburnMarks >= 6)
            {
                modifiers.SetCrit();
                modifiers.CritDamage *= ScorchingPoint.MarkDetonationCritDmgAmp;
            }
            if (target.HasBuff(ModContent.BuffType<AwestruckDebuff>()))
            {
                modifiers.FinalDamage *= 1f + CenterOfTheUniverse.AwestruckStarDamageAmp / 100f;
            }
        }
        public override void CustomOnHitNPC(NPC target)
        {
            Player player = Main.player[Projectile.owner];
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().SunburnMarks >= 6)
            {
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().SunburnMarks = 0;
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().SuperSunburnDuration = ScorchingPoint.SuperBurnDuration;
                if (player.GetModPlayer<tsorcRevampPlayer>().CenterOfTheUniverseStardustCount < 10)
                {
                    player.GetModPlayer<tsorcRevampPlayer>().CenterOfTheUniverseStardustCount++;
                    // add stardust pickup visual
                    if (player.GetModPlayer<tsorcRevampPlayer>().CenterOfTheUniverseStardustCount == 10)
                    {
                        //Can cast comet visual
                    }
                }
                Dust.NewDust(Projectile.position, 20, 20, dustID, 1, 1, 0, default, 1.5f);
                SoundEngine.PlaySound(new SoundStyle(SoundPath + "MarkDetonation") with { Volume = CenterOfTheUniverse.SoundVolume * 1.2f });
            }
        }
        public override void SetEffectParameters(Effect effect)
        {
            trailWidth = 35;
            trailMaxLength = 250;

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
            effect.Parameters["shaderColor"].SetValue(new Color(2.52f, 1.87f, 0.7f, 1f).ToVector4());
            effect.Parameters["secondaryColor"].SetValue(new Color(0f, 0f, 2.52f, 0.7f).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
        public override void CustomCheckActive()
        {
            CenterOfTheUniverse.projectiles.Clear();
        }
        public override bool PreDraw(ref Color lightColor)
        {
            visualizeTrail = false;
            base.PreDraw(ref lightColor);
            return false;
        }
    }
}