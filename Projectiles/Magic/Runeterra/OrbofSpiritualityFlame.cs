using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic.Runeterra
{
    public class OrbofSpiritualityFlame : ModProjectile
    {
        public float angularSpeed = 0.3f;
        public float circleRad1 = 50f;

        public float currentAngle;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 9;
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true;
        }
        public override void SetDefaults()
        {
			Projectile.width = 24;
            Projectile.height = 24;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 900;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
        }
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
			Projectile.CritChance = owner.GetWeaponCrit(owner.HeldItem);
            Vector2 visualplayercenter = owner.Center;
            float maxDetectRadius = 500f;
            float projSpeed = 5f;


            NPC closestNPC = FindClosestNPC(maxDetectRadius);
            if (closestNPC == null)
            {
                currentAngle += angularSpeed / (circleRad1 / 15);

                Vector2 offset = new Vector2(MathF.Sin(currentAngle), MathF.Cos(currentAngle)) * circleRad1;

                Projectile.position = visualplayercenter + offset;

                return;
            }

            Projectile.velocity = (closestNPC.Center - Projectile.Center).SafeNormalize(Vector2.Zero) * projSpeed;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public NPC FindClosestNPC(float maxDetectDistance)
        {
            NPC closestNPC = null;

            float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;

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
            Visuals();
            return closestNPC;
        }
        private void Visuals()
        {
            int frameSpeed = 5;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }
            }

            Lighting.AddLight(Projectile.Center, Color.Firebrick.ToVector3() * 0.78f);
            Dust.NewDust(Projectile.Center, 2, 2, DustID.Firework_Blue, 0, 0, 150, default, 0.5f);
        }
    }
}