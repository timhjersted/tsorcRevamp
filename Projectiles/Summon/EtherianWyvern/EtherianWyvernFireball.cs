using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles
{
    class EtherianWyvernFireball : DynamicTrail
    {
        public override string Texture => base.Texture;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.SummonTagDamageMultiplier[Type] = EtherianWyvernStaff.SummonTagDmgMult / 100f;
        }
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.timeLeft = 120;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.extraUpdates = 1;

            trailWidth = 50;
            trailPointLimit = 1000;
            trailCollision = true;
            collisionFrequency = 2;
            collisionEndPadding = 7;
            collisionPadding = 0;
            trailYOffset = 50;
            trailMaxLength = 350;
            NPCSource = false;
            noFadeOut = true;
            noDiscontinuityCheck = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CursedTormentor", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        }
        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);
            Projectile.originalDamage = (int)Projectile.ai[0];
        }
        public override void AI()
        {
            base.AI();

            if (Projectile.wet)
            {
                Projectile.Kill();
            }
            if (Main.GameUpdateCount % 5 == 0)
            {
                Projectile.netUpdate = true;
            }
        }
        public override float CollisionWidthFunction(float progress)
        {
            if (progress > 0.9)
            {
                return ((1 - progress) / 0.1f) * trailWidth;
            }

            return trailWidth * progress;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Condition.DownedOldOnesArmyT3.IsMet())
            {
                target.AddBuff(BuffID.BetsysCurse, EtherianWyvernStaff.DebuffDuration * 60);
            }
        }
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.NPCHit3 with { Volume = 0.45f }, Projectile.position);
        }
        Vector2 samplePointOffset1;
        Vector2 samplePointOffset2;
        public override void SetEffectParameters(Effect effect)
        {
            float hostVel = Projectile.velocity.Length();

            float modifiedTime = 0.0007f * hostVel;

            if (Main.gamePaused)
            {
                modifiedTime = 0;
            }

            if (fadeOut == 1)
            {
                samplePointOffset1.X += (modifiedTime);
                samplePointOffset1.Y -= (0.001f);
                samplePointOffset2.X += (modifiedTime * 3.01f);
                samplePointOffset2.Y += (0.001f);

                samplePointOffset1.X += modifiedTime;
                samplePointOffset1.X %= 1;
                samplePointOffset1.Y %= 1;
                samplePointOffset2.X %= 1;
                samplePointOffset2.Y %= 1;
            }
            collisionEndPadding = trailPositions.Count / 2;

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseWavy);
            effect.Parameters["length"].SetValue(trailCurrentLength);
            effect.Parameters["samplePointOffset1"].SetValue(samplePointOffset1);
            effect.Parameters["samplePointOffset2"].SetValue(samplePointOffset2);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["speed"].SetValue(hostVel);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(new Color(0.886f, 0.17f, 0.15f, 1f).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}
