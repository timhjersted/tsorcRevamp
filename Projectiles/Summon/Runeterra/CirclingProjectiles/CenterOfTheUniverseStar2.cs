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

namespace tsorcRevamp.Projectiles.Summon.Runeterra.CirclingProjectiles
{
    /// <summary>
    /// Trail behind main star, deals no damage, entirely visual
    /// </summary>
    public class CenterOfTheUniverseStar2 : RuneterraCirclingProjectiles
    {
        public override int ProjFrames => 1;
        public override int Width => 98;
        public override int Height => 50;
        public override int TrailWidth => 150;
        public override int TrailPointLimit => 900;
        public override int TrailMaxLength => 400;
        public override string EffectType => "tsorcRevamp/Effects/InterstellarVessel";
        public override string SoundPath => "tsorcRevamp/Sounds/Runeterra/Summon/CenterOfTheUniverse/";
        public override int BuffType => ModContent.BuffType<CenterOfTheUniverseBuff>();
        public override int dustID => DustID.AncientLight;
        public override string Texture => "tsorcRevamp/Projectiles/Summon/Runeterra/CirclingProjectiles/CenterOfTheUniverseStar";
        public override void OnSpawn(IEntitySource source)
        {
            CenterOfTheUniverse.projectiles2.Add(this);
        }
        public override void OnKill(int timeLeft)
        {
            CenterOfTheUniverse.projectiles2.Remove(this);
        }
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 999999999;
        }
        public override void CustomCheckActive()
        {
            CenterOfTheUniverse.projectiles2.Clear();
        }

        public override void CustomSetDefaults()
        {
            Projectile.minionSlots = 0.5f; //shares slots with Star2 to sync with it properly
        }
        public override bool? CanCutTiles()
        {
            return null;
        }
        public override bool MinionContactDamage()
        {
            return false;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
        }
        public override void SetEffectParameters(Effect effect)
        {
            trailWidth = 150;
            trailMaxLength = 400;
            
            trailIntensity = 0.9f;
            if (Main.player[Projectile.owner].GetModPlayer<tsorcRevampPlayer>().Turboboost)
            {
                trailIntensity = 1.1f;
            }

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
            effect.Parameters["shaderColor"].SetValue(new Color(3f, 0f, 3f, 0.25f).ToVector4());
            effect.Parameters["secondaryColor"].SetValue(new Color(0f, 0f, 3f, 0.5f).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
        public override bool PreDraw(ref Color lightColor)
        {
            visualizeTrail = false;
            base.PreDraw(ref lightColor);
            return false;
        }
    }
}