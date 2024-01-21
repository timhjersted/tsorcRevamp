using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{
    public abstract class FlameRuneterraOrb : ModProjectile
    {
        public abstract int MaxDetectRadius { get; }
        public abstract int ProjectileSpeed { get; }
        public abstract string SoundPath { get; }
        public abstract Color LightColor { get; }
        public abstract int dustID { get; }

        public float angularSpeed = 0.04f;
        public float circleRad1 = 50f;

        public float currentAngle;
        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
            Main.projFrames[Projectile.type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 32;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 900;
            Projectile.extraUpdates = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
        }
        public int ManaCost;
        public override void OnSpawn(IEntitySource source)
        {
            Player player = Main.player[Projectile.owner];
            SoundEngine.PlaySound(new SoundStyle(SoundPath + "FireCast") with { Volume = OrbOfDeception.OrbSoundVolume });
            if (Projectile.ai[0] != 1)
            {
                ManaCost = player.GetManaCost(player.HeldItem);
            }
            else
            {
                ManaCost = 0;
            }
        }
        public override void AI()
        {
            Player player = Main.player[Projectile.owner];

            Dust.NewDust(Projectile.TopLeft + new Vector2(Projectile.width / 2, Projectile.height / 2), Projectile.width / 2, Projectile.height / 2, dustID, 0, 0, 150, default, 0.5f);
            Lighting.AddLight(Projectile.Center, LightColor.ToVector3() * 1f);

            NPC closestNPC = FindClosestNPC(MaxDetectRadius);
            if (closestNPC == null || (Projectile.timeLeft >= 780 && Projectile.ai[0] != 1))
            {
                currentAngle += (angularSpeed / (50f * 0.001f + 1f));

                Vector2 offset = new Vector2(0, 50f).RotatedBy(-currentAngle);

                Projectile.Center = player.Center + offset;

                Projectile.velocity = Projectile.rotation.ToRotationVector2();

                int IdleFrameSpeed = 5;
                Projectile.frameCounter++;

                if (Projectile.frameCounter >= IdleFrameSpeed)
                {
                    Projectile.frameCounter = 0;
                    Projectile.frame++;

                    if (Projectile.frame >= Main.projFrames[Projectile.type] / 2)
                    {
                        Projectile.frame = 0;
                    }
                }

                return;
            }

            Projectile.velocity = (closestNPC.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * ProjectileSpeed;
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;

            int frameSpeed = 5;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type] || Projectile.frame <= Main.projFrames[Projectile.type] / 2)
                {
                    Projectile.frame = 4;
                }
            }
        }

        public NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;

            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

            for (int k = 0; k < Main.maxNPCs; k++)
            {
                NPC target = Main.npc[k];
                if (target.CanBeChasedBy() && target.GetGlobalNPC<tsorcRevampGlobalNPC>().Sundered)
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

                    if (sqrDistanceToTarget < sqrMaxDetectDistance * 1.25f)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestNPC = target;
                    }
                    return closestNPC;
                }
            }
            for (int k = 0; k < Main.maxNPCs; k++)
            {
                NPC target = Main.npc[k];
                if (target.CanBeChasedBy())
                {
                    float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

                    if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    {
                        sqrMaxDetectDistance = sqrDistanceToTarget;
                        closestNPC = target;
                    }
                }
            }
            return closestNPC;
        }
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            NPC closestNPC = FindClosestNPC(MaxDetectRadius);
            if (closestNPC != null && closestNPC.Hitbox.Equals(targetHitbox) && closestNPC.Hitbox.Intersects(projHitbox))
            {
                return base.Colliding(projHitbox, targetHitbox);
            }
            return false;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            player.statMana += ManaCost / 2;
            SoundEngine.PlaySound(new SoundStyle(SoundPath + "FireHit") with { Volume = OrbOfDeception.OrbSoundVolume });
        }
        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(new SoundStyle(SoundPath + "FireDespawn") with { Volume = OrbOfDeception.OrbSoundVolume });
        }
    }
}