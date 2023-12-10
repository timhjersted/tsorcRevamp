using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public class ScorchingPointFireball : RuneterraCirclingProjectiles
    {
        public override int ProjFrames => 8;
        public override int Width => 66;
        public override int Height => 28;
        public override int TrailWidth => 45;
        public override int TrailPointLimit => 900;
        public override int TrailMaxLength => 400; //111
        public override string EffectType => "tsorcRevamp/Effects/CursedTormentor";
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Summon/ScorchingPoint/";
        public override int BuffType => ModContent.BuffType<CenterOfTheHeat>();
        public override int dustID => DustID.FlameBurst;
        public override void OnSpawn(IEntitySource source)
        {
            ScorchingPoint.projectiles.Add(this);
        }
        public override void OnKill(int timeLeft)
        {
            ScorchingPoint.projectiles.Remove(this);
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            Player owner = Main.player[Projectile.owner];
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().SuperScorchDuration > 0)
            {
                modifiers.SourceDamage += ScorchingPoint.SuperBurnDmgAmp / 100f;
            }
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().ScorchMarks >= 6)
            {
                modifiers.SetCrit();
                modifiers.CritDamage *= ScorchingPoint.MarkDetonationCritDmgAmp;
            }
        }
        public override void CustomOnHitNPC(NPC target)
        {
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().ScorchMarks >= 6)
            {
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().ScorchMarks = 0;
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().SuperScorchDuration = ScorchingPoint.SuperBurnDuration;
                Dust.NewDust(Projectile.position, 20, 20, dustID, 1, 1, 0, default, 1.5f);
                SoundEngine.PlaySound(new SoundStyle(SoundPath + "MarkDetonation") with { Volume = ScorchingPoint.SoundVolume * 1.2f });
            }
        }
        public override void CustomCheckActive()
        {
            ScorchingPoint.projectiles.Clear();
        }
        public override void SetEffectParameters(Effect effect)
        {
            trailWidth = 45;
            trailMaxLength = 400;

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseWavy);
            effect.Parameters["length"].SetValue(trailCurrentLength);
            float hostVel = 0;
            hostVel = Projectile.velocity.Length();
            float modifiedTime = 0.001f * hostVel;

            if (Main.gamePaused)
            {
                modifiedTime = 0;
            }
            samplePointOffset1.X += (modifiedTime);
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
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["speed"].SetValue(hostVel);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(Color.Orange.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}