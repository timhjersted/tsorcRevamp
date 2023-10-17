using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    public class ScorchingPointFireball : DynamicTrail
    {
        public float angularSpeed = 0.03f;
        public float currentAngle = 0;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
            Main.projPet[Projectile.type] = true;
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;
            ProjectileID.Sets.SummonTagDamageMultiplier[Projectile.type] = ScorchingPoint.BallSummonTagDmgMult / 100f;
        }
        public sealed override void SetDefaults()
        {
            Projectile.width = 66;
            Projectile.height = 28;
            Projectile.tileCollide = false;

            Projectile.friendly = true;
            Projectile.minion = true;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.minionSlots = 0.5f;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.ContinuouslyUpdateDamageStats = true;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
            ScreenSpace = true;
            trailWidth = 45;
            trailPointLimit = 900;
            trailMaxLength = 111;
            collisionPadding = 50;
            NPCSource = false;
            trailCollision = true;
            collisionFrequency = 5;
            noFadeOut = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedTormentor", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public override void OnSpawn(IEntitySource source)
        {
            ScorchingPoint.projectiles.Add(this);
        }
        public override bool? CanCutTiles()
        {
            return false;
        }
        public override bool MinionContactDamage()
        {
            return true;
        }
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
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
                modifiers.CritDamage += 0.5f;
            }
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            target.GetGlobalNPC<tsorcRevampGlobalNPC>().lastHitPlayerSummoner = player;
            if (Main.rand.NextBool(3))
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ScorchingPoint/FireballHit1") with { Volume = ScorchingPoint.SoundVolume });
            }
            else if (Main.rand.NextBool(3))
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ScorchingPoint/FireballHit2") with { Volume = ScorchingPoint.SoundVolume });
            }
            else
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/ScorchingPoint/FireballHit3") with { Volume = ScorchingPoint.SoundVolume });
            }
            if (target.GetGlobalNPC<tsorcRevampGlobalNPC>().ScorchMarks >= 6)
            {
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().ScorchMarks = 0;
                target.GetGlobalNPC<tsorcRevampGlobalNPC>().SuperScorchDuration = ScorchingPoint.SuperBurnDuration;
                Dust.NewDust(Projectile.position, 20, 20, DustID.FlameBurst, 1, 1, 0, default, 1.5f);
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Runeterra/Summon/InterstellarVessel/MarkDetonation") with { Volume = ScorchingPoint.SoundVolume * 1.2f });
            }
        }

        public override void AI()
        {
            base.AI();
            Player owner = Main.player[Projectile.owner];
            tsorcRevampPlayer modPlayer = owner.GetModPlayer<tsorcRevampPlayer>();

            if (!CheckActive(owner))
            {
                return;
            }

            currentAngle += (angularSpeed / (modPlayer.MinionCircleRadius * 0.001f + 1f));

            Vector2 offset = new Vector2(0, modPlayer.MinionCircleRadius).RotatedBy(-currentAngle);

            Projectile.Center = owner.Center + offset;
            Projectile.velocity = Projectile.rotation.ToRotationVector2();

            Projectile.rotation = currentAngle * -1f;
            Lighting.AddLight(Projectile.Center, Color.Gold.ToVector3() * 0.48f);
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = Vector2.Distance(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2());
            if (distance < Projectile.height * 1.2f && distance > Projectile.height * 1.2f - 32)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public override float CollisionWidthFunction(float progress)
        {
            return WidthFunction(progress) - 35;
        }


        private bool CheckActive(Player owner)
        {
            if (owner.dead || !owner.active)
            {
                owner.ClearBuff(ModContent.BuffType<CenterOfTheHeat>());

                return false;
            }

            if (!owner.HasBuff(ModContent.BuffType<CenterOfTheHeat>()))
            {
                currentAngle = 0;
                ScorchingPoint.projectiles.Clear();
            }

            if (owner.HasBuff(ModContent.BuffType<CenterOfTheHeat>()))
            {
                Projectile.timeLeft = 2;
            }

            return true;
        }
        Vector2 samplePointOffset1;
        Vector2 samplePointOffset2;
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